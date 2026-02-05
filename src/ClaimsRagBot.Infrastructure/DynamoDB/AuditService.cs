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

        accessKeyId = "testaccesskey";
        secretAccessKey = "testsecretaccesskey";

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

    public async Task<ClaimAuditRecord?> GetByClaimIdAsync(string claimId)
    {
        try
        {
            var request = new GetItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["ClaimId"] = new AttributeValue { S = claimId }
                }
            };

            var response = await _client.GetItemAsync(request);
            
            if (response.Item == null || !response.Item.Any())
            {
                return null;
            }

            return MapToClaimAuditRecord(response.Item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving claim {claimId}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ClaimAuditRecord>> GetByPolicyNumberAsync(string policyNumber)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TableName,
                IndexName = "PolicyNumberIndex", // Requires GSI on PolicyNumber
                KeyConditionExpression = "PolicyNumber = :policyNumber",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":policyNumber"] = new AttributeValue { S = policyNumber }
                },
                ScanIndexForward = false // Most recent first
            };

            var response = await _client.QueryAsync(request);
            
            return response.Items.Select(MapToClaimAuditRecord).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving claims for policy {policyNumber}: {ex.Message}");
            return new List<ClaimAuditRecord>();
        }
    }

    private ClaimAuditRecord MapToClaimAuditRecord(Dictionary<string, AttributeValue> item)
    {
        return new ClaimAuditRecord(
            ClaimId: item["ClaimId"].S,
            Timestamp: DateTime.Parse(item["Timestamp"].S),
            PolicyNumber: item["PolicyNumber"].S,
            ClaimAmount: decimal.Parse(item["ClaimAmount"].N),
            ClaimDescription: item["ClaimDescription"].S,
            DecisionStatus: item["DecisionStatus"].S,
            Explanation: item["Explanation"].S,
            ConfidenceScore: float.Parse(item["ConfidenceScore"].N),
            ClauseReferences: item["ClauseReferences"].L.Select(v => v.S).ToList(),
            RequiredDocuments: item["RequiredDocuments"].L.Select(v => v.S).ToList(),
            SpecialistNotes: item.ContainsKey("SpecialistNotes") ? item["SpecialistNotes"].S : null,
            SpecialistId: item.ContainsKey("SpecialistId") ? item["SpecialistId"].S : null,
            ReviewedAt: item.ContainsKey("ReviewedAt") ? DateTime.Parse(item["ReviewedAt"].S) : null
        );
    }

    public async Task<List<ClaimAuditRecord>> GetAllClaimsAsync(string? statusFilter = null)
    {
        try
        {
            var request = new ScanRequest
            {
                TableName = TableName
            };

            if (!string.IsNullOrEmpty(statusFilter))
            {
                request.FilterExpression = "DecisionStatus = :status";
                request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":status"] = new AttributeValue { S = statusFilter }
                };
            }

            var response = await _client.ScanAsync(request);
            var claims = response.Items.Select(MapToClaimAuditRecord).ToList();
            
            // Sort by timestamp descending (most recent first)
            return claims.OrderByDescending(c => c.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving all claims: {ex.Message}");
            return new List<ClaimAuditRecord>();
        }
    }

    public async Task<bool> UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId)
    {
        try
        {
            var updateRequest = new UpdateItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["ClaimId"] = new AttributeValue { S = claimId }
                },
                UpdateExpression = "SET DecisionStatus = :status, SpecialistNotes = :notes, SpecialistId = :specialistId, ReviewedAt = :reviewedAt",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":status"] = new AttributeValue { S = newStatus },
                    [":notes"] = new AttributeValue { S = specialistNotes },
                    [":specialistId"] = new AttributeValue { S = specialistId },
                    [":reviewedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };

            await _client.UpdateItemAsync(updateRequest);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating claim {claimId}: {ex.Message}");
            return false;
        }
    }
}
