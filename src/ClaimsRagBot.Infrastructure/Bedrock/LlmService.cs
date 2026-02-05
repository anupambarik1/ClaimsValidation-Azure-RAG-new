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

        accessKeyId = "testaccesskey";
        secretAccessKey = "testsecretaccesskey";


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
            ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
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

    private string BuildPrompt(ClaimRequest request, List<PolicyClause> clauses)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.CoverageType}: {c.Text}"));

        return $@"Claim:
Policy Number: {request.PolicyNumber}
Claim Amount: ${request.ClaimAmount}
Description: {request.ClaimDescription}

Policy Clauses:
{clausesText}

Respond in JSON:
{{
  ""status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""explanation"": ""<explanation>"",
  ""clauseReferences"": [""<clause_id>""],
  ""requiredDocuments"": [""<document>""],
  ""confidenceScore"": 0.0-1.0
}}";
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
