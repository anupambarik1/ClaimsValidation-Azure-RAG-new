using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ClaimsRagBot.Infrastructure.Textract;

public class TextractService : ITextractService
{
    private readonly IAmazonTextract _client;
    private readonly int _pollingIntervalMs;
    private readonly int _maxPollingAttempts;

    public TextractService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];
        
        var config = new AmazonTextractConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonTextractClient(credentials, config);
            Console.WriteLine($"[Textract] Using credentials from appsettings for region: {region}");
        }
        else
        {
            _client = new AmazonTextractClient(config);
            Console.WriteLine($"[Textract] Using default credential chain for region: {region}");
        }
        
        _pollingIntervalMs = int.Parse(configuration["AWS:Textract:PollingIntervalMs"] ?? "5000");
        _maxPollingAttempts = int.Parse(configuration["AWS:Textract:MaxPollingAttempts"] ?? "60");
    }

    public async Task<TextractResult> AnalyzeDocumentAsync(string s3Bucket, string s3Key, string[] featureTypes)
    {
        try
        {
            Console.WriteLine($"[Textract] Starting async document analysis for s3://{s3Bucket}/{s3Key}");
            
            var startRequest = new StartDocumentAnalysisRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new Amazon.Textract.Model.S3Object
                    {
                        Bucket = s3Bucket,
                        Name = s3Key
                    }
                },
                FeatureTypes = featureTypes.ToList()
            };
            
            var startResponse = await _client.StartDocumentAnalysisAsync(startRequest);
            var jobId = startResponse.JobId;
            
            Console.WriteLine($"[Textract] Job started with ID: {jobId}");
            
            // Poll for completion
            GetDocumentAnalysisResponse analysisResponse = null;
            int attempts = 0;
            
            while (attempts < _maxPollingAttempts)
            {
                await Task.Delay(_pollingIntervalMs);
                attempts++;
                
                var getRequest = new GetDocumentAnalysisRequest { JobId = jobId };
                analysisResponse = await _client.GetDocumentAnalysisAsync(getRequest);
                
                Console.WriteLine($"[Textract] Job status: {analysisResponse.JobStatus} (attempt {attempts})");
                
                if (analysisResponse.JobStatus == JobStatus.SUCCEEDED)
                {
                    break;
                }
                else if (analysisResponse.JobStatus == JobStatus.FAILED)
                {
                    throw new Exception($"Textract job failed: {analysisResponse.StatusMessage}");
                }
            }
            
            if (analysisResponse == null || analysisResponse.JobStatus != JobStatus.SUCCEEDED)
            {
                throw new TimeoutException($"Textract job did not complete within {_maxPollingAttempts} attempts");
            }
            
            // Parse results
            var result = ParseTextractResponse(analysisResponse);
            Console.WriteLine($"[Textract] Successfully analyzed document. Confidence: {result.Confidence:F2}");
            
            return result;
        }
        catch (AmazonTextractException ex)
        {
            Console.WriteLine($"[Textract] Error: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"Textract analysis failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<TextractResult> DetectDocumentTextAsync(string s3Bucket, string s3Key)
    {
        try
        {
            Console.WriteLine($"[Textract] Starting text detection for s3://{s3Bucket}/{s3Key}");
            
            var startRequest = new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new Amazon.Textract.Model.S3Object
                    {
                        Bucket = s3Bucket,
                        Name = s3Key
                    }
                }
            };
            
            var startResponse = await _client.StartDocumentTextDetectionAsync(startRequest);
            var jobId = startResponse.JobId;
            
            Console.WriteLine($"[Textract] Text detection job started with ID: {jobId}");
            
            // Poll for completion
            GetDocumentTextDetectionResponse detectionResponse = null;
            int attempts = 0;
            
            while (attempts < _maxPollingAttempts)
            {
                await Task.Delay(_pollingIntervalMs);
                attempts++;
                
                var getRequest = new GetDocumentTextDetectionRequest { JobId = jobId };
                detectionResponse = await _client.GetDocumentTextDetectionAsync(getRequest);
                
                Console.WriteLine($"[Textract] Job status: {detectionResponse.JobStatus} (attempt {attempts})");
                
                if (detectionResponse.JobStatus == JobStatus.SUCCEEDED)
                {
                    break;
                }
                else if (detectionResponse.JobStatus == JobStatus.FAILED)
                {
                    throw new Exception($"Textract job failed: {detectionResponse.StatusMessage}");
                }
            }
            
            if (detectionResponse == null || detectionResponse.JobStatus != JobStatus.SUCCEEDED)
            {
                throw new TimeoutException($"Textract job did not complete within {_maxPollingAttempts} attempts");
            }
            
            // Parse text-only results
            var text = new StringBuilder();
            float totalConfidence = 0;
            int blockCount = 0;
            
            foreach (var block in detectionResponse.Blocks.Where(b => b.BlockType == BlockType.LINE))
            {
                text.AppendLine(block.Text);
                totalConfidence += block.Confidence ?? 0f;
                blockCount++;
            }
            
            var avgConfidence = blockCount > 0 ? totalConfidence / blockCount : 0f;
            
            Console.WriteLine($"[Textract] Successfully detected text. Confidence: {avgConfidence:F2}");
            
            return new TextractResult(
                ExtractedText: text.ToString(),
                KeyValuePairs: new Dictionary<string, string>(),
                Tables: new List<TableData>(),
                Confidence: avgConfidence
            );
        }
        catch (AmazonTextractException ex)
        {
            Console.WriteLine($"[Textract] Error: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"Textract text detection failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    private TextractResult ParseTextractResponse(GetDocumentAnalysisResponse response)
    {
        var text = new StringBuilder();
        var kvPairs = new Dictionary<string, string>();
        var tables = new List<TableData>();
        float totalConfidence = 0;
        int blockCount = 0;
        
        // Extract all text from LINE blocks
        foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.LINE))
        {
            text.AppendLine(block.Text);
            totalConfidence += block.Confidence ?? 0f;
            blockCount++;
        }
        
        // Extract key-value pairs from FORMS
        var keyBlocks = response.Blocks.Where(b => b.BlockType == BlockType.KEY_VALUE_SET && 
                                                     b.EntityTypes.Contains("KEY")).ToList();
        
        foreach (var keyBlock in keyBlocks)
        {
            var keyText = GetTextFromBlock(keyBlock, response.Blocks);
            var valueText = GetValueForKey(keyBlock, response.Blocks);
            
            if (!string.IsNullOrWhiteSpace(keyText) && !string.IsNullOrWhiteSpace(valueText))
            {
                kvPairs[keyText.Trim()] = valueText.Trim();
            }
        }
        
        // Extract tables
        var tableBlocks = response.Blocks.Where(b => b.BlockType == BlockType.TABLE).ToList();
        int tableIndex = 0;
        
        foreach (var tableBlock in tableBlocks)
        {
            var tableData = ParseTable(tableBlock, response.Blocks);
            if (tableData.Rows.Any())
            {
                tables.Add(new TableData(tableIndex++, tableData.Rows, tableBlock.Confidence ?? 0f));
            }
        }
        
        var avgConfidence = blockCount > 0 ? totalConfidence / blockCount : 0f;
        
        return new TextractResult(
            ExtractedText: text.ToString(),
            KeyValuePairs: kvPairs,
            Tables: tables,
            Confidence: avgConfidence
        );
    }

    private string GetTextFromBlock(Block block, List<Block> allBlocks)
    {
        if (block.Relationships == null) return string.Empty;
        
        var childRelationship = block.Relationships.FirstOrDefault(r => r.Type == RelationshipType.CHILD);
        if (childRelationship == null) return string.Empty;
        
        var text = new StringBuilder();
        foreach (var childId in childRelationship.Ids)
        {
            var childBlock = allBlocks.FirstOrDefault(b => b.Id == childId);
            if (childBlock != null && childBlock.BlockType == BlockType.WORD)
            {
                text.Append(childBlock.Text + " ");
            }
        }
        
        return text.ToString().Trim();
    }

    private string GetValueForKey(Block keyBlock, List<Block> allBlocks)
    {
        if (keyBlock.Relationships == null) return string.Empty;
        
        var valueRelationship = keyBlock.Relationships.FirstOrDefault(r => r.Type == RelationshipType.VALUE);
        if (valueRelationship == null) return string.Empty;
        
        foreach (var valueId in valueRelationship.Ids)
        {
            var valueBlock = allBlocks.FirstOrDefault(b => b.Id == valueId);
            if (valueBlock != null)
            {
                return GetTextFromBlock(valueBlock, allBlocks);
            }
        }
        
        return string.Empty;
    }

    private (List<List<string>> Rows, float Confidence) ParseTable(Block tableBlock, List<Block> allBlocks)
    {
        var rows = new List<List<string>>();
        
        if (tableBlock.Relationships == null)
            return (rows, tableBlock.Confidence ?? 0f);
        
        var cellRelationship = tableBlock.Relationships.FirstOrDefault(r => r.Type == RelationshipType.CHILD);
        if (cellRelationship == null)
            return (rows, tableBlock.Confidence ?? 0f);
        
        var cells = cellRelationship.Ids
            .Select(id => allBlocks.FirstOrDefault(b => b.Id == id))
            .Where(b => b != null && b.BlockType == BlockType.CELL)
            .OrderBy(b => b.RowIndex)
            .ThenBy(b => b.ColumnIndex)
            .ToList();
        
        var currentRow = new List<string>();
        int? lastRowIndex = null;
        
        foreach (var cell in cells)
        {
            if (lastRowIndex.HasValue && cell.RowIndex != lastRowIndex)
            {
                rows.Add(currentRow);
                currentRow = new List<string>();
            }
            
            currentRow.Add(GetTextFromBlock(cell, allBlocks));
            lastRowIndex = cell.RowIndex;
        }
        
        if (currentRow.Any())
        {
            rows.Add(currentRow);
        }
        
        return (rows, tableBlock.Confidence ?? 0f);
    }
}
