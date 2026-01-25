using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Infrastructure.DynamoDB;

public class AuditService : IAuditService
{
    private readonly AmazonDynamoDBClient _client;
    private const string TableName = "ClaimsAuditTrail";

    public AuditService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];
        
        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonDynamoDBClient(credentials, config);
        }
        else
        {
            // Fallback to default credential chain
            _client = new AmazonDynamoDBClient(config);
        }
    }

    public async Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses)
    {
        var auditRecord = new Dictionary<string, AttributeValue>
        {
            ["ClaimId"] = new AttributeValue { S = Guid.NewGuid().ToString() },
            ["Timestamp"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["PolicyNumber"] = new AttributeValue { S = request.PolicyNumber },
            ["ClaimAmount"] = new AttributeValue { N = request.ClaimAmount.ToString() },
            ["ClaimDescription"] = new AttributeValue { S = request.ClaimDescription },
            ["DecisionStatus"] = new AttributeValue { S = decision.Status },
            ["Explanation"] = new AttributeValue { S = decision.Explanation },
            ["ConfidenceScore"] = new AttributeValue { N = decision.ConfidenceScore.ToString() },
            ["ClauseReferences"] = new AttributeValue 
            { 
                L = decision.ClauseReferences.Select(c => new AttributeValue { S = c }).ToList() 
            },
            ["RequiredDocuments"] = new AttributeValue 
            { 
                L = decision.RequiredDocuments.Select(d => new AttributeValue { S = d }).ToList() 
            },
            ["RetrievedClauses"] = new AttributeValue 
            { 
                S = JsonSerializer.Serialize(clauses.Select(c => new { c.ClauseId, c.Score })) 
            }
        };

        try
        {
            var putRequest = new PutItemRequest
            {
                TableName = TableName,
                Item = auditRecord
            };

            await _client.PutItemAsync(putRequest);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the claim validation
            Console.WriteLine($"Audit save failed: {ex.Message}");
            // In production, use proper logging framework
        }
    }
}
