using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using OpenSearch.Client;
using OpenSearch.Net.Auth.AwsSigV4;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        try
        {
            _httpClient = new HttpClient();
            
            var accessKeyId = configuration?["AWS:AccessKeyId"];
            var secretAccessKey = configuration?["AWS:SecretAccessKey"];

            // accessKeyId = "testaccesskey";
            // secretAccessKey = "testsecretaccesskey";

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

            _opensearchEndpoint = "testopensearchEndpoint";


            _useRealOpenSearch = !string.IsNullOrEmpty(_opensearchEndpoint);
            
            Console.WriteLine($"[RetrievalService] Endpoint: {_opensearchEndpoint}");
            Console.WriteLine($"[RetrievalService] Index: {_indexName}");
            Console.WriteLine($"[RetrievalService] Using Real OpenSearch: {_useRealOpenSearch}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RetrievalService] FATAL: Constructor failed - {ex.Message}");
            Console.WriteLine($"[RetrievalService] Stack: {ex.StackTrace}");
            throw;
        }
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
            Console.WriteLine($"[RetrievalService] Querying OpenSearch for policyType: {policyType}");
            var result = await QueryOpenSearchAsync(embedding, policyType);
            Console.WriteLine($"[RetrievalService] ✓ Retrieved {result.Count} clauses from OpenSearch");
            return result;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[RetrievalService] ✗ HTTP Error: {ex.StatusCode} - {ex.Message}");
            Console.WriteLine($"[RetrievalService] Falling back to mock data");
            return await GetMockClausesAsync(policyType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RetrievalService] ✗ Error: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[RetrievalService] Stack: {ex.StackTrace}");
            Console.WriteLine($"[RetrievalService] Falling back to mock data");
            return await GetMockClausesAsync(policyType);
        }
    }

    private async Task<List<PolicyClause>> QueryOpenSearchAsync(float[] embedding, string policyType)
    {

        // 1. Setup the connection for SERVERLESS (must use "aoss")
        var connection = new AwsSigV4HttpConnection(RegionEndpoint.USEast1, service: "aoss");
        var settings = new ConnectionSettings(new Uri("testopensearchEndpoint"), connection)
            .DefaultIndex("policy-clauses");

        var client = new OpenSearchClient(settings);

        // 2. Perform the search using the high-level client
        var searchResponse = await client.SearchAsync<dynamic>(s => s
            .Index("policy-clauses")
            .Size(5)
            .Query(q => q.MatchAll())
            .Source(sr => sr
                .Includes(f => f
                    .Fields("clauseId", "text", "coverageType", "policyType")
                )
            )
        );

        if (searchResponse.IsValid)
        {
            var documents = searchResponse.Documents; // Your results

            foreach (var hit in searchResponse.Hits)
            {
                // Access the fields from the dynamic Source object
                var clauseId = hit.Source["clauseId"];
                var text = hit.Source["text"];
                var coverageType = hit.Source["coverageType"];
                var policyType1 = hit.Source["policyType"];

                // Your logic here (e.g., printing to console or mapping to a list)
                Console.WriteLine($"ID: {clauseId} | Type: {policyType1}");
                Console.WriteLine($"Text: {text}");
                Console.WriteLine(new string('-', 20));
            }

            return searchResponse?.Hits?
            .Select((hit, index) => new PolicyClause(
                ClauseId: hit.Source?["clauseId"] ?? $"UNKNOWN-{index}",
                Text: hit.Source?["text"] ?? "",
                CoverageType: hit.Source?["coverageType"] ?? "",
                Score: hit.Score!=null? (float)hit.Score : 0.0f
            ))
            .ToList() ?? new List<PolicyClause>();
        }
        else
        {
            // If you get a 404 here, double check that _indexName exactly matches your index in AWS
            Console.WriteLine(searchResponse.DebugInformation);
        }




        //// 1. Define your Serverless collection details
        //var endpoint = new Uri("testopensearchEndpoint");
        //var region = RegionEndpoint.USEast1;

        //// 2. Configure the SigV4 Connection specifically for Serverless
        //// IMPORTANT: You MUST specify "aoss" as the service name for Serverless collections.
        //var connection = new AwsSigV4HttpConnection(region, service: "aoss");

        //// 3. Setup Client Settings
        //var settings = new ConnectionSettings(endpoint, connection)
        //    .DefaultIndex("bedrock-knowledge-base-default-index")
        //    .EnableDebugMode(); // Helpful to see signing headers in logs

        //// 4. Instantiate the Client
        //var client = new OpenSearchClient(settings);

        //// Attempt to list aliases or indices to verify the connection and permissions
        //var response1 = await client.Cat.IndicesAsync();


        //if (response1.IsValid)
        //{
        //    Console.WriteLine("Connected successfully!");
        //}
        //else
        //{
        //    // If this fails with 403, it's a permission/policy issue.
        //    // If it still fails with 404, check your endpoint URL for typos.
        //    Console.WriteLine($"Connection failed: {response1.DebugInformation}");
        //}





        // 1. Define your Serverless collection details
        var endpoint = new Uri("testopensearchEndpoint");
        var region = RegionEndpoint.USEast1;

        // 2. Configure the SigV4 Connection specifically for Serverless
        // IMPORTANT: You MUST specify "aoss" as the service name for Serverless collections.
        var connection1 = new AwsSigV4HttpConnection(region, service: "aoss");

        // 3. Setup Client Settings
        var settings1 = new ConnectionSettings(endpoint, connection)
            .DefaultIndex("bedrock-knowledge-base-default-index")
            .EnableDebugMode(); // Helpful to see signing headers in logs

        // 4. Instantiate the Client
        var client1 = new OpenSearchClient(settings);

        var searchQuery = new
        {
            size = 5,
            query = new
            {
                match_all = new { }
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
        //await SignRequestAsync(request);

        var response = await _httpClient.SendAsync(request);
       // response.EnsureSuccessStatusCode();

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
        var creds = await _credentials.GetCredentialsAsync();
        var uri = request.RequestUri!;
        var region = "us-east-1";
        var service = "aoss";
        
        byte[] contentBytes = Array.Empty<byte>();
        string contentHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        string contentType = "";
        
        if (request.Content != null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync();
            contentHash = ComputeSha256Hash(contentBytes);
            contentType = request.Content.Headers.ContentType?.ToString() ?? "application/json";
        }
        
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var dateStamp = now.ToString("yyyyMMdd");
        
        request.Headers.Host = uri.Host;
        request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);
        
        if (!string.IsNullOrEmpty(creds.Token))
        {
            request.Headers.TryAddWithoutValidation("X-Amz-Security-Token", creds.Token);
        }
        
        // Build canonical headers (must be sorted alphabetically)
        var canonicalHeaders = new StringBuilder();
        if (!string.IsNullOrEmpty(contentType))
        {
            canonicalHeaders.Append($"content-type:{contentType}\n");
        }
        canonicalHeaders.Append($"host:{uri.Host}\n");
        canonicalHeaders.Append($"x-amz-date:{amzDate}\n");
        if (!string.IsNullOrEmpty(creds.Token))
        {
            canonicalHeaders.Append($"x-amz-security-token:{creds.Token}\n");
        }
        
        // Build signed headers list (must match canonical headers, sorted)
        var signedHeadersList = new List<string>();
        if (!string.IsNullOrEmpty(contentType))
        {
            signedHeadersList.Add("content-type");
        }
        signedHeadersList.Add("host");
        signedHeadersList.Add("x-amz-date");
        if (!string.IsNullOrEmpty(creds.Token))
        {
            signedHeadersList.Add("x-amz-security-token");
        }
        var signedHeaders = string.Join(";", signedHeadersList);
        
        var canonicalRequest = new StringBuilder();
        canonicalRequest.Append($"{request.Method.Method}\n");
        canonicalRequest.Append($"{uri.AbsolutePath}\n");
        canonicalRequest.Append($"{uri.Query.TrimStart('?')}\n");
        canonicalRequest.Append(canonicalHeaders.ToString());
        canonicalRequest.Append($"\n{signedHeaders}\n");
        canonicalRequest.Append(contentHash);
        
        var canonicalRequestHash = ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest.ToString()));
        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{canonicalRequestHash}";
        
        var signingKey = GetSignatureKey(creds.SecretKey, dateStamp, region, service);
        var signature = ToHexString(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));
        
        var authorizationHeader = $"AWS4-HMAC-SHA256 Credential={creds.AccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
    }
    
    private static string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return ToHexString(hash);
    }
    
    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(data);
    }
    
    private static byte[] GetSignatureKey(string key, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{key}"), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        return HmacSha256(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }
    
    private static string ToHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
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
