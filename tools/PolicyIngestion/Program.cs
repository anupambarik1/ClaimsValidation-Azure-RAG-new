using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClaimsRagBot.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Tools.PolicyIngestion;

/// <summary>
/// Console application to ingest policy documents into Azure AI Search or AWS OpenSearch
/// Usage: dotnet run
/// Configuration is read from appsettings.json (local) or src/ClaimsRagBot.Api/appsettings.json
/// </summary>
internal class Program
{
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Policy Ingestion Tool");
        Console.WriteLine("========================================\n");

        // Build configuration - try local appsettings.json first, then API project
        var localConfigPath = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "src", "ClaimsRagBot.Api");

        IConfiguration? configuration = null;

        // Try local appsettings.json first
        if (File.Exists(Path.Combine(localConfigPath, "appsettings.json")))
        {
            Console.WriteLine($"📄 Loading configuration from: {localConfigPath}\\appsettings.json");
            configuration = new ConfigurationBuilder()
                .SetBasePath(localConfigPath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }
        // Fall back to API project appsettings.json
        else if (File.Exists(Path.Combine(apiProjectPath, "appsettings.json")))
        {
            Console.WriteLine($"📄 Loading configuration from: {apiProjectPath}\\appsettings.json");
            configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
        }
        else
        {
            Console.WriteLine("❌ Error: No appsettings.json found!");
            Console.WriteLine($"\nSearched locations:");
            Console.WriteLine($"  1. {localConfigPath}\\appsettings.json");
            Console.WriteLine($"  2. {apiProjectPath}\\appsettings.json");
            Console.WriteLine($"\nPlease create appsettings.json in the PolicyIngestion folder with Azure or AWS configuration.");
            return;
        }

        var cloudProvider = configuration["CloudProvider"] ?? "AWS";
        Console.WriteLine($"🌩️  Cloud Provider: {cloudProvider}\n");

        try
        {
            if (cloudProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                await RunAzureIngestionAsync(configuration);
            }
            else if (cloudProvider.Equals("AWS", StringComparison.OrdinalIgnoreCase))
            {
                await RunAwsIngestionAsync(configuration, args);
            }
            else
            {
                Console.WriteLine($"❌ Unsupported CloudProvider: {cloudProvider}");
                Console.WriteLine("Supported values: Azure, AWS");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error during ingestion: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static async Task RunAzureIngestionAsync(IConfiguration configuration)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Azure AI Search Policy Ingestion");
        Console.WriteLine("========================================\n");

        var endpoint = configuration["Azure:AISearch:Endpoint"];
        var indexName = configuration["Azure:AISearch:IndexName"] ?? "policy-clauses";

        Console.WriteLine($"AI Search Endpoint: {endpoint}");
        Console.WriteLine($"Index Name: {indexName}\n");

        var ingestion = new AzurePolicyIngestion(configuration);

        // Step 1: Create index
        Console.WriteLine("Step 1: Creating AI Search index...");
        await ingestion.CreateIndexAsync();
        Console.WriteLine();

        // Step 2: Process and ingest policy documents
        Console.WriteLine("Step 2: Processing policy documents from TestDocuments/...");
        await ingestion.ProcessPolicyDocumentsAsync();
        Console.WriteLine();

        Console.WriteLine("========================================");
        Console.WriteLine("✅ Policy ingestion completed successfully!");
        Console.WriteLine("========================================");
        Console.WriteLine("\nNext steps:");
        Console.WriteLine("1. Verify index in Azure Portal → AI Search → Indexes");
        Console.WriteLine("2. Update src/ClaimsRagBot.Api/appsettings.json with CloudProvider='Azure'");
        Console.WriteLine("3. Run the API: cd src/ClaimsRagBot.Api && dotnet run");
        Console.WriteLine("4. Test claims validation");
    }

    private static async Task RunAwsIngestionAsync(IConfiguration configuration, string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("AWS OpenSearch Policy Ingestion");
        Console.WriteLine("========================================\n");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- <opensearch-endpoint> [index-name]");
            Console.WriteLine("Example: dotnet run -- https://abc123.us-east-1.aoss.amazonaws.com policy-clauses");
            Console.WriteLine("\nNote: AWS credentials will be loaded from appsettings.json or AWS credential chain");
            return;
        }

        var opensearchEndpoint = args[0];
        var indexName = args.Length > 1 ? args[1] : "policy-clauses";

        Console.WriteLine($"OpenSearch Endpoint: {opensearchEndpoint}");
        Console.WriteLine($"Index Name: {indexName}");

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
