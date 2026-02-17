using Azure;
using Azure.AI.OpenAI;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure OpenAI implementation for LLM-based claim decision generation
/// </summary>
public class AzureLlmService : ILlmService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public AzureLlmService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:OpenAI:Endpoint"] 
            ?? throw new ArgumentException("Azure:OpenAI:Endpoint not configured");
        var apiKey = configuration["Azure:OpenAI:ApiKey"] 
            ?? throw new ArgumentException("Azure:OpenAI:ApiKey not configured");
        
        _deploymentName = configuration["Azure:OpenAI:ChatDeployment"] ?? "gpt-4-turbo";
        
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine($"[AzureLLM] Initialized with deployment: {_deploymentName}");
    }

    public async Task<ClaimDecision> GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request, clauses);

        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 4096,
                Temperature = 0.3f,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _client.GetChatCompletionsAsync(chatOptions);
            var content = response.Value.Choices[0].Message.Content;

            Console.WriteLine($"[AzureLLM] Generated decision with {response.Value.Usage.TotalTokens} tokens");

            var decision = JsonSerializer.Deserialize<ClaimDecision>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return decision ?? throw new InvalidOperationException("Failed to parse LLM decision response");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureLLM] Error: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Azure OpenAI chat completion failed: {ex.Message}", ex);
        }
    }

    public async Task<ClaimDecision> GenerateDecisionWithSupportingDocumentsAsync(
        ClaimRequest request, 
        List<PolicyClause> clauses, 
        List<string> supportingDocumentContents)
    {
        var systemPrompt = BuildSystemPromptWithDocuments();
        var userPrompt = BuildUserPromptWithSupportingDocuments(request, clauses, supportingDocumentContents);

        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 4096,
                Temperature = 0.3f,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _client.GetChatCompletionsAsync(chatOptions);
            var content = response.Value.Choices[0].Message.Content;

            Console.WriteLine($"[AzureLLM] Generated decision with supporting docs using {response.Value.Usage.TotalTokens} tokens");

            var decision = JsonSerializer.Deserialize<ClaimDecision>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return decision ?? throw new InvalidOperationException("Failed to parse LLM decision response with documents");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureLLM] Error with supporting docs: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Azure OpenAI chat completion failed: {ex.Message}", ex);
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert insurance claims adjuster for Aflac.
Your role is to analyze claims against policy clauses and provide accurate coverage decisions.

CRITICAL: Return ONLY valid JSON with this exact structure:
{
  ""Status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""Explanation"": ""detailed explanation"",
  ""ClauseReferences"": [""clause-id-1"", ""clause-id-2""],
  ""RequiredDocuments"": [""document-1"", ""document-2""],
  ""ConfidenceScore"": 0.0-1.0
}

Do not include any text outside the JSON structure.";
    }

    private string BuildSystemPromptWithDocuments()
    {
        return @"You are an expert insurance claims adjuster for Aflac.
Your role is to validate claims against policy clauses AND supporting documents.

CRITICAL TASKS:
1. Verify claim details match the supporting documents
2. Check if document evidence supports the claimed amount
3. Assess document quality and completeness
4. Identify any contradictions between claim and evidence
5. Increase confidence if evidence strongly supports claim
6. Flag for manual review if evidence is missing, contradictory, or insufficient

Return ONLY valid JSON with this exact structure:
{
  ""Status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""Explanation"": ""detailed explanation referencing supporting evidence"",
  ""ClauseReferences"": [""clause-id-1"", ""clause-id-2""],
  ""RequiredDocuments"": [""any additional documents needed""],
  ""ConfidenceScore"": 0.0-1.0
}

Do not include any text outside the JSON structure.";
    }

    private string BuildUserPrompt(ClaimRequest request, List<PolicyClause> clauses)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.Text}"));

        var documentGuidance = GetDocumentRequirementGuidance(request.ClaimAmount);

        return $@"CLAIM DETAILS:
Policy Number: {request.PolicyNumber}
Policy Type: {request.PolicyType}
Claim Amount: ${request.ClaimAmount:N2}
Description: {request.ClaimDescription}

RELEVANT POLICY CLAUSES:
{clausesText}

{documentGuidance}

Analyze this claim and return your decision as JSON.";
    }

    private string BuildUserPromptWithSupportingDocuments(
        ClaimRequest request, 
        List<PolicyClause> clauses, 
        List<string> supportingDocumentContents)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.Text}"));

        var documentsText = string.Join("\n\n---\n\n", supportingDocumentContents.Select((doc, idx) => 
            $"SUPPORTING DOCUMENT {idx + 1}:\n{doc}"));

        return $@"CLAIM DETAILS:
Policy Number: {request.PolicyNumber}
Policy Type: {request.PolicyType}
Claim Amount: ${request.ClaimAmount:N2}
Description: {request.ClaimDescription}

RELEVANT POLICY CLAUSES:
{clausesText}

SUPPORTING DOCUMENTS SUBMITTED:
{documentsText}

VALIDATION INSTRUCTIONS:
1. Validate the claim details against the supporting documents
2. Check if document evidence supports the claimed amount
3. Verify all claim details are consistent with evidence
4. Assess document quality and completeness
5. Increase confidence if evidence strongly supports claim
6. Decrease confidence or flag for manual review if evidence is missing, contradictory, or insufficient

Analyze this claim with its supporting evidence and return your decision as JSON.";
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

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 2000,
                Temperature = 0.1f,  // Low temperature for consistent extraction
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _client.GetChatCompletionsAsync(chatOptions);
            var content = response.Value.Choices[0].Message.Content;

            Console.WriteLine($"[AzureLLM] Generated extraction response with {response.Value.Usage.TotalTokens} tokens");

            return content;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureLLM] Error in GenerateResponseAsync: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Azure OpenAI response generation failed: {ex.Message}", ex);
        }
    }
}
