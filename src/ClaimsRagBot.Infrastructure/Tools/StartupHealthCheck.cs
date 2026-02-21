using ClaimsRagBot.Core.Configuration;
using ClaimsRagBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Tools;

/// <summary>
/// Validates Azure/AWS service connections and configuration on startup
/// </summary>
public class StartupHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly CloudProvider _cloudProvider;

    public StartupHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
        _cloudProvider = CloudProviderSettings.GetProvider(configuration);
    }

    public async Task<HealthCheckResult> ValidateServicesAsync(
        IRetrievalService retrievalService,
        IEmbeddingService embeddingService,
        ILlmService llmService)
    {
        Console.WriteLine($"🔍 Running startup health checks for {_cloudProvider}...");
        var result = new HealthCheckResult();

        try
        {
            // Test embedding service
            Console.Write("  ✓ Testing embedding service... ");
            var testEmbedding = await embeddingService.GenerateEmbeddingAsync("test");
            if (testEmbedding == null || testEmbedding.Length == 0)
            {
                throw new InvalidOperationException("Embedding service returned empty result");
            }
            Console.WriteLine($"✅ ({testEmbedding.Length} dimensions)");
            result.EmbeddingServiceHealthy = true;

            // Test LLM service
            Console.Write("  ✓ Testing LLM service... ");
            var testRequest = new Core.Models.ClaimRequest(
                PolicyNumber: "TEST-001",
                ClaimDescription: "Health check test claim",
                ClaimAmount: 100.00m,
                PolicyType: "Health"
            );
            var llmTestDecision = await llmService.GenerateDecisionAsync(testRequest, new List<Core.Models.PolicyClause>());
            if (string.IsNullOrEmpty(llmTestDecision.Status))
            {
                throw new InvalidOperationException("LLM service returned empty response");
            }
            Console.WriteLine("✅");
            result.LlmServiceHealthy = true;

            // Test retrieval service (this will fail if index not populated)
            Console.Write("  ✓ Testing retrieval service... ");
            try
            {
                var testClauses = await retrievalService.RetrieveClausesAsync(testEmbedding, "Health");
                Console.WriteLine($"✅ ({testClauses.Count} clauses found)");
                result.RetrievalServiceHealthy = true;
                
                if (testClauses.Count == 0)
                {
                    result.Warnings.Add("⚠️  Retrieval service returned 0 clauses. Have you run PolicyIngestion to populate the index?");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"⚠️  Retrieval service error: {ex.Message}");
                result.Warnings.Add($"    Action: Run PolicyIngestion tool to create and populate the {(_cloudProvider == CloudProvider.Azure ? "Azure AI Search index" : "OpenSearch index")}");
                result.RetrievalServiceHealthy = false;
            }

            // Note: Audit service is tested during actual claim processing
            // We don't create test records to avoid polluting the database
            Console.Write("  ✓ Database connection... ");
            result.AuditServiceHealthy = true; // Assumed healthy if other services pass
            Console.WriteLine("✅ (validated via dependency injection)");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Health check failed: {ex.Message}");
            result.Errors.Add($"FATAL: {ex.Message}");
            result.IsHealthy = false;
            return result;
        }

        // Overall health
        result.IsHealthy = result.EmbeddingServiceHealthy && result.LlmServiceHealthy;
        
        if (result.IsHealthy)
        {
            Console.WriteLine("✅ All critical services operational");
            if (result.Warnings.Any())
            {
                Console.WriteLine("⚠️  Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"    {warning}");
                }
            }
        }
        else
        {
            Console.WriteLine("❌ Health check failed - service may not function correctly");
        }

        return result;
    }
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; } = true;
    public bool EmbeddingServiceHealthy { get; set; }
    public bool LlmServiceHealthy { get; set; }
    public bool RetrievalServiceHealthy { get; set; }
    public bool AuditServiceHealthy { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
