using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure Computer Vision implementation for image analysis
/// </summary>
public class AzureComputerVisionService : IRekognitionService
{
    private readonly ComputerVisionClient _client;
    private readonly double _minConfidence;

    public AzureComputerVisionService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:ComputerVision:Endpoint"] 
            ?? throw new ArgumentException("Azure:ComputerVision:Endpoint not configured");
        var apiKey = configuration["Azure:ComputerVision:ApiKey"] 
            ?? throw new ArgumentException("Azure:ComputerVision:ApiKey not configured");
        
        _minConfidence = double.Parse(configuration["Azure:ComputerVision:MinConfidence"] ?? "0.7");

        _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey))
        {
            Endpoint = endpoint
        };
        
        Console.WriteLine("[ComputerVision] Initialized");
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(string s3Bucket, string s3Key)
    {
        try
        {
            var imageUrl = $"https://{s3Bucket}.blob.core.windows.net/{s3Key}";
            
            var features = new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Description
            };

            var analysis = await _client.AnalyzeImageAsync(imageUrl, features);
            var labels = new List<string>();
            var damageType = "Unknown";
            var maxConfidence = 0f;

            // Extract tags
            foreach (var tag in analysis.Tags)
            {
                if (tag.Confidence >= _minConfidence)
                {
                    labels.Add(tag.Name);
                    
                    // Check for damage-related tags
                    if ((tag.Name.Contains("damage") || tag.Name.Contains("broken") || tag.Name.Contains("crack")) 
                        && tag.Confidence > maxConfidence)
                    {
                        damageType = tag.Name;
                        maxConfidence = (float)tag.Confidence;
                    }
                }
            }

            // Extract objects
            foreach (var obj in analysis.Objects)
            {
                if (obj.Confidence >= _minConfidence && !labels.Contains(obj.ObjectProperty))
                {
                    labels.Add(obj.ObjectProperty);
                }
            }

            Console.WriteLine($"[ComputerVision] Detected {labels.Count} labels/objects");

            return new ImageAnalysisResult(
                s3Key,
                labels,
                damageType,
                maxConfidence,
                new Dictionary<string, object>
                {
                    { "TagCount", analysis.Tags.Count },
                    { "ObjectCount", analysis.Objects.Count }
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ComputerVision] Error: {ex.Message}");
            throw new InvalidOperationException($"Computer Vision analysis failed: {ex.Message}", ex);
        }
    }

    public async Task<List<ImageAnalysisResult>> AnalyzeImagesAsync(string s3Bucket, List<string> s3Keys)
    {
        var results = new List<ImageAnalysisResult>();
        
        foreach (var key in s3Keys)
        {
            try
            {
                var result = await AnalyzeImageAsync(s3Bucket, key);
                results.Add(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComputerVision] Failed to analyze {key}: {ex.Message}");
                // Continue with other images
            }
        }
        
        return results;
    }
}
