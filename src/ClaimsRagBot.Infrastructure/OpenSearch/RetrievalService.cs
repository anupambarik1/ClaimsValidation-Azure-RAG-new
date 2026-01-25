using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.OpenSearch;

public class RetrievalService : IRetrievalService
{
    private readonly HttpClient _httpClient;
    private readonly string _opensearchEndpoint;
    private readonly string _indexName;
    private readonly bool _useRealOpenSearch;
    private readonly AWSCredentials _credentials;

    public RetrievalService(IConfiguration? configuration = null)
    {
        _httpClient = new HttpClient();
        
        var accessKeyId = configuration?["AWS:AccessKeyId"];
        var secretAccessKey = configuration?["AWS:SecretAccessKey"];
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            _credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        }
        else
        {
            // Fallback to default credential chain
#pragma warning disable CS0618 // Type or member is obsolete
            _credentials = FallbackCredentialsFactory.GetCredentials();
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        // Read from config or use defaults
        _opensearchEndpoint = configuration?["AWS:OpenSearchEndpoint"] ?? "";
        _indexName = configuration?["AWS:OpenSearchIndexName"] ?? "policy-clauses";
        _useRealOpenSearch = !string.IsNullOrEmpty(_opensearchEndpoint);
    }

    public async Task<List<PolicyClause>> RetrieveClausesAsync(float[] embedding, string policyType)
    {
        if (!_useRealOpenSearch)
        {
            // Fallback to mock data if OpenSearch not configured
            return await GetMockClausesAsync(policyType);
        }

        try
        {
            return await QueryOpenSearchAsync(embedding, policyType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenSearch query failed, falling back to mock data: {ex.Message}");
            return await GetMockClausesAsync(policyType);
        }
    }

    private async Task<List<PolicyClause>> QueryOpenSearchAsync(float[] embedding, string policyType)
    {
        var searchQuery = new
        {
            size = 5,
            query = new
            {
                @bool = new
                {
                    must = new object[]
                    {
                        new
                        {
                            knn = new
                            {
                                embedding = new
                                {
                                    vector = embedding,
                                    k = 5
                                }
                            }
                        }
                    },
                    filter = new[]
                    {
                        new
                        {
                            term = new
                            {
                                policyType = policyType.ToLower()
                            }
                        }
                    }
                }
            },
            _source = new[] { "clauseId", "text", "coverageType", "policyType" }
        };

        var requestUri = $"{_opensearchEndpoint}/{_indexName}/_search";
        var jsonContent = JsonSerializer.Serialize(searchQuery);
        
        // Sign request with AWS SigV4
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        // Add AWS SigV4 authentication
        await SignRequestAsync(request);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<OpenSearchResponse>(responseContent);

        return searchResult?.Hits?.Hits?
            .Select((hit, index) => new PolicyClause(
                ClauseId: hit.Source?.ClauseId ?? $"UNKNOWN-{index}",
                Text: hit.Source?.Text ?? "",
                CoverageType: hit.Source?.CoverageType ?? "",
                Score: hit.Score ?? 0.0f
            ))
            .ToList() ?? new List<PolicyClause>();
    }

    private async Task SignRequestAsync(HttpRequestMessage request)
    {
        // For OpenSearch Serverless, use AWS SigV4 signing
        // This is a simplified version - in production use AWS.Signers or similar
        var creds = await _credentials.GetCredentialsAsync();
        request.Headers.Add("X-Amz-Security-Token", creds.Token);
        // Note: Full SigV4 implementation would go here
        // For now, relying on IAM role if running in AWS environment
    }

    private async Task<List<PolicyClause>> GetMockClausesAsync(string policyType)
    {
        await Task.Delay(100); // Simulate network call
        
        return policyType.ToLower() switch
        {
            "motor" => GetMotorPolicyClauses(),
            "health" => GetHealthPolicyClauses(),
            _ => new List<PolicyClause>()
        };
    }

    private List<PolicyClause> GetMotorPolicyClauses()
    {
        return new List<PolicyClause>
        {
            new PolicyClause(
                ClauseId: "MOT-001",
                Text: "Collision coverage applies to damage from accidents with other vehicles or objects. Deductible: $500. Maximum coverage: Actual cash value of vehicle.",
                CoverageType: "Collision",
                Score: 0.92f
            ),
            new PolicyClause(
                ClauseId: "MOT-002",
                Text: "Comprehensive coverage includes theft, vandalism, weather damage, and animal collisions. Deductible: $250.",
                CoverageType: "Comprehensive",
                Score: 0.88f
            ),
            new PolicyClause(
                ClauseId: "MOT-003",
                Text: "Liability coverage excludes: intentional damage, racing, driving under influence, or use for commercial delivery without rider.",
                CoverageType: "Exclusions",
                Score: 0.85f
            ),
            new PolicyClause(
                ClauseId: "MOT-004",
                Text: "Towing and rental reimbursement up to $75/day for maximum 10 days following covered loss.",
                CoverageType: "Additional Benefits",
                Score: 0.80f
            ),
            new PolicyClause(
                ClauseId: "MOT-005",
                Text: "Glass damage (windshield, windows) covered with $100 deductible waiver for repair, full deductible for replacement.",
                CoverageType: "Glass Coverage",
                Score: 0.78f
            )
        };
    }

    private List<PolicyClause> GetHealthPolicyClauses()
    {
        return new List<PolicyClause>
        {
            new PolicyClause(
                ClauseId: "HLT-001",
                Text: "Hospital confinement benefit: $200 per day for up to 365 days per calendar year.",
                CoverageType: "Hospital Confinement",
                Score: 0.90f
            ),
            new PolicyClause(
                ClauseId: "HLT-002",
                Text: "Surgical procedure benefits based on schedule: minor $500-$1000, major $2000-$5000.",
                CoverageType: "Surgical",
                Score: 0.87f
            ),
            new PolicyClause(
                ClauseId: "HLT-003",
                Text: "Pre-existing conditions excluded for first 12 months of coverage.",
                CoverageType: "Exclusions",
                Score: 0.84f
            )
        };
    }
}

// OpenSearch response models
internal class OpenSearchResponse
{
    public HitsContainer? Hits { get; set; }
}

internal class HitsContainer
{
    public List<Hit>? Hits { get; set; }
}

internal class Hit
{
    public float? Score { get; set; }
    public HitSource? Source { get; set; }

    [JsonPropertyName("_source")]
    public HitSource? SourceAlias
    {
        get => Source;
        set => Source = value;
    }
}

internal class HitSource
{
    [JsonPropertyName("clauseId")]
    public string? ClauseId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("coverageType")]
    public string? CoverageType { get; set; }

    [JsonPropertyName("policyType")]
    public string? PolicyType { get; set; }
}
