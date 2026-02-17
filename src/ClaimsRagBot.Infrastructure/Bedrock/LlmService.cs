using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Infrastructure.Bedrock;

public class LlmService : ILlmService
{
    private readonly AmazonBedrockRuntimeClient _client;

    public LlmService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];

        // accessKeyId = "testaccesskey";
        // secretAccessKey = "testsecretaccesskey";


        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonBedrockRuntimeClient(credentials, config);
            Console.WriteLine($"[Bedrock] Using credentials from appsettings for region: {region}");
        }
        else
        {
            // Fallback to default credential chain
            _client = new AmazonBedrockRuntimeClient(config);
            Console.WriteLine($"[Bedrock] Using default credential chain for region: {region}");
        }
    }

    public async Task<ClaimDecision> GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses)
    {
        var prompt = BuildPrompt(request, clauses);
        
        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 1024,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            system = @"You are an insurance claims validation assistant.
You MUST:
- Use ONLY the provided policy clauses
- Cite clause IDs
- If unsure, say 'Needs Manual Review'
- Respond in valid JSON format only"
        };

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
            ContentType = "application/json",
            Accept = "application/json"
        };

        InvokeModelResponse response;
        try
        {
            response = await _client.InvokeModelAsync(invokeRequest);
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            Console.WriteLine($"[Bedrock Error] {ex.ErrorCode}: {ex.Message}");
            Console.WriteLine($"[Bedrock] Status Code: {ex.StatusCode}");
            throw new Exception($"Bedrock API Error: {ex.ErrorCode} - {ex.Message}. Check: 1) Credentials are valid, 2) Model access is enabled in AWS Console (Bedrock > Model access), 3) IAM permissions include bedrock:InvokeModel", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bedrock Error] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        
        using var reader = new StreamReader(response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        Console.WriteLine($"[DEBUG] Raw Bedrock Response: {responseBody}");
        
        var result = JsonSerializer.Deserialize<ClaudeResponse>(responseBody);

        var content = result?.Content?.FirstOrDefault()?.Text ?? "{}";
        
        Console.WriteLine($"[DEBUG] Extracted Text Content: {content}");
        
        // Extract JSON from potential markdown code blocks
        content = content.Replace("```json", "").Replace("```", "").Trim();
        
        Console.WriteLine($"[DEBUG] Cleaned JSON: {content}");
        
        ClaimDecision? decision = null;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            decision = JsonSerializer.Deserialize<ClaimDecision>(content, options);
            Console.WriteLine($"[DEBUG] Deserialized Decision: Status={decision?.Status}, Explanation={decision?.Explanation}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON Deserialization Failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Problematic JSON: {content}");
        }
        
        return decision ?? new ClaimDecision(
            Status: "Manual Review",
            Explanation: "Failed to parse LLM response",
            ClauseReferences: new List<string>(),
            RequiredDocuments: new List<string>(),
            ConfidenceScore: 0.0f
        );
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt)
    {
        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 2000,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = userPrompt
                }
            },
            system = systemPrompt,
            temperature = 0.1  // Low temperature for consistent extraction
        };

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
            ContentType = "application/json",
            Accept = "application/json"
        };

        try
        {
            var response = await _client.InvokeModelAsync(invokeRequest);
            
            using var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var result = JsonSerializer.Deserialize<ClaudeResponse>(responseBody);
            var content = result?.Content?.FirstOrDefault()?.Text ?? "{}";
            
            // Clean up any markdown code blocks
            content = content.Replace("```json", "").Replace("```", "").Trim();
            
            Console.WriteLine($"[Bedrock] Generated extraction response");
            
            return content;
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            Console.WriteLine($"[Bedrock Error] in GenerateResponseAsync: {ex.ErrorCode}: {ex.Message}");
            throw new Exception($"Bedrock API Error: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<ClaimDecision> GenerateDecisionWithSupportingDocumentsAsync(
        ClaimRequest request, 
        List<PolicyClause> clauses, 
        List<string> supportingDocumentContents)
    {
        var prompt = BuildPromptWithSupportingDocuments(request, clauses, supportingDocumentContents);
        
        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 2048,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            system = @"You are an insurance claims validation assistant.
You MUST:
- Validate claim details against the supporting documents provided
- Verify consistency between claim and evidence
- Use ONLY the provided policy clauses
- Cite clause IDs and document evidence
- If evidence contradicts claim or is insufficient, say 'Needs Manual Review'
- Respond in valid JSON format only"
        };

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
            ContentType = "application/json",
            Accept = "application/json"
        };

        InvokeModelResponse response;
        try
        {
            response = await _client.InvokeModelAsync(invokeRequest);
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            Console.WriteLine($"[Bedrock Error] {ex.ErrorCode}: {ex.Message}");
            throw new Exception($"Bedrock API Error: {ex.ErrorCode} - {ex.Message}. Check: 1) Credentials are valid, 2) Model access is enabled in AWS Console (Bedrock > Model access), 3) IAM permissions include bedrock:InvokeModel", ex);
        }
        
        using var reader = new StreamReader(response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        Console.WriteLine($"[DEBUG] Raw Bedrock Response (with docs): {responseBody}");
        
        var result = JsonSerializer.Deserialize<ClaudeResponse>(responseBody);
        var content = result?.Content?.FirstOrDefault()?.Text ?? "{}";
        
        content = content.Replace("```json", "").Replace("```", "").Trim();
        
        ClaimDecision? decision = null;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            decision = JsonSerializer.Deserialize<ClaimDecision>(content, options);
            Console.WriteLine($"[DEBUG] Deserialized Decision with Docs: Status={decision?.Status}, Confidence={decision?.ConfidenceScore}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON Deserialization Failed: {ex.Message}");
        }
        
        return decision ?? new ClaimDecision(
            Status: "Manual Review",
            Explanation: "Failed to parse LLM response with supporting documents",
            ClauseReferences: new List<string>(),
            RequiredDocuments: new List<string>(),
            ConfidenceScore: 0.0f
        );
    }

    private string BuildPrompt(ClaimRequest request, List<PolicyClause> clauses)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.CoverageType}: {c.Text}"));

        // Add amount-based document requirement guidance
        var documentGuidance = GetDocumentRequirementGuidance(request.ClaimAmount);

        return $@"Claim:
