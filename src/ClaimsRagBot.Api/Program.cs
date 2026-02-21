using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Application.Security;
using ClaimsRagBot.Application.Validation;
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
using System.Threading.RateLimiting;

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
    builder.Services.AddSingleton<IBlobMetadataRepository, CosmosBlobMetadataRepository>();
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

// Guardrail services (security & validation)
Console.WriteLine("✅ Registering guardrail services...");
builder.Services.AddSingleton<IPiiMaskingService, PiiMaskingService>();
builder.Services.AddSingleton<IPromptInjectionDetector, PromptInjectionDetector>();
builder.Services.AddSingleton<ICitationValidator, CitationValidator>();
builder.Services.AddSingleton<IContradictionDetector, ContradictionDetector>();
Console.WriteLine("✅ All 4 guardrail services registered (PII Masking, Prompt Injection Detection, Citation Validation, Contradiction Detection)");

builder.Services.AddScoped<ClaimValidationOrchestrator>(sp =>
    new ClaimValidationOrchestrator(
        sp.GetRequiredService<IEmbeddingService>(),
        sp.GetRequiredService<IRetrievalService>(),
        sp.GetRequiredService<ILlmService>(),
        sp.GetRequiredService<IAuditService>(),
        sp.GetRequiredService<IDocumentExtractionService>(),
        sp.GetRequiredService<ICitationValidator>(),
        sp.GetRequiredService<IContradictionDetector>()
    ));

// Configure rate limiting (guardrail)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", token);
    };
});

Console.WriteLine("✅ Rate limiting configured: 100 requests per minute per host");

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
    app.Services.GetRequiredService<ILlmService>()
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
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
