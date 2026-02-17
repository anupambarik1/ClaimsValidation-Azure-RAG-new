using Microsoft.Azure.Cosmos;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure Cosmos DB implementation for claims audit trail
/// </summary>
public class AzureCosmosAuditService : IAuditService
{
    private readonly CosmosClient _client;
    private readonly Container _container;

    public AzureCosmosAuditService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:CosmosDB:Endpoint"] 
            ?? throw new ArgumentException("Azure:CosmosDB:Endpoint not configured");
        var key = configuration["Azure:CosmosDB:Key"] 
            ?? throw new ArgumentException("Azure:CosmosDB:Key not configured");
        var databaseId = configuration["Azure:CosmosDB:DatabaseId"] ?? "ClaimsDatabase";
        var containerId = configuration["Azure:CosmosDB:ContainerId"] ?? "AuditTrail";

        _client = new CosmosClient(endpoint, key);
        _container = _client.GetContainer(databaseId, containerId);
        
        Console.WriteLine($"[CosmosDB] Connected to {databaseId}/{containerId}");
    }

    public async Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> retrievedClauses)
    {
        var claimId = Guid.NewGuid().ToString();
        var auditRecord = new ClaimAuditRecord(
            ClaimId: claimId,
            Timestamp: DateTime.UtcNow,
            PolicyNumber: request.PolicyNumber,
            ClaimAmount: request.ClaimAmount,
            ClaimDescription: request.ClaimDescription,
            DecisionStatus: decision.Status,
            Explanation: decision.Explanation,
            ConfidenceScore: decision.ConfidenceScore,
            ClauseReferences: decision.ClauseReferences,
            RequiredDocuments: decision.RequiredDocuments
        );

        // Create Cosmos document with lowercase 'id' for compatibility
        var cosmosDoc = new
        {
            id = claimId,
            auditRecord.ClaimId,
            auditRecord.Timestamp,
            auditRecord.PolicyNumber,
            auditRecord.ClaimAmount,
            auditRecord.ClaimDescription,
            auditRecord.DecisionStatus,
            auditRecord.Explanation,
            auditRecord.ConfidenceScore,
            auditRecord.ClauseReferences,
            auditRecord.RequiredDocuments
        };

        try
        {
            await _container.CreateItemAsync(cosmosDoc, new PartitionKey(request.PolicyNumber));
            Console.WriteLine($"[CosmosDB] Saved audit record: {claimId}");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.StatusCode} - {ex.Message}");
            throw new InvalidOperationException($"Cosmos DB save failed: {ex.Message}", ex);
        }
    }

    public async Task<ClaimAuditRecord?> GetByClaimIdAsync(string claimId)
    {
        try
        {
            var query = $"SELECT * FROM c WHERE c.ClaimId = '{claimId}'";
            var iterator = _container.GetItemQueryIterator<CosmosAuditRecord>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var doc = response.FirstOrDefault();
                if (doc != null)
                    return MapToAuditRecord(doc);
            }
            
            return null;
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.StatusCode} - {ex.Message}");
            return null;
        }
    }

    public async Task<List<ClaimAuditRecord>> GetByPolicyNumberAsync(string policyNumber)
    {
        try
        {
            var query = $"SELECT * FROM c WHERE c.PolicyNumber = '{policyNumber}'";
            var iterator = _container.GetItemQueryIterator<CosmosAuditRecord>(query);
            var records = new List<ClaimAuditRecord>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                records.AddRange(response.Select(MapToAuditRecord));
            }
            
            return records;
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.StatusCode} - {ex.Message}");
            return new List<ClaimAuditRecord>();
        }
    }

    public async Task<List<ClaimAuditRecord>> GetAllClaimsAsync(string? statusFilter = null)
    {
        try
        {
            var query = statusFilter == null 
                ? "SELECT * FROM c" 
                : $"SELECT * FROM c WHERE c.DecisionStatus = '{statusFilter}'";
                
            var iterator = _container.GetItemQueryIterator<CosmosAuditRecord>(query);
            var records = new List<ClaimAuditRecord>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                records.AddRange(response.Select(MapToAuditRecord));
            }
            
            return records;
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.StatusCode} - {ex.Message}");
            return new List<ClaimAuditRecord>();
        }
    }

    public async Task<bool> UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId)
    {
        try
        {
            var existingRecord = await GetByClaimIdAsync(claimId);
            if (existingRecord == null)
                return false;

            var updatedRecord = existingRecord with
            {
                DecisionStatus = newStatus,
                SpecialistNotes = specialistNotes,
                SpecialistId = specialistId,
                ReviewedAt = DateTime.UtcNow
            };

            var cosmosDoc = new
            {
                id = claimId,
                updatedRecord.ClaimId,
                updatedRecord.Timestamp,
                updatedRecord.PolicyNumber,
                updatedRecord.ClaimAmount,
                updatedRecord.ClaimDescription,
                updatedRecord.DecisionStatus,
                updatedRecord.Explanation,
                updatedRecord.ConfidenceScore,
                updatedRecord.ClauseReferences,
                updatedRecord.RequiredDocuments,
                updatedRecord.SpecialistNotes,
                updatedRecord.SpecialistId,
                updatedRecord.ReviewedAt
            };

            await _container.UpsertItemAsync(cosmosDoc, new PartitionKey(existingRecord.PolicyNumber));
            Console.WriteLine($"[CosmosDB] Updated claim: {claimId}");
            return true;
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.StatusCode} - {ex.Message}");
            return false;
        }
    }

    private ClaimAuditRecord MapToAuditRecord(CosmosAuditRecord doc)
    {
        return new ClaimAuditRecord(
            doc.ClaimId,
            doc.Timestamp,
            doc.PolicyNumber,
            doc.ClaimAmount,
            doc.ClaimDescription,
            doc.DecisionStatus,
            doc.Explanation,
            doc.ConfidenceScore,
            doc.ClauseReferences,
            doc.RequiredDocuments,
            doc.SpecialistNotes,
            doc.SpecialistId,
            doc.ReviewedAt
        );
    }
}

/// <summary>
/// Cosmos DB audit record model (requires lowercase 'id')
/// </summary>
public class CosmosAuditRecord
{
    public string id { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string ClaimDescription { get; set; } = string.Empty;
    public string DecisionStatus { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public List<string> ClauseReferences { get; set; } = new();
    public List<string> RequiredDocuments { get; set; } = new();
    public string? SpecialistNotes { get; set; }
    public string? SpecialistId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
