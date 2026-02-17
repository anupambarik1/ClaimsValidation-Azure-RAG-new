using Azure;
using Azure.AI.OpenAI;
using ClaimsRagBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure OpenAI implementation for generating text embeddings
/// </summary>
public class AzureEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public AzureEmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:OpenAI:Endpoint"] 
            ?? throw new ArgumentException("Azure:OpenAI:Endpoint not configured");
        var apiKey = configuration["Azure:OpenAI:ApiKey"] 
            ?? throw new ArgumentException("Azure:OpenAI:ApiKey not configured");
        
        _deploymentName = configuration["Azure:OpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";
        
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine($"[AzureEmbedding] Initialized with deployment: {_deploymentName}");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var options = new EmbeddingsOptions(_deploymentName, new[] { text });
            var response = await _client.GetEmbeddingsAsync(options);
            
            var embedding = response.Value.Data[0].Embedding.ToArray();
            
            Console.WriteLine($"[AzureEmbedding] Generated {embedding.Length}-dimensional embedding");
            return embedding;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureEmbedding] Error: {ex.Status} - {ex.Message}");
            throw new InvalidOperationException($"Azure OpenAI embedding generation failed: {ex.Message}", ex);
        }
    }
}
