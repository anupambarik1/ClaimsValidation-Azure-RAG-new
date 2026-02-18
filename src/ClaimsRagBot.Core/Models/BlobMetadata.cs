using Newtonsoft.Json;

namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Maps document IDs to blob storage locations
/// </summary>
public class BlobMetadata
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty("DocumentId")]
    public string DocumentId { get; set; } = string.Empty;
    
    [JsonProperty("BlobName")]
    public string BlobName { get; set; } = string.Empty;
    
    [JsonProperty("ContainerName")]
    public string ContainerName { get; set; } = string.Empty;
    
    [JsonProperty("FileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonProperty("ContentType")]
    public string ContentType { get; set; } = string.Empty;
    
    [JsonProperty("FileSize")]
    public long FileSize { get; set; }
    
    [JsonProperty("UserId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonProperty("UploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [JsonProperty("DeletedAt")]
    public DateTime? DeletedAt { get; set; } = null;
}
