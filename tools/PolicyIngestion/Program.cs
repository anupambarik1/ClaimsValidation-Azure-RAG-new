using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClaimsRagBot.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Tools.PolicyIngestion;

/// <summary>
/// Console application to ingest policy documents into OpenSearch Serverless
/// Usage: dotnet run -- <opensearch-endpoint>
/// Example: dotnet run -- https://abc123.us-east-1.aoss.amazonaws.com
/// </summary>
internal class Program
{
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("=== Policy Ingestion Tool ===\n");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- <opensearch-endpoint>");
            Console.WriteLine("Example: dotnet run -- https://abc123.us-east-1.aoss.amazonaws.com");
            Console.WriteLine("\nNote: Ensure AWS credentials are configured (aws configure)");
            return;
        }

        var opensearchEndpoint = args[0];
        var indexName = args.Length > 1 ? args[1] : "policy-clauses";

        Console.WriteLine($"OpenSearch Endpoint: {opensearchEndpoint}");
        Console.WriteLine($"Index Name: {indexName}\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:Region"] = "us-east-1"
            })
            .Build();

        var ingestionService = new PolicyIngestionService(configuration, opensearchEndpoint, indexName);

        // Step 1: Create index
        Console.WriteLine("Step 1: Creating OpenSearch index...");
        await ingestionService.CreateIndexAsync();
        Console.WriteLine();

        // Step 2: Ingest Motor policy clauses
        Console.WriteLine("Step 2: Ingesting Motor Insurance clauses...");
        var motorClauses = PolicyIngestionService.GetSampleMotorPolicyClauses();
        await ingestionService.IngestPolicyClausesAsync(motorClauses);
        Console.WriteLine();

        // Step 3: Ingest Health policy clauses
        Console.WriteLine("Step 3: Ingesting Health Insurance clauses...");
        var healthClauses = PolicyIngestionService.GetSampleHealthPolicyClauses();
        await ingestionService.IngestPolicyClausesAsync(healthClauses);
        Console.WriteLine();

        Console.WriteLine("✓ Policy ingestion completed!");
        Console.WriteLine("\nNext steps:");
        Console.WriteLine("1. Update appsettings.json with OpenSearch endpoint");
        Console.WriteLine("2. Run the API: cd src/ClaimsRagBot.Api && dotnet run");
        Console.WriteLine("3. Test claims validation");
    }
}
