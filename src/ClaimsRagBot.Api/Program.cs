using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Infrastructure.Bedrock;
using ClaimsRagBot.Infrastructure.DynamoDB;
using ClaimsRagBot.Infrastructure.OpenSearch;
using ClaimsRagBot.Infrastructure.S3;
using ClaimsRagBot.Infrastructure.Textract;
using ClaimsRagBot.Infrastructure.Comprehend;
using ClaimsRagBot.Infrastructure.Rekognition;
using ClaimsRagBot.Infrastructure.DocumentExtraction;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Claims RAG Bot API",
        Version = "v1",
        Description = "AI-powered claims validation system using RAG (Retrieval-Augmented Generation) with AWS Bedrock, OpenSearch, and DynamoDB",
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

// Register dependencies
builder.Services.AddSingleton<IEmbeddingService>(sp =>
    new EmbeddingService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IRetrievalService>(sp => 
    new RetrievalService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<ILlmService>(sp =>
    new LlmService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IAuditService>(sp =>
    new AuditService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<ClaimValidationOrchestrator>();

// Register new document extraction services
builder.Services.AddSingleton<IDocumentUploadService>(sp =>
    new DocumentUploadService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<ITextractService>(sp =>
    new TextractService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IComprehendService>(sp =>
    new ComprehendService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IRekognitionService>(sp =>
    new RekognitionService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IDocumentExtractionService>(sp =>
    new DocumentExtractionOrchestrator(
        sp.GetRequiredService<IDocumentUploadService>(),
        sp.GetRequiredService<ITextractService>(),
        sp.GetRequiredService<IComprehendService>(),
        sp.GetRequiredService<ILlmService>(),
        sp.GetRequiredService<IRekognitionService>(),
        sp.GetRequiredService<IConfiguration>()
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
