# Cloud Provider Toggle - Quick Start Guide

## Overview
The Claims RAG Bot now supports both AWS and Azure cloud providers with a simple configuration toggle. The application uses the same business logic and interfaces but switches the underlying cloud service implementations based on configuration.

## Current Status
✅ **Implemented:**
- Azure OpenAI for Embeddings (replaces AWS Bedrock Titan)
- Azure OpenAI for LLM Chat (replaces AWS Bedrock Claude)
- Cloud provider toggle logic in Program.cs
- Configuration structure for both providers

⚠️ **Still using AWS:**
- Vector Search (OpenSearch) - will migrate to Azure AI Search
- Audit Trail (DynamoDB) - will migrate to Azure Cosmos DB
- Document Storage (S3) - will migrate to Azure Blob Storage
- Document OCR (Textract) - will migrate to Azure Document Intelligence
- NLP (Comprehend) - will migrate to Azure Language Service
- Image Analysis (Rekognition) - will migrate to Azure Computer Vision

## How to Switch Between Providers

### Option 1: Edit appsettings.json
```json
{
  "CloudProvider": "AWS"  // or "Azure"
}
```

### Option 2: Environment Variable
```bash
# PowerShell
$env:CloudProvider = "Azure"
dotnet run

# Or
$env:CloudProvider = "AWS"
dotnet run
```

### Option 3: Command Line Argument
```bash
dotnet run --CloudProvider=Azure
```

## Azure Setup Required

### Step 1: Create Azure OpenAI Resource
```bash
# Create resource group
az group create --name rg-claims-rag --location eastus

# Create Azure OpenAI service
az cognitiveservices account create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

### Step 2: Deploy Models
```bash
# Deploy embedding model
az cognitiveservices account deployment create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag \
  --deployment-name text-embedding-ada-002 \
  --model-name text-embedding-ada-002 \
  --model-version 2 \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard

# Deploy chat model
az cognitiveservices account deployment create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag \
  --deployment-name gpt-4-turbo \
  --model-name gpt-4 \
  --model-version turbo-2024-04-09 \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard
```

### Step 3: Get Connection Details
```bash
# Get endpoint
az cognitiveservices account show \
  --name openai-claims-rag \
  --resource-group rg-claims-rag \
  --query properties.endpoint -o tsv

# Get API key
az cognitiveservices account keys list \
  --name openai-claims-rag \
  --resource-group rg-claims-rag \
  --query key1 -o tsv
```

### Step 4: Update appsettings.json
```json
{
  "CloudProvider": "Azure",
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://openai-claims-rag.openai.azure.com/",
      "ApiKey": "your-key-from-step-3",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4-turbo"
    }
  }
}
```

## Testing the Toggle

### Test 1: Verify AWS (Default)
```bash
# Ensure CloudProvider is set to AWS or not set
dotnet run

# Look for console output:
# 🌩️  Cloud Provider: AWS
# ✅ Registering AWS services...
```

### Test 2: Verify Azure
```bash
# Set CloudProvider to Azure
$env:CloudProvider = "Azure"
dotnet run

# Look for console output:
# 🌩️  Cloud Provider: Azure
# ✅ Registering Azure services...
# [AzureEmbedding] Initialized with deployment: text-embedding-ada-002
# [AzureLLM] Initialized with deployment: gpt-4-turbo
# ⚠️  Note: Only Embedding and LLM services using Azure. Other services still on AWS.
```

### Test 3: Validate Claim with Azure
```bash
POST http://localhost:5000/claims/validate
Content-Type: application/json

{
  "policyNumber": "TEST-AZURE-001",
  "policyType": "Health",
  "claimAmount": 1500,
  "claimDescription": "Hospital admission for pneumonia treatment"
}

# Check response and console logs:
# [AzureEmbedding] Generated 1536-dimensional embedding
# [AzureLLM] Generated decision with 523 tokens
```

## Key Differences: AWS vs Azure

| Service | AWS | Azure | Dimension Change |
|---------|-----|-------|------------------|
| **Embedding Model** | Titan Text Embeddings V2 | text-embedding-ada-002 | 1024 → 1536 dimensions |
| **Chat Model** | Claude 3.5 Sonnet | GPT-4 Turbo | Different API format |
| **API Pattern** | InvokeModel with JSON body | GetEmbeddingsAsync / GetChatCompletionsAsync | Native SDK |
| **Authentication** | AWS credentials | API key | Simpler in Azure |

## Important Notes

### Embedding Dimension Change
⚠️ **CRITICAL:** Azure embeddings are 1536 dimensions vs AWS 1024 dimensions. When you switch to Azure:
- Existing OpenSearch index will NOT work with Azure embeddings
- You'll need to re-index all policy clauses with Azure embeddings
- Don't switch OpenSearch to Azure AI Search until you're ready to migrate fully

### Cost Implications
- **Azure OpenAI:** Pay per token (similar to AWS Bedrock)
- **Embedding:** ~$0.0001 per 1K tokens
- **GPT-4 Turbo:** ~$0.01 per 1K input tokens, ~$0.03 per 1K output tokens

### Current Limitations
Since only Embedding and LLM are migrated to Azure:
- Vector search still uses AWS OpenSearch (expects 1024-dim embeddings)
- This will cause errors if you use Azure embeddings with AWS OpenSearch
- **Recommendation:** Keep CloudProvider as "AWS" until full migration is complete

## Next Steps

1. **Test Azure services** (if you have Azure OpenAI resource)
2. **Migrate remaining services:**
   - Azure AI Search (vector search)
   - Azure Cosmos DB (audit trail)
   - Azure Blob Storage (documents)
   - Azure Document Intelligence (OCR)
   - Azure Language Service (NLP)
   - Azure Computer Vision (image analysis)

3. **Full cutover** once all services migrated

## Troubleshooting

### Error: "Azure:OpenAI:Endpoint not configured"
- Make sure appsettings.json has Azure.OpenAI section
- Check CloudProvider is set to "Azure"

### Error: "RequestFailedException: 404"
- Verify deployment names match your Azure OpenAI deployments
- Check endpoint URL is correct

### Error: Dimension mismatch in OpenSearch
- This is expected! Azure embeddings (1536) don't match AWS (1024)
- Keep using AWS until full migration

## Rollback
To switch back to AWS at any time:
```json
{
  "CloudProvider": "AWS"
}
```
Restart the application. No code changes needed.

---
**Created:** February 5, 2026  
**Version:** 1.0 (Phase 1 - Embeddings & LLM only)
