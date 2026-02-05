using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClaimsRagBot.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Tools.PolicyIngestion;

/// <summary>
/// Console application to ingest policy documents into OpenSearch Serverless
/// Usage: dotnet run -- <opensearch-endpoint> [index-name]
/// Example: dotnet run -- https://abc123.us-east-1.aoss.amazonaws.com policy-clauses
/// </summary>
internal class Program
{
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Starting Policy Ingestion Process");
        Console.WriteLine("========================================");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- <opensearch-endpoint> [index-name]");
            Console.WriteLine("Example: dotnet run --  policy-clauses");
            Console.WriteLine("\nNote: AWS credentials will be loaded from appsettings.json or AWS credential chain");
            return;
        }

        var opensearchEndpoint = args[0];
        opensearchEndpoint = "testopensearchEndpoint";
        var indexName = args.Length > 1 ? args[1] : "policy-clauses";

        Console.WriteLine($"OpenSearch Endpoint: {opensearchEndpoint}");
        Console.WriteLine($"Index Name: {indexName}");

        // Build configuration - load from appsettings.json
        var apiProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "src", "ClaimsRagBot.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var region = configuration["AWS:Region"] ?? "us-east-1";
        Console.WriteLine($"Region: {region}\n");

        var ingestionService = new PolicyIngestionService(configuration, opensearchEndpoint, indexName);

        // Step 1: Create index
        Console.WriteLine("Step 1: Creating index 'policy-clauses'...");
        await ingestionService.CreateIndexAsync();
        Console.WriteLine();

        // Step 2: Ingest Life policy clauses
        Console.WriteLine("Step 2: Ingesting Life Insurance clauses...");
        var lifeClauses = PolicyIngestionService.GetSampleLifePolicyClauses();
        await ingestionService.IngestPolicyClausesAsync(lifeClauses);
        Console.WriteLine();

        // Step 3: Ingest Health policy clauses
        Console.WriteLine("Step 3: Ingesting Health Insurance clauses...");
        var healthClauses = PolicyIngestionService.GetSampleHealthPolicyClauses();
        await ingestionService.IngestPolicyClausesAsync(healthClauses);
        Console.WriteLine();

        Console.WriteLine("========================================");
        Console.WriteLine("✓ Policy ingestion completed successfully!");
        Console.WriteLine($"Total clauses indexed: {lifeClauses.Count + healthClauses.Count}");
        Console.WriteLine("========================================");
        Console.WriteLine("\nNext steps:");
        Console.WriteLine("1. Update appsettings.json with OpenSearch endpoint");
        Console.WriteLine("2. Run the API: cd src/ClaimsRagBot.Api && dotnet run");
        Console.WriteLine("3. Test claims validation");
    }
}
