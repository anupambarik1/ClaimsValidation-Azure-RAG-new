using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Infrastructure.Bedrock;
using ClaimsRagBot.Infrastructure.OpenSearch;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Tools;

/// <summary>
/// Utility to ingest policy documents into OpenSearch Serverless
/// </summary>
public class PolicyIngestionService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly HttpClient _httpClient;
    private readonly string _opensearchEndpoint;
    private readonly string _indexName;
    private readonly AWSCredentials _credentials;
    private readonly string _region;

    public PolicyIngestionService(IConfiguration configuration, string opensearchEndpoint, string indexName = "policy-clauses")
    {
        _embeddingService = new EmbeddingService(configuration);
        _httpClient = new HttpClient();
        _opensearchEndpoint = opensearchEndpoint;
        _indexName = indexName;
        _region = configuration["AWS:Region"] ?? "us-east-1";
        
        // Get AWS credentials
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            _credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        }
        else
        {
#pragma warning disable CS0618
            _credentials = FallbackCredentialsFactory.GetCredentials();
#pragma warning restore CS0618
        }
    }

    public async Task CreateIndexAsync()
    {
        var indexMapping = new
        {
            mappings = new
            {
                properties = new
                {
                    clauseId = new { type = "keyword" },
                    text = new { type = "text" },
                    coverageType = new { type = "keyword" },
                    policyType = new { type = "keyword" },
                    embedding = new
                    {
                        type = "knn_vector",
                        dimension = 1536, // Titan embeddings dimension
                        method = new
                        {
                            name = "hnsw",
                            space_type = "l2",
                            engine = "nmslib"
                        }
                    }
                }
            },
            settings = new
            {
                index = new
                {
                    knn = true
                }
            }
        };

        var requestUri = $"{_opensearchEndpoint}/{_indexName}";
        var jsonContent = JsonSerializer.Serialize(indexMapping);

        var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        await SignRequestAsync(request);
        var response = await _httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✓ Index '{_indexName}' created successfully");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✗ Index creation failed: {error}");
        }
    }

    public async Task IngestPolicyClausesAsync(List<PolicyClauseDocument> clauses)
    {
        Console.WriteLine($"Ingesting {clauses.Count} policy clauses...");
        int successCount = 0;

        foreach (var clause in clauses)
        {
            try
            {
                // Generate embedding for the clause text
                var embedding = await _embeddingService.GenerateEmbeddingAsync(clause.Text);

                // Create document with embedding
                var document = new
                {
                    clauseId = clause.ClauseId,
                    text = clause.Text,
                    coverageType = clause.CoverageType,
                    policyType = clause.PolicyType,
                    embedding = embedding
                };

                // Index document - Use POST to auto-generate ID, then update with specific ID
                var requestUri = $"{_opensearchEndpoint}/{_indexName}/_doc";
                var jsonContent = JsonSerializer.Serialize(document);

                var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                await SignRequestAsync(request);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    Console.WriteLine($"✓ Indexed: {clause.ClauseId}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✗ Failed to index {clause.ClauseId}: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error ingesting {clause.ClauseId}: {ex.Message}");
            }
        }

        Console.WriteLine($"\nIngestion complete: {successCount}/{clauses.Count} clauses indexed");
    }

    private async Task SignRequestAsync(HttpRequestMessage request)
    {
        var creds = await _credentials.GetCredentialsAsync();
        var requestDateTime = DateTime.UtcNow;
        var dateStamp = requestDateTime.ToString("yyyyMMdd");
        var amzDate = requestDateTime.ToString("yyyyMMddTHHmmssZ");
        
        // Read request body
        var requestBody = string.Empty;
        if (request.Content != null)
        {
            requestBody = await request.Content.ReadAsStringAsync();
        }
        
        var payloadHash = HashSHA256(requestBody);
        
        // Create canonical request
        var canonicalUri = request.RequestUri!.AbsolutePath;
        var canonicalQuerystring = "";
        var canonicalHeaders = $"host:{request.RequestUri.Host}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-date";
        
        var canonicalRequest = $"{request.Method}\n{canonicalUri}\n{canonicalQuerystring}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        
        // Create string to sign
        var credentialScope = $"{dateStamp}/{_region}/aoss/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{HashSHA256(canonicalRequest)}";
        
        // Calculate signature
        var signingKey = GetSignatureKey(creds.SecretKey, dateStamp, _region, "aoss");
        var signature = ToHexString(HmacSHA256(signingKey, stringToSign));
        
        // Add authorization header
        var authorizationHeader = $"AWS4-HMAC-SHA256 Credential={creds.AccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        
        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        
        if (!string.IsNullOrEmpty(creds.Token))
        {
            request.Headers.TryAddWithoutValidation("X-Amz-Security-Token", creds.Token);
        }
    }
    
    private static string HashSHA256(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return ToHexString(hash);
    }
    
    private static byte[] HmacSHA256(byte[] key, string data)
    {
        var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
    
    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kDate = HmacSHA256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
        var kRegion = HmacSHA256(kDate, regionName);
        var kService = HmacSHA256(kRegion, serviceName);
        return HmacSHA256(kService, "aws4_request");
    }
    
    private static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    public static List<PolicyClauseDocument> GetSampleMotorPolicyClauses()
    {
        return new List<PolicyClauseDocument>
        {
            new("MOT-001",
                "Collision coverage applies to damage from accidents with other vehicles or objects. Deductible: $500. Maximum coverage: Actual cash value of vehicle.",
                "Collision", "motor"),
            new("MOT-002",
                "Comprehensive coverage includes theft, vandalism, weather damage, and animal collisions. Deductible: $250.",
                "Comprehensive", "motor"),
            new("MOT-003",
                "Liability coverage excludes: intentional damage, racing, driving under influence, or use for commercial delivery without rider.",
                "Exclusions", "motor"),
            new("MOT-004",
                "Towing and rental reimbursement up to $75/day for maximum 10 days following covered loss.",
                "Additional Benefits", "motor"),
            new("MOT-005",
                "Glass damage (windshield, windows) covered with $100 deductible waiver for repair, full deductible for replacement.",
                "Glass Coverage", "motor"),
            new("MOT-006",
                "Uninsured motorist coverage protects you when the at-fault driver has no insurance. Covers medical expenses and property damage.",
                "Uninsured Motorist", "motor"),
            new("MOT-007",
                "Personal injury protection (PIP) covers medical expenses regardless of fault. Includes lost wages and rehabilitation costs.",
                "Personal Injury", "motor"),
            new("MOT-008",
                "Roadside assistance includes flat tire service, battery jump-start, lockout service, and fuel delivery. Available 24/7.",
                "Roadside Assistance", "motor"),
            new("MOT-009",
                "Custom parts and equipment coverage for aftermarket modifications. Must be declared and documented prior to claim.",
                "Custom Equipment", "motor"),
            new("MOT-010",
                "Gap insurance covers the difference between actual cash value and loan balance in total loss situations.",
                "Gap Coverage", "motor")
        };
    }

    public static List<PolicyClauseDocument> GetSampleHealthPolicyClauses()
    {
        return new List<PolicyClauseDocument>
        {
            new("HLT-001",
                "Hospital confinement benefit: $200 per day for up to 365 days per calendar year.",
                "Hospital Confinement", "health"),
            new("HLT-002",
                "Surgical procedure benefits based on schedule: minor $500-$1000, major $2000-$5000.",
                "Surgical", "health"),
            new("HLT-003",
                "Pre-existing conditions excluded for first 12 months of coverage.",
                "Exclusions", "health"),
            new("HLT-004",
                "Intensive care unit (ICU) benefit: $400 per day for up to 30 days per confinement.",
                "ICU Coverage", "health"),
            new("HLT-005",
                "Outpatient surgery benefit: $150 per procedure when performed in ambulatory surgical center.",
                "Outpatient Surgery", "health"),
            new("HLT-006",
                "Emergency room visit benefit: $100 per visit for accidental injury, $75 for illness.",
                "Emergency Room", "health"),
            new("HLT-007",
                "Diagnostic imaging benefit: $50 for X-rays, $100 for CT/MRI scans when hospitalized.",
                "Diagnostic Imaging", "health"),
            new("HLT-008",
                "Cancer diagnosis benefit: Lump sum $10,000 upon first diagnosis of invasive cancer.",
                "Cancer Coverage", "health")
        };
    }
}

public record PolicyClauseDocument(
    string ClauseId,
    string Text,
    string CoverageType,
    string PolicyType
);
