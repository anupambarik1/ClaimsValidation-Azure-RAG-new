using Azure;
using Azure.AI.TextAnalytics;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure Language Service implementation for NLP entity extraction
/// </summary>
public class AzureLanguageService : IComprehendService
{
    private readonly TextAnalyticsClient _client;

    public AzureLanguageService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:LanguageService:Endpoint"] 
            ?? throw new ArgumentException("Azure:LanguageService:Endpoint not configured");
        var apiKey = configuration["Azure:LanguageService:ApiKey"] 
            ?? throw new ArgumentException("Azure:LanguageService:ApiKey not configured");
        
        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine("[LanguageService] Initialized");
    }

    public async Task<List<ComprehendEntity>> DetectEntitiesAsync(string text)
    {
        try
        {
            // Azure Language Service has a limit of 5120 text elements per document
            // Split large documents into chunks and process them separately
            const int maxChunkLength = 5000; // Conservative limit in characters
            var allEntities = new List<ComprehendEntity>();
            
            if (text.Length <= maxChunkLength)
            {
                // Process as single document
                var response = await _client.RecognizeEntitiesAsync(text);
                foreach (var entity in response.Value)
                {
                    allEntities.Add(new ComprehendEntity(
                        MapEntityCategory(entity.Category.ToString()),
                        entity.Text,
                        (float)entity.ConfidenceScore,
                        entity.Offset,
                        entity.Offset + entity.Length
                    ));
                }
            }
            else
            {
                // Split into chunks and process each
                Console.WriteLine($"[LanguageService] Text is {text.Length} chars, splitting into chunks");
                
                var chunks = SplitIntoChunks(text, maxChunkLength);
                int processedOffset = 0;
                
                foreach (var chunk in chunks)
                {
                    var response = await _client.RecognizeEntitiesAsync(chunk);
                    
                    foreach (var entity in response.Value)
                    {
                        // Adjust offset to account for chunk position in original text
                        allEntities.Add(new ComprehendEntity(
                            MapEntityCategory(entity.Category.ToString()),
                            entity.Text,
                            (float)entity.ConfidenceScore,
                            processedOffset + entity.Offset,
                            processedOffset + entity.Offset + entity.Length
                        ));
                    }
                    
                    processedOffset += chunk.Length;
                }
                
                // Deduplicate entities that might appear across chunk boundaries
                allEntities = DeduplicateEntities(allEntities);
                
                Console.WriteLine($"[LanguageService] Processed {chunks.Count} chunks");
            }

            Console.WriteLine($"[LanguageService] Extracted {allEntities.Count} entities");
            return allEntities;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[LanguageService] Error: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Language Service entity extraction failed: {ex.Message}", ex);
        }
    }
    
    private List<string> SplitIntoChunks(string text, int maxChunkLength)
    {
        var chunks = new List<string>();
        
        // Try to split at sentence boundaries to avoid cutting entities
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
                continue;
                
            // If adding this sentence would exceed the limit, save current chunk
            if (currentChunk.Length + trimmedSentence.Length + 1 > maxChunkLength && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }
            
            // If a single sentence is too long, split it forcefully
            if (trimmedSentence.Length > maxChunkLength)
            {
                var words = trimmedSentence.Split(' ');
                var subChunk = new StringBuilder();
                
                foreach (var word in words)
                {
                    if (subChunk.Length + word.Length + 1 > maxChunkLength && subChunk.Length > 0)
                    {
                        chunks.Add(subChunk.ToString());
                        subChunk.Clear();
                    }
                    
                    if (subChunk.Length > 0)
                        subChunk.Append(' ');
                    subChunk.Append(word);
                }
                
                if (subChunk.Length > 0)
                {
                    if (currentChunk.Length > 0)
                        currentChunk.Append(' ');
                    currentChunk.Append(subChunk.ToString());
                }
            }
            else
            {
                if (currentChunk.Length > 0)
                    currentChunk.Append(". ");
                currentChunk.Append(trimmedSentence);
            }
        }
        
        // Add the last chunk if it has content
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }
        
        return chunks;
    }
    
    private List<ComprehendEntity> DeduplicateEntities(List<ComprehendEntity> entities)
    {
        // Remove duplicate entities based on text and type
        // Keep the one with highest confidence score
        var deduplicated = new Dictionary<string, ComprehendEntity>();
        
        foreach (var entity in entities)
        {
            var key = $"{entity.Type}:{entity.Text}";
            
            if (!deduplicated.ContainsKey(key) || entity.Score > deduplicated[key].Score)
            {
                deduplicated[key] = entity;
            }
        }
        
        return deduplicated.Values.ToList();
    }

    public async Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text)
    {
        // Extract entities first
        var entities = await DetectEntitiesAsync(text);
        
        // Map entities to claim fields
        var fields = new Dictionary<string, string>();
        
        foreach (var entity in entities)
        {
            switch (entity.Type)
            {
                case "PERSON":
                    if (!fields.ContainsKey("ClaimantName"))
                        fields["ClaimantName"] = entity.Text;
                    break;
                case "DATE":
                    if (!fields.ContainsKey("IncidentDate"))
                        fields["IncidentDate"] = entity.Text;
                    break;
                case "QUANTITY":
                    // Try to extract numeric amount from quantity entities
                    if (!fields.ContainsKey("Amount"))
                    {
                        var cleanAmount = entity.Text.Replace("$", "").Replace(",", "").Replace("USD", "").Trim();
                        if (decimal.TryParse(cleanAmount, out _))
                        {
                            fields["Amount"] = cleanAmount;
                        }
                    }
                    break;
                case "LOCATION":
                    if (!fields.ContainsKey("Location"))
                        fields["Location"] = entity.Text;
                    break;
            }
        }
        
        // Use regex patterns to find policy numbers and amounts that entities might miss
        ExtractPolicyNumberFromText(text, fields);
        ExtractAmountFromText(text, fields);
        
        Console.WriteLine($"[LanguageService] Extracted {fields.Count} claim fields");
        return fields;
    }
    
    private void ExtractPolicyNumberFromText(string text, Dictionary<string, string> fields)
    {
        if (fields.ContainsKey("policyNumber"))
            return;
            
        // Common policy number patterns
        var policyPatterns = new[]
        {
            @"(?:Policy\s*(?:Number|No\.?|#):?\s*)([A-Z0-9-]+)",
            @"(?:POL-\d{4}-\d+)",
            @"(?:Policy\s+ID:?\s*)([A-Z0-9-]+)"
        };
        
        foreach (var pattern in policyPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var policyNumber = match.Groups[1].Success ? match.Groups[1].Value : match.Value;
                fields["policyNumber"] = policyNumber.Trim();
                Console.WriteLine($"[LanguageService] Found policy number via regex: {fields["policyNumber"]}");
                break;
            }
        }
    }
    
    private void ExtractAmountFromText(string text, Dictionary<string, string> fields)
    {
        if (fields.ContainsKey("claimAmount"))
            return;
            
        // Look for claim amount patterns
        var amountPatterns = new[]
        {
            @"(?:Claim\s*Amount:?\s*)\$?([\d,]+\.?\d*)",
            @"(?:Amount\s*Claimed:?\s*)\$?([\d,]+\.?\d*)",
            @"(?:Total\s*Amount:?\s*)\$?([\d,]+\.?\d*)"
        };
        
        foreach (var pattern in amountPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups[1].Success)
            {
                var amount = match.Groups[1].Value.Replace(",", "").Trim();
                if (decimal.TryParse(amount, out _))
                {
                    fields["claimAmount"] = amount;
                    Console.WriteLine($"[LanguageService] Found claim amount via regex: {fields["claimAmount"]}");
                    break;
                }
            }
        }
    }

    private string MapEntityCategory(string azureCategory)
    {
        // Map Azure entity categories to AWS Comprehend-style naming
        return azureCategory switch
        {
            "Person" => "PERSON",
            "DateTime" => "DATE",
            "Quantity" => "QUANTITY",
            "Organization" => "ORGANIZATION",
            "Location" => "LOCATION",
            "Event" => "EVENT",
            "Product" => "COMMERCIAL_ITEM",
            _ => "OTHER"
        };
    }
}
