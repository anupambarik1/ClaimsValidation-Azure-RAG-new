namespace ClaimsRagBot.Core.Configuration;

/// <summary>
/// Supported cloud providers for the Claims RAG Bot
/// </summary>
public enum CloudProvider
{
    AWS,
    Azure
}

/// <summary>
/// Cloud provider configuration settings
/// </summary>
public class CloudProviderSettings
{
    public CloudProvider Provider { get; set; } = CloudProvider.AWS;

    /// <summary>
    /// Get the configured cloud provider from app settings
    /// </summary>
    public static CloudProvider GetProvider(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var providerString = configuration["CloudProvider"] ?? "AWS";
        
        if (Enum.TryParse<CloudProvider>(providerString, ignoreCase: true, out var provider))
        {
            return provider;
        }
        
        Console.WriteLine($"[Warning] Invalid CloudProvider '{providerString}', defaulting to AWS");
        return CloudProvider.AWS;
    }
}
