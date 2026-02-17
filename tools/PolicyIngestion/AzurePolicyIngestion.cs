using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Tools.PolicyIngestion;

/// <summary>
/// Azure-specific policy ingestion utility
/// Creates AI Search index and ingests policy documents with embeddings
/// </summary>
public class AzurePolicyIngestion
{
    private readonly IConfiguration _configuration;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly OpenAIClient _openAIClient;
    private readonly string _indexName;
    private readonly string _embeddingDeployment;

    public AzurePolicyIngestion(IConfiguration configuration)
    {
        _configuration = configuration;
        _indexName = configuration["Azure:AISearch:IndexName"] ?? "policy-clauses";
        _embeddingDeployment = configuration["Azure:OpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";

        // Initialize Azure AI Search
        var searchEndpoint = configuration["Azure:AISearch:Endpoint"] 
            ?? throw new ArgumentException("Azure:AISearch:Endpoint not configured");
        var adminKey = configuration["Azure:AISearch:AdminApiKey"] 
            ?? throw new ArgumentException("Azure:AISearch:AdminApiKey not configured");

        var searchCredential = new AzureKeyCredential(adminKey);
        _indexClient = new SearchIndexClient(new Uri(searchEndpoint), searchCredential);
        _searchClient = _indexClient.GetSearchClient(_indexName);

        // Initialize Azure OpenAI
        var openAIEndpoint = configuration["Azure:OpenAI:Endpoint"] 
            ?? throw new ArgumentException("Azure:OpenAI:Endpoint not configured");
        var openAIKey = configuration["Azure:OpenAI:ApiKey"] 
            ?? throw new ArgumentException("Azure:OpenAI:ApiKey not configured");

        _openAIClient = new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
    }

    /// <summary>
    /// Create the AI Search index with vector search configuration
    /// </summary>
    public async Task CreateIndexAsync()
    {
        Console.WriteLine($"Creating AI Search index: {_indexName}");

        // Define the index schema with vector search
        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("ClauseId", SearchFieldDataType.String) { IsKey = true, IsFilterable = false },
                new SearchableField("Text") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField("PolicyType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("CoverageType", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("Section", SearchFieldDataType.String) { IsFilterable = true },
                new VectorSearchField("Embedding", 1536, "vector-profile-1536")
            },
            VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile("vector-profile-1536", "hnsw-algorithm")
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-algorithm")
                    {
                        Parameters = new HnswParameters
                        {
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500,
                            Metric = VectorSearchAlgorithmMetric.Cosine
                        }
                    }
                }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("semantic-config", new()
                    {
                        ContentFields =
                        {
                            new SemanticField("Text")
                        }
                    })
                }
            }
        };

        try
        {
            await _indexClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine($"✓ Index '{_indexName}' created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error creating index: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generate embeddings for policy clauses using Azure OpenAI
    /// </summary>
    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var options = new EmbeddingsOptions(_embeddingDeployment, new[] { text });
        var response = await _openAIClient.GetEmbeddingsAsync(options);
        return response.Value.Data[0].Embedding.ToArray();
    }

    /// <summary>
    /// Ingest policy clauses into AI Search
    /// </summary>
    public async Task IngestPolicyClausesAsync(List<PolicyClause> clauses)
    {
        Console.WriteLine($"Ingesting {clauses.Count} policy clauses...");

        var documents = new List<SearchDocument>();

        // Generate embeddings with progress
        for (int i = 0; i < clauses.Count; i++)
        {
            var clause = clauses[i];

            // Generate embedding
            var embedding = await GenerateEmbeddingAsync(clause.Text);

            var document = new SearchDocument
            {
                ["ClauseId"] = clause.ClauseId,
                ["Text"] = clause.Text,
                ["PolicyType"] = clause.PolicyType,
                ["CoverageType"] = clause.CoverageType,
                ["Section"] = clause.Section,
                ["Embedding"] = embedding
            };

            documents.Add(document);

            // Show progress every 10 documents
            if ((i + 1) % 10 == 0 || i == clauses.Count - 1)
            {
                Console.WriteLine($"Progress: {i + 1}/{clauses.Count} ({(i + 1) * 100 / clauses.Count}%)");
            }

            // Upload in batches of 100
            if (documents.Count >= 100 || i == clauses.Count - 1)
            {
                await UploadDocumentsAsync(documents);
                documents.Clear();
            }
        }

        Console.WriteLine($"✓ Successfully ingested {clauses.Count} clauses");
    }

    /// <summary>
    /// Upload documents to AI Search
    /// </summary>
    private async Task UploadDocumentsAsync(List<SearchDocument> documents)
    {
        if (documents.Count == 0) return;

        try
        {
            var batch = IndexDocumentsBatch.Upload(documents);
            var result = await _searchClient.IndexDocumentsAsync(batch);
            Console.WriteLine($"✓ Uploaded batch of {documents.Count} documents");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error uploading documents: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Process policy documents from TestDocuments folder
    /// </summary>
    public async Task ProcessPolicyDocumentsAsync()
    {
        var testDocsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "TestDocuments");

        if (!Directory.Exists(testDocsPath))
        {
            Console.WriteLine($"✗ TestDocuments folder not found at: {testDocsPath}");
            return;
        }

        Console.WriteLine($"\nProcessing policy documents from: {testDocsPath}");

        var allClauses = new List<PolicyClause>();

        // Process JSON files (structured policy clauses)
        var jsonFiles = Directory.GetFiles(testDocsPath, "*policy*.json");
        foreach (var file in jsonFiles)
        {
            Console.WriteLine($"- Processing JSON: {Path.GetFileName(file)}");
            var clauses = ParseJsonPolicyDocument(file);
            allClauses.AddRange(clauses);
            Console.WriteLine($"  ✓ Extracted {clauses.Count} clauses");
        }

        // Process TXT files (unstructured policy documents)
        var policyFiles = Directory.GetFiles(testDocsPath, "*policy*.txt");
        foreach (var file in policyFiles)
        {
            Console.WriteLine($"- Processing TXT: {Path.GetFileName(file)}");
            var clauses = ParsePolicyDocument(file);
            allClauses.AddRange(clauses);
            Console.WriteLine($"  ✓ Extracted {clauses.Count} clauses");
        }

        Console.WriteLine($"\nTotal clauses extracted: {allClauses.Count}");
        
        if (allClauses.Count > 0)
        {
            await IngestPolicyClausesAsync(allClauses);
        }
        else
        {
            Console.WriteLine("⚠️  No policy clauses found to ingest");
        }
    }

    /// <summary>
    /// Parse a JSON policy document (structured format)
    /// </summary>
    private List<PolicyClause> ParseJsonPolicyDocument(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonClauses = JsonSerializer.Deserialize<List<JsonPolicyClause>>(json);
        
        if (jsonClauses == null)
        {
            Console.WriteLine($"  ⚠️  Failed to parse JSON file");
            return new List<PolicyClause>();
        }

        var clauses = new List<PolicyClause>();
        foreach (var jsonClause in jsonClauses)
        {
            clauses.Add(new PolicyClause
            {
                ClauseId = jsonClause.clauseId ?? $"CLAUSE-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Text = jsonClause.text ?? "",
                PolicyType = jsonClause.policyType ?? "General",
                CoverageType = jsonClause.coverageType ?? "General",
                Section = jsonClause.coverageType ?? "General"
            });
        }

        return clauses;
    }

    /// <summary>
    /// Parse a policy document into clauses
    /// </summary>
    private List<PolicyClause> ParsePolicyDocument(string filePath)
    {
        var clauses = new List<PolicyClause>();
        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Determine policy type from filename
        string policyType = "General";
        if (fileName.Contains("health", StringComparison.OrdinalIgnoreCase))
            policyType = "Health";
        else if (fileName.Contains("life", StringComparison.OrdinalIgnoreCase))
            policyType = "Life";
        else if (fileName.Contains("motor", StringComparison.OrdinalIgnoreCase))
            policyType = "Motor";

        // Split by sections (assuming sections are marked with headers)
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentSection = "General";
        var currentClauseText = new StringBuilder();
        var clauseCounter = 1;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Detect section headers (e.g., "Section 3.2:" or "3.2 Coverage Details")
            if (trimmedLine.StartsWith("Section", StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(trimmedLine, @"^\d+\.\d+"))
            {
                // Save previous clause if exists
                if (currentClauseText.Length > 0)
                {
                    clauses.Add(CreateClause(policyType, currentSection, currentClauseText.ToString(), clauseCounter++));
                    currentClauseText.Clear();
                }

                currentSection = trimmedLine;
            }
            else if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                currentClauseText.AppendLine(trimmedLine);

                // Create a clause every 3-5 sentences
                if (currentClauseText.Length > 300)
                {
                    clauses.Add(CreateClause(policyType, currentSection, currentClauseText.ToString(), clauseCounter++));
                    currentClauseText.Clear();
                }
            }
        }

        // Add final clause
        if (currentClauseText.Length > 0)
        {
            clauses.Add(CreateClause(policyType, currentSection, currentClauseText.ToString(), clauseCounter++));
        }

        return clauses;
    }

    private PolicyClause CreateClause(string policyType, string section, string text, int id)
    {
        return new PolicyClause
        {
            ClauseId = $"CLAUSE-{policyType.ToUpper()}-{id:D4}",
            Text = text.Trim(),
            PolicyType = policyType,
            CoverageType = DetermineCoverageType(text),
            Section = section
        };
    }

    private string DetermineCoverageType(string text)
    {
        var lowerText = text.ToLower();
        if (lowerText.Contains("hospital")) return "Hospitalization";
        if (lowerText.Contains("surgery") || lowerText.Contains("surgical")) return "Surgery";
        if (lowerText.Contains("death") || lowerText.Contains("mortality")) return "Death";
        if (lowerText.Contains("accident") || lowerText.Contains("injury")) return "Accident";
        if (lowerText.Contains("maternity") || lowerText.Contains("pregnancy")) return "Maternity";
        if (lowerText.Contains("dental")) return "Dental";
        if (lowerText.Contains("vision") || lowerText.Contains("optical")) return "Vision";
        return "General";
    }
}

/// <summary>
/// Policy clause model
/// </summary>
public class PolicyClause
{
    public string ClauseId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public string CoverageType { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
}

/// <summary>
/// JSON policy clause model (for deserialization)
/// </summary>
internal class JsonPolicyClause
{
    public string? clauseId { get; set; }
    public string? text { get; set; }
    public string? policyType { get; set; }
    public string? coverageType { get; set; }
}
