using System.Text.Json.Serialization;

namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Maps document IDs to blob storage locations
/// </summary>
public class BlobMetadata
{
    [JsonPropertyName("id")]
    public string Id => DocumentId;
    
    public string DocumentId { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
