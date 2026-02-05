using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace ClaimsRagBot.Infrastructure.Comprehend;

public class ComprehendService : IComprehendService
{
    private readonly IAmazonComprehend _client;
    private readonly string? _customEndpointArn;

    public ComprehendService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];

        // accessKeyId = "testaccesskey";
        // secretAccessKey = "testsecretaccesskey";

        var config = new AmazonComprehendConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonComprehendClient(credentials, config);
            Console.WriteLine($"[Comprehend] Using credentials from appsettings for region: {region}");
        }
        else
        {
            _client = new AmazonComprehendClient(config);
            Console.WriteLine($"[Comprehend] Using default credential chain for region: {region}");
        }
        
        _customEndpointArn = configuration["AWS:Comprehend:CustomEntityRecognizerArn"];
    }

    public async Task<List<ComprehendEntity>> DetectEntitiesAsync(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<ComprehendEntity>();
            }
            
            // Truncate text if too long (Comprehend has 5000 byte limit for synchronous calls)
            var truncatedText = text.Length > 5000 ? text.Substring(0, 5000) : text;
            
            Console.WriteLine($"[Comprehend] Detecting entities in text ({truncatedText.Length} chars)");
            
            DetectEntitiesResponse response;
            
            if (!string.IsNullOrEmpty(_customEndpointArn))
            {
                // Use custom entity recognizer if configured
                var request = new DetectEntitiesRequest
                {
                    Text = truncatedText,
                    LanguageCode = LanguageCode.En,
                    EndpointArn = _customEndpointArn
                };
                
                response = await _client.DetectEntitiesAsync(request);
                Console.WriteLine($"[Comprehend] Using custom endpoint: {_customEndpointArn}");
            }
            else
            {
                // Use built-in entity recognition
                var request = new DetectEntitiesRequest
                {
                    Text = truncatedText,
                    LanguageCode = LanguageCode.En
                };
                
                response = await _client.DetectEntitiesAsync(request);
                Console.WriteLine($"[Comprehend] Using built-in entity recognition");
            }
            
            var entities = response.Entities.Select(e => new ComprehendEntity(
                Type: e.Type ?? "UNKNOWN",
                Text: e.Text ?? "",
                Score: e.Score ?? 0f,
                BeginOffset: e.BeginOffset ?? 0,
                EndOffset: e.EndOffset ?? 0
            )).ToList();
            
            Console.WriteLine($"[Comprehend] Detected {entities.Count} entities");
            
            return entities;
        }
        catch (AmazonComprehendException ex)
        {
            Console.WriteLine($"[Comprehend] Error: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"Comprehend entity detection failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text)
    {
        var entities = await DetectEntitiesAsync(text);
        var fields = new Dictionary<string, string>();
        
        try
        {
            // Extract policy number (pattern: POL-XXXX-XXXXX or similar)
            var policyNumberMatch = Regex.Match(text, @"POL-\d{4,}-\d+|Policy\s*(?:Number|#|No\.?):?\s*([A-Z0-9\-]+)", RegexOptions.IgnoreCase);
            if (policyNumberMatch.Success)
            {
                fields["policyNumber"] = policyNumberMatch.Groups.Count > 1 && !string.IsNullOrEmpty(policyNumberMatch.Groups[1].Value)
                    ? policyNumberMatch.Groups[1].Value
                    : policyNumberMatch.Value;
            }
            else
            {
                // Check entities for COMMERCIAL_ITEM or OTHER that might be policy number
                var potentialPolicy = entities.FirstOrDefault(e => 
                    e.Score > 0.7 && 
                    (e.Text.StartsWith("POL", StringComparison.OrdinalIgnoreCase) || 
                     e.Text.Contains("-")));
                
                if (potentialPolicy != null)
                {
                    fields["policyNumber"] = potentialPolicy.Text;
                }
            }
            
            // Extract claim amount from QUANTITY or text patterns
            var quantityEntities = entities.Where(e => e.Type == "QUANTITY" && e.Score > 0.7).ToList();
            var amountMatch = Regex.Match(text, @"\$?\s*([0-9,]+\.?\d*)\s*(?:dollars?|USD)?", RegexOptions.IgnoreCase);
            
            if (amountMatch.Success)
            {
                var amountStr = amountMatch.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(amountStr, out var amount))
                {
                    fields["claimAmount"] = amount.ToString("F2");
                }
            }
            else if (quantityEntities.Any())
            {
                var numericValue = ExtractNumericValue(quantityEntities.First().Text);
                if (numericValue.HasValue)
                {
                    fields["claimAmount"] = numericValue.Value.ToString("F2");
                }
            }
            
            // Extract policy type from text patterns
            var policyTypePatterns = new Dictionary<string, string>
            {
                { @"\b(motor|auto|vehicle|car)\s+insurance\b", "Motor" },
                { @"\b(home|house|property)\s+insurance\b", "Home" },
                { @"\b(health|medical)\s+insurance\b", "Health" },
                { @"\b(life)\s+insurance\b", "Life" }
            };
            
            foreach (var pattern in policyTypePatterns)
            {
                if (Regex.IsMatch(text, pattern.Key, RegexOptions.IgnoreCase))
                {
                    fields["policyType"] = pattern.Value;
                    break;
                }
            }
            
            // Extract dates
            var dateEntities = entities.Where(e => e.Type == "DATE" && e.Score > 0.8).ToList();
            if (dateEntities.Any())
            {
                fields["dateOfLoss"] = dateEntities.First().Text;
            }
            
            // Extract location
            var locationEntities = entities.Where(e => e.Type == "LOCATION" && e.Score > 0.75).ToList();
            if (locationEntities.Any())
            {
                fields["location"] = string.Join(", ", locationEntities.Select(e => e.Text));
            }
            
            Console.WriteLine($"[Comprehend] Extracted {fields.Count} claim fields");
            
            return fields;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Comprehend] Error extracting claim fields: {ex.Message}");
            return fields;
        }
    }

    private decimal? ExtractNumericValue(string text)
    {
        var match = Regex.Match(text, @"[\d,]+\.?\d*");
        if (match.Success)
        {
            var cleanValue = match.Value.Replace(",", "");
            if (decimal.TryParse(cleanValue, out var value))
            {
                return value;
            }
        }
        return null;
    }
}
