using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Core.Configuration;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Infrastructure.Bedrock;
using ClaimsRagBot.Infrastructure.DynamoDB;
using ClaimsRagBot.Infrastructure.OpenSearch;
using ClaimsRagBot.Infrastructure.S3;
using ClaimsRagBot.Infrastructure.Textract;
using ClaimsRagBot.Infrastructure.Comprehend;
using ClaimsRagBot.Infrastructure.Rekognition;
using ClaimsRagBot.Infrastructure.DocumentExtraction;
using ClaimsRagBot.Infrastructure.Azure;
using ClaimsRagBot.Infrastructure.Tools;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Detect cloud provider from configuration
var cloudProvider = CloudProviderSettings.GetProvider(builder.Configuration);
Console.WriteLine($"🌩️  Cloud Provider: {cloudProvider}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Claims RAG Bot API",
        Version = "v1",
        Description = $"AI-powered claims validation system using RAG with {cloudProvider} services",
        Contact = new()
        {
            Name = "Claims RAG Bot",
            Email = "support@claimsragbot.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register dependencies based on cloud provider
if (cloudProvider == CloudProvider.Azure)
{
    Console.WriteLine("✅ Registering Azure services...");
    
    // Azure AI Services
    builder.Services.AddSingleton<IEmbeddingService, AzureEmbeddingService>();
    builder.Services.AddSingleton<ILlmService, AzureLlmService>();
    builder.Services.AddSingleton<IRetrievalService, AzureAISearchService>();
    
    // Azure Data Services
    builder.Services.AddSingleton<IAuditService, AzureCosmosAuditService>();
    // Register blob metadata repository as optional - will be null if Cosmos DB not fully configured
    builder.Services.AddSingleton<IBlobMetadataRepository>(sp =>
    {
        try
        {
            return new CosmosBlobMetadataRepository(sp.GetRequiredService<IConfiguration>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Blob metadata repository unavailable: {ex.Message}");
            Console.WriteLine($"    Document download/delete by ID will not work. Run setup-cosmos-containers.ps1 to fix.");
            return null!;
        }
    });
    builder.Services.AddSingleton<IDocumentUploadService, AzureBlobStorageService>();
    
    // Azure Document Processing Services
    builder.Services.AddSingleton<ITextractService, AzureDocumentIntelligenceService>();
    builder.Services.AddSingleton<IComprehendService, AzureLanguageService>();
    builder.Services.AddSingleton<IRekognitionService, AzureComputerVisionService>();
    
    Console.WriteLine("✅ All 8 services configured for Azure (OpenAI, AI Search, Cosmos DB, Blob Storage, Document Intelligence, Language, Computer Vision)");
}
else // AWS (default)
{
    Console.WriteLine("✅ Registering AWS services...");
    builder.Services.AddSingleton<IEmbeddingService>(sp =>
        new EmbeddingService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<IRetrievalService>(sp => 
        new RetrievalService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<ILlmService>(sp =>
        new LlmService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<IAuditService>(sp =>
        new AuditService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<IDocumentUploadService>(sp =>
        new DocumentUploadService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<ITextractService>(sp =>
        new TextractService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<IComprehendService>(sp =>
        new ComprehendService(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddSingleton<IRekognitionService>(sp =>
        new RekognitionService(sp.GetRequiredService<IConfiguration>()));
}

// Shared services (cloud-agnostic)
builder.Services.AddScoped<IDocumentExtractionService>(sp =>
    new DocumentExtractionOrchestrator(
        sp.GetRequiredService<IDocumentUploadService>(),
        sp.GetRequiredService<ITextractService>(),
        sp.GetRequiredService<IComprehendService>(),
        sp.GetRequiredService<ILlmService>(),
        sp.GetRequiredService<IRekognitionService>(),
        sp.GetRequiredService<IConfiguration>()
    ));

builder.Services.AddScoped<ClaimValidationOrchestrator>(sp =>
    new ClaimValidationOrchestrator(
        sp.GetRequiredService<IEmbeddingService>(),
        sp.GetRequiredService<IRetrievalService>(),
        sp.GetRequiredService<ILlmService>(),
        sp.GetRequiredService<IAuditService>(),
        sp.GetRequiredService<IDocumentExtractionService>()
    ));

// Configure CORS for testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Run startup health check
Console.WriteLine("\n" + new string('=', 60));
var healthCheck = new StartupHealthCheck(builder.Configuration);
var healthResult = await healthCheck.ValidateServicesAsync(
    app.Services.GetRequiredService<IRetrievalService>(),
    app.Services.GetRequiredService<IEmbeddingService>(),
    app.Services.GetRequiredService<ILlmService>(),
    app.Services.GetRequiredService<IAuditService>()
);
Console.WriteLine(new string('=', 60) + "\n");

if (!healthResult.IsHealthy)
{
    Console.WriteLine("⚠️  WARNING: Some services failed health checks. API may not function correctly.");
    Console.WriteLine("    Review errors above and check your configuration in appsettings.json");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Claims RAG Bot API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Claims RAG Bot API";
        options.DisplayRequestDuration();
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