Policy Number: {request.PolicyNumber}
Claim Amount: ${request.ClaimAmount}
Description: {request.ClaimDescription}

Policy Clauses:
{clausesText}

{documentGuidance}

Respond in JSON:
{{
  ""status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""explanation"": ""<explanation>"",
  ""clauseReferences"": [""<clause_id>""],
  ""requiredDocuments"": [""<document>""],
  ""confidenceScore"": 0.0-1.0
}}";
    }

    private string BuildPromptWithSupportingDocuments(
        ClaimRequest request, 
        List<PolicyClause> clauses, 
        List<string> supportingDocumentContents)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.CoverageType}: {c.Text}"));

        var documentsText = string.Join("\n\n---\n\n", supportingDocumentContents.Select((doc, idx) => 
            $"SUPPORTING DOCUMENT {idx + 1}:\n{doc}"));

        return $@"Claim:
Policy Number: {request.PolicyNumber}
Claim Amount: ${request.ClaimAmount}
Description: {request.ClaimDescription}

Policy Clauses:
{clausesText}

Supporting Documents Submitted:
{documentsText}

INSTRUCTIONS:
1. Validate the claim details against the supporting documents
2. Check if document evidence supports the claimed amount
3. Verify all claim details are consistent with evidence
4. Assess document quality and completeness
5. Increase confidence if evidence strongly supports claim
6. Decrease confidence or flag for manual review if evidence is missing, contradictory, or insufficient

Respond in JSON:
{{
  ""status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""explanation"": ""<explanation with reference to supporting evidence>"",
  ""clauseReferences"": [""<clause_id>""],
  ""requiredDocuments"": [""<any additional documents needed>""],
  ""confidenceScore"": 0.0-1.0
}}";
    }

    private string GetDocumentRequirementGuidance(decimal claimAmount)
    {
        return claimAmount switch
        {
            < 500m => @"DOCUMENT REQUIREMENTS: For this low-value claim (<$500), require only basic proof:
- Claim form or receipt
- Brief description of incident
Note: Minimal documentation acceptable for small claims.",

            < 1000m => @"DOCUMENT REQUIREMENTS: For this moderate claim ($500-$1,000), require standard documentation:
- Claim form
- Receipts or invoices
- Basic incident documentation (e.g., photos, brief report)
Note: Standard verification required.",

            < 5000m => @"DOCUMENT REQUIREMENTS: For this significant claim ($1,000-$5,000), require comprehensive documentation:
- Detailed claim form
- Itemized receipts/bills
- Incident reports or medical records
- Photos or damage assessment
- Supporting evidence of loss
Note: Thorough documentation required for substantial claims.",

            _ => @"DOCUMENT REQUIREMENTS: For this high-value claim (>$5,000), require extensive documentation and verification:
- Complete claim form with all details
- Comprehensive receipts, bills, and invoices
- Official reports (medical, police, repair estimates)
- Multiple forms of evidence (photos, videos, witness statements)
- Professional assessments where applicable
Note: Extensive verification required. Consider flagging for manual review even with good documentation."
        };
    }
}


internal class ClaudeResponse
{
    [JsonPropertyName("content")]
    public List<ContentBlock>? Content { get; set; }
}

internal class ContentBlock
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
