using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure Document Intelligence (Form Recognizer) implementation for OCR
/// </summary>
public class AzureDocumentIntelligenceService : ITextractService
{
    private readonly DocumentAnalysisClient _client;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _modelId;
    private readonly string _containerName;

    public AzureDocumentIntelligenceService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:DocumentIntelligence:Endpoint"] 
            ?? throw new ArgumentException("Azure:DocumentIntelligence:Endpoint not configured");
        var apiKey = configuration["Azure:DocumentIntelligence:ApiKey"] 
            ?? throw new ArgumentException("Azure:DocumentIntelligence:ApiKey not configured");
        
        _modelId = configuration["Azure:DocumentIntelligence:ModelId"] ?? "prebuilt-document";
        
        _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        // Initialize Blob Storage client for generating SAS URLs
        var connectionString = configuration["Azure:BlobStorage:ConnectionString"] 
            ?? throw new ArgumentException("Azure:BlobStorage:ConnectionString not configured");
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "claims-documents";
        
        _blobServiceClient = new BlobServiceClient(connectionString);
        
        Console.WriteLine($"[DocumentIntelligence] Using model: {_modelId}");
    }

    public async Task<TextractResult> AnalyzeDocumentAsync(string s3Bucket, string s3Key, string[] featureTypes)
    {
        return await ExtractTextAsync(s3Bucket, s3Key);
    }

    public async Task<TextractResult> DetectDocumentTextAsync(string s3Bucket, string s3Key)
    {
        return await ExtractTextAsync(s3Bucket, s3Key);
    }

    private async Task<TextractResult> ExtractTextAsync(string s3Bucket, string s3Key)
    {
        try
        {
            // Generate SAS URL for the blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(s3Key);
            
            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                throw new InvalidOperationException($"Blob not found: {s3Key}");
            }
            
            // Generate SAS token with read permissions (valid for 1 hour)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = s3Key,
                Resource = "b", // blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            
            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            
            Console.WriteLine($"[DocumentIntelligence] Analyzing document from blob: {s3Key}");
            
            var operation = await _client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, _modelId, sasToken);
            var result = operation.Value;

            var extractedText = new StringBuilder();
            var keyValuePairs = new Dictionary<string, string>();
            var tables = new List<TableData>();

            // Extract text from all pages
            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    extractedText.AppendLine(line.Content);
                }
            }

            // Extract key-value pairs from forms
            foreach (var kvp in result.KeyValuePairs)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    keyValuePairs[kvp.Key.Content] = kvp.Value.Content;
                }
            }

            // Extract tables
            for (int tableIndex = 0; tableIndex < result.Tables.Count; tableIndex++)
            {
                var table = result.Tables[tableIndex];
                var rows = new List<List<string>>();
                
                // Group cells by row
                var cellsByRow = table.Cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key);
                
                foreach (var rowGroup in cellsByRow)
                {
                    var row = rowGroup.OrderBy(c => c.ColumnIndex).Select(c => c.Content).ToList();
                    rows.Add(row);
                }

                tables.Add(new TableData(tableIndex, rows, 0.95f));
            }

            Console.WriteLine($"[DocumentIntelligence] Extracted text from {result.Pages.Count} page(s)");

            return new TextractResult(
                extractedText.ToString(),
                keyValuePairs,
                tables,
                0.95f
            );
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[DocumentIntelligence] Error: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Document Intelligence extraction failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentIntelligence] Unexpected error: {ex.Message}");
            throw new InvalidOperationException($"Document Intelligence extraction failed: {ex.Message}", ex);
        }
    }
}