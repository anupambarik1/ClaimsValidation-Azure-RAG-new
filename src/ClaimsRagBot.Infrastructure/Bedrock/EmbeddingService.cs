using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using ClaimsRagBot.Core.Interfaces;

namespace ClaimsRagBot.Infrastructure.Bedrock;

public class EmbeddingService : IEmbeddingService
{
    private readonly AmazonBedrockRuntimeClient _client;

    public EmbeddingService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];

        accessKeyId = "testaccesskey";
        secretAccessKey = "testsecretaccesskey";

        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _client = new AmazonBedrockRuntimeClient(credentials, config);
        }
        else
        {
            // Fallback to default credential chain
            _client = new AmazonBedrockRuntimeClient(config);
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                inputText = text
            };

            var request = new InvokeModelRequest
            {
                ModelId = "amazon.titan-embed-text-v1",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
                ContentType = "application/json",
                Accept = "application/json"
            };

            var response = await _client.InvokeModelAsync(request);
            
            using var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);

            if (result?.Embedding == null || result.Embedding.Length == 0)
            {
                Console.WriteLine($"⚠️ Warning: Empty embedding returned for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                Console.WriteLine($"Response: {responseBody}");
            }

            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error generating embedding: {ex.Message}");
            return Array.Empty<float>();
        }
    }
}

internal class EmbeddingResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("inputTextTokenCount")]
    public int InputTextTokenCount { get; set; }
}
