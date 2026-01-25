using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Rekognition;

public class RekognitionService : IRekognitionService
{
    private readonly IAmazonRekognition _client;
    private readonly float _minConfidence;
    private readonly string? _customModelArn;

    public RekognitionService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];
        
        var config = new AmazonRekognitionConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonRekognitionClient(credentials, config);
            Console.WriteLine($"[Rekognition] Using credentials from appsettings for region: {region}");
        }
        else
        {
            _client = new AmazonRekognitionClient(config);
            Console.WriteLine($"[Rekognition] Using default credential chain for region: {region}");
        }
        
        _minConfidence = float.Parse(configuration["AWS:Rekognition:MinConfidence"] ?? "70");
        _customModelArn = configuration["AWS:Rekognition:CustomModelArn"];
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(string s3Bucket, string s3Key)
    {
        try
        {
            Console.WriteLine($"[Rekognition] Analyzing image s3://{s3Bucket}/{s3Key}");
            
            var image = new Image
            {
                S3Object = new Amazon.Rekognition.Model.S3Object
                {
                    Bucket = s3Bucket,
                    Name = s3Key
                }
            };
            
            List<string> labels = new List<string>();
            string damageType = "Unknown";
            float confidence = 0;
            var metadata = new Dictionary<string, object>();
            
            // Use custom model if configured (for vehicle damage detection)
            if (!string.IsNullOrEmpty(_customModelArn))
            {
                var customRequest = new DetectCustomLabelsRequest
                {
                    ProjectVersionArn = _customModelArn,
                    Image = image,
                    MinConfidence = _minConfidence
                };
                
                var customResponse = await _client.DetectCustomLabelsAsync(customRequest);
                
                labels = customResponse.CustomLabels.Select(l => l.Name).ToList();
                
                if (customResponse.CustomLabels.Any())
                {
                    var topLabel = customResponse.CustomLabels.OrderByDescending(l => l.Confidence).First();
                    damageType = topLabel.Name ?? "Unknown";
                    confidence = topLabel.Confidence ?? 0f;
                }
                
                metadata["customLabels"] = customResponse.CustomLabels.Select(l => new
                {
                    l.Name,
                    l.Confidence
                }).ToList();
                
                Console.WriteLine($"[Rekognition] Custom model detected {labels.Count} labels");
            }
            else
            {
                // Use standard label detection
                var labelRequest = new DetectLabelsRequest
                {
                    Image = image,
                    MaxLabels = 20,
                    MinConfidence = _minConfidence
                };
                
                var labelResponse = await _client.DetectLabelsAsync(labelRequest);
                
                labels = labelResponse.Labels.Select(l => l.Name).ToList();
                
                // Infer damage type from standard labels
                damageType = InferDamageType(labelResponse.Labels);
                confidence = labelResponse.Labels.Any() 
                    ? labelResponse.Labels.Max(l => l.Confidence ?? 0f)
                    : 0f;
                
                metadata["labels"] = labelResponse.Labels.Select(l => new
                {
                    l.Name,
                    l.Confidence,
                    Categories = l.Categories?.Select(c => c.Name).ToList() ?? new List<string>()
                }).ToList();
                
                Console.WriteLine($"[Rekognition] Standard detection found {labels.Count} labels");
            }
            
            // Detect text in image (useful for license plates, signs, etc.)
            var textRequest = new DetectTextRequest
            {
                Image = image
            };
            
            var textResponse = await _client.DetectTextAsync(textRequest);
            
            if (textResponse.TextDetections.Any())
            {
                var detectedText = string.Join(" ", 
                    textResponse.TextDetections
                        .Where(t => t.Type == TextTypes.LINE && t.Confidence > _minConfidence)
                        .Select(t => t.DetectedText));
                
                if (!string.IsNullOrWhiteSpace(detectedText))
                {
                    metadata["detectedText"] = detectedText;
                    Console.WriteLine($"[Rekognition] Detected text: {detectedText}");
                }
            }
            
            return new ImageAnalysisResult(
                ImageId: s3Key,
                Labels: labels,
                DamageType: damageType,
                Confidence: confidence,
                Metadata: metadata
            );
        }
        catch (AmazonRekognitionException ex)
        {
            Console.WriteLine($"[Rekognition] Error: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"Rekognition analysis failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<List<ImageAnalysisResult>> AnalyzeImagesAsync(string s3Bucket, List<string> s3Keys)
    {
        var results = new List<ImageAnalysisResult>();
        
        foreach (var s3Key in s3Keys)
        {
            try
            {
                var result = await AnalyzeImageAsync(s3Bucket, s3Key);
                results.Add(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rekognition] Failed to analyze {s3Key}: {ex.Message}");
                // Continue with other images even if one fails
            }
        }
        
        Console.WriteLine($"[Rekognition] Successfully analyzed {results.Count}/{s3Keys.Count} images");
        
        return results;
    }

    private string InferDamageType(List<Label> labels)
    {
        // Map standard Rekognition labels to damage types
        var damageKeywords = new Dictionary<string, List<string>>
        {
            { "Collision", new List<string> { "Crash", "Collision", "Dent", "Damaged", "Broken" } },
            { "Scratch", new List<string> { "Scratch", "Scrape", "Abrasion" } },
            { "Fire", new List<string> { "Fire", "Burn", "Smoke", "Charred" } },
            { "Water", new List<string> { "Water", "Flood", "Wet", "Moisture" } },
            { "Theft", new List<string> { "Theft", "Stolen", "Missing", "Vandalism" } },
            { "Glass", new List<string> { "Glass", "Windshield", "Window", "Shattered" } }
        };
        
        foreach (var damageType in damageKeywords)
        {
            foreach (var keyword in damageType.Value)
            {
                if (labels.Any(l => l.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    return damageType.Key;
                }
            }
        }
        
        // Check for vehicle-related labels as fallback
        var vehicleLabels = labels.Where(l => 
            l.Name.Contains("Car", StringComparison.OrdinalIgnoreCase) ||
            l.Name.Contains("Vehicle", StringComparison.OrdinalIgnoreCase) ||
            l.Name.Contains("Auto", StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (vehicleLabels.Any())
        {
            return "VehicleDamage";
        }
        
        return "Unknown";
    }
}
