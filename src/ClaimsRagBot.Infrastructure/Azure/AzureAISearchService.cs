using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure AI Search implementation for RAG vector retrieval
/// </summary>
public class AzureAISearchService : IRetrievalService
{
    private readonly SearchClient _searchClient;
    private readonly string _indexName;

    public AzureAISearchService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:AISearch:Endpoint"] 
            ?? throw new ArgumentException("Azure:AISearch:Endpoint not configured");
        var apiKey = configuration["Azure:AISearch:QueryApiKey"] 
            ?? throw new ArgumentException("Azure:AISearch:QueryApiKey not configured");
        
        _indexName = configuration["Azure:AISearch:IndexName"] ?? "policy-clauses";
        
        var credential = new AzureKeyCredential(apiKey);
        var indexClient = new SearchIndexClient(new Uri(endpoint), credential);
        _searchClient = indexClient.GetSearchClient(_indexName);
        
        Console.WriteLine($"[AzureAISearch] Connected to index: {_indexName}");
    }

    public async Task<List<PolicyClause>> RetrieveClausesAsync(float[] embedding, string policyType)
    {
        try
        {
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = 5,
                Fields = { "Embedding" }
            };

            var searchOptions = new SearchOptions
            {
                VectorSearch = new() { Queries = { vectorQuery } },
                Filter = $"PolicyType eq '{policyType}'",
                Select = { "ClauseId", "Text", "CoverageType", "PolicyType" },
                Size = 5
            };

            var results = await _searchClient.SearchAsync<PolicyClauseSearchResult>(null, searchOptions);

            var clauses = new List<PolicyClause>();
            await foreach (var result in results.Value.GetResultsAsync())
            {
                clauses.Add(new PolicyClause(
                    result.Document.ClauseId,
                    result.Document.Text,
                    result.Document.CoverageType,
                    (float)(result.Score ?? 0.0)
                ));
            }

            Console.WriteLine($"[AzureAISearch] Retrieved {clauses.Count} clauses for policy type: {policyType}");
            return clauses;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureAISearch] Azure AI Search error: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException(
                $"Azure AI Search query failed. Status: {ex.Status}. Ensure the index is created and populated. Error: {ex.Message}", 
                ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureAISearch] Unexpected error: {ex.Message}");
            throw new InvalidOperationException(
                $"Azure AI Search query failed: {ex.Message}. Check connection string and index configuration.", 
                ex);
        }
    }
}

/// <summary>
/// Search result model for Azure AI Search
/// </summary>
public class PolicyClauseSearchResult
{
    public string ClauseId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string CoverageType { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
}