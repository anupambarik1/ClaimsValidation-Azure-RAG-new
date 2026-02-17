# Azure Deployment Requirements - Claims RAG Bot
## Complete Infrastructure Setup & Cost Analysis

**Document Version:** 1.0  
**Last Updated:** February 10, 2026  
**Application:** Claims Validation RAG Bot  
**Target Cloud Provider:** Microsoft Azure

---

## Table of Contents
1. [Overview](#overview)
2. [Azure Services Required](#azure-services-required)
3. [Detailed Service Specifications](#detailed-service-specifications)
4. [Configuration Requirements](#configuration-requirements)
5. [Estimated Azure Costs](#estimated-azure-costs)
6. [Deployment Checklist](#deployment-checklist)
7. [Security & Access Management](#security--access-management)
8. [Monitoring & Alerts](#monitoring--alerts)

---

## Overview

The Claims RAG Bot is an AI-powered insurance claims validation system that requires **7 Azure services** to operate. This document provides comprehensive details on:

- What Azure resources to provision
- Service tier/SKU recommendations
- Configuration parameters needed
- Detailed cost breakdown with monthly projections
- Setup sequence and dependencies

### Architecture Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                  Claims RAG Bot Architecture                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Document Upload → Azure Blob Storage                            │
│       ↓                                                           │
│  OCR Processing → Azure Document Intelligence                    │
│       ↓                                                           │
│  Entity Extraction → Azure Language Service                      │
│       ↓                                                           │
│  Image Analysis → Azure Computer Vision                          │
│       ↓                                                           │
│  Text Embedding → Azure OpenAI (text-embedding-ada-002)         │
│       ↓                                                           │
│  Policy Retrieval → Azure AI Search (Vector Database)           │
│       ↓                                                           │
│  Decision Making → Azure OpenAI (GPT-4 Turbo)                   │
│       ↓                                                           │
│  Audit Trail → Azure Cosmos DB                                   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Azure Services Required

### Service Summary Table

| # | Azure Service | Purpose | Replaces AWS Service | Required? |
|---|---------------|---------|---------------------|-----------|
| 1 | **Azure OpenAI Service** | Embeddings + LLM reasoning | AWS Bedrock | ✅ Critical |
| 2 | **Azure AI Search** | Vector database for RAG | AWS OpenSearch Serverless | ✅ Critical |
| 3 | **Azure Cosmos DB** | Claims audit trail | AWS DynamoDB | ✅ Critical |
| 4 | **Azure Blob Storage** | Document storage | AWS S3 | ✅ Critical |
| 5 | **Azure Document Intelligence** | OCR & extraction | AWS Textract | ✅ Critical |
| 6 | **Azure Language Service** | NLP entity recognition | AWS Comprehend | ✅ Critical |
| 7 | **Azure Computer Vision** | Image analysis | AWS Rekognition | ✅ Critical |

**Total Services:** 7 Azure resources  
**Service Category:** 4 AI/ML services + 3 data/storage services

---

## Detailed Service Specifications

### 1. Azure OpenAI Service 🤖

#### Overview
Foundation AI service providing both semantic embeddings and large language model capabilities.

#### What to Provision
- **Resource Type:** Azure OpenAI
- **Region:** East US, West Europe, or South Central US (check GPT-4 availability)
- **Pricing Tier:** Standard (pay-as-you-go)
- **Access Control:** Network restricted + Azure AD authentication

#### Model Deployments Required

**Deployment 1: Text Embeddings**
```yaml
Model: text-embedding-ada-002
Deployment Name: text-embedding-ada-002
Capacity Type: Standard
Tokens Per Minute (TPM): 120,000
Version: 2 (latest)
```

**Deployment 2: Chat Completions**
```yaml
Model: gpt-4-turbo (or gpt-4-1106-preview)
Deployment Name: gpt-4-turbo
Capacity Type: Standard
Tokens Per Minute (TPM): 80,000
Version: Latest stable
```

#### Configuration Needed
```json
"Azure": {
  "OpenAI": {
    "Endpoint": "https://<your-resource-name>.openai.azure.com/",
    "ApiKey": "<32-character-api-key>",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ChatDeployment": "gpt-4-turbo"
  }
}
```

#### Usage Pattern
- **Embeddings:** ~500-1,000 requests/day (claim descriptions + policy ingestion)
- **GPT-4 Turbo:** ~300-600 requests/day (claim decisions)
- **Token Usage:** 
  - Embeddings: ~150 tokens/request
  - GPT-4: ~1,500 tokens/request (input) + ~500 tokens/response

#### Estimated Monthly Cost
- Embeddings: $0.0001/1K tokens × 500 requests × 150 tokens × 30 days = **$2.25**
- GPT-4 Turbo: 
  - Input: $0.01/1K tokens × 600 requests × 1,500 tokens × 30 days = **$270**
  - Output: $0.03/1K tokens × 600 requests × 500 tokens × 30 days = **$270**
- **Subtotal: ~$542/month**

---

### 2. Azure AI Search 🔍

#### Overview
Managed search service with vector search capabilities for semantic policy clause retrieval.

#### What to Provision
- **Resource Type:** Azure Cognitive Search / AI Search
- **Pricing Tier:** Standard S1 (minimum for vector search)
- **Region:** Same as Azure OpenAI (reduce latency)
- **Capacity:** 1 replica, 1 partition (can scale later)

#### Features Required
- ✅ Vector search enabled
- ✅ Semantic search (optional but recommended)
- ✅ HTTPS endpoint
- ✅ RBAC + API key authentication

#### Index Configuration

**Index Name:** `policy-clauses`

**Index Schema:**
```json
{
  "name": "policy-clauses",
  "fields": [
    {
      "name": "ClauseId",
      "type": "Edm.String",
      "key": true,
      "filterable": false,
      "searchable": false
    },
    {
      "name": "Text",
      "type": "Edm.String",
      "searchable": true,
      "filterable": false,
      "analyzer": "en.microsoft"
    },
    {
      "name": "PolicyType",
      "type": "Edm.String",
      "filterable": true,
      "facetable": true
    },
    {
      "name": "CoverageType",
      "type": "Edm.String",
      "filterable": true
    },
    {
      "name": "Section",
      "type": "Edm.String",
      "filterable": true
    },
    {
      "name": "Embedding",
      "type": "Collection(Edm.Single)",
      "dimensions": 1536,
      "vectorSearchProfile": "vector-profile-1536"
    }
  ],
  "vectorSearch": {
    "profiles": [
      {
        "name": "vector-profile-1536",
        "algorithm": "hnsw",
        "vectorizer": null
      }
    ],
    "algorithms": [
      {
        "name": "hnsw",
        "kind": "hnsw",
        "hnswParameters": {
          "m": 4,
          "efConstruction": 400,
          "efSearch": 500,
          "metric": "cosine"
        }
      }
    ]
  }
}
```

#### Storage Requirements
- **Expected Documents:** 5,000-10,000 policy clauses
- **Storage Per Document:** ~2 KB text + 6 KB vector (1536 dims × 4 bytes)
- **Total Storage:** ~80 MB (well within S1 limits)

#### Configuration Needed
```json
"Azure": {
  "AISearch": {
    "Endpoint": "https://<your-search-name>.search.windows.net/",
    "QueryApiKey": "<query-key-from-portal>",
    "AdminApiKey": "<admin-key-for-indexing>",
    "IndexName": "policy-clauses"
  }
}
```

#### Usage Pattern
- **Queries:** ~600 searches/day (1 per claim validation)
- **Indexing:** Batch updates during policy ingestion (weekly)
- **Vector Search:** KNN with k=5, cosine similarity

#### Estimated Monthly Cost
- **Standard S1:** Fixed price
- **Base Cost: $250/month** (1 replica, 1 partition)
- Semantic search add-on: +$500/month (optional - skip for cost savings)
- **Subtotal: $250/month**

---

### 3. Azure Cosmos DB 🗄️

#### Overview
Globally distributed NoSQL database for storing claims audit trail and decision history.

#### What to Provision
- **Resource Type:** Azure Cosmos DB
- **API:** Core (SQL)
- **Capacity Mode:** Provisioned throughput (predictable costs)
- **Region:** Single region (primary application region)
- **Consistency Level:** Session (default, good for this use case)

#### Database Structure

**Database:** `ClaimsDatabase`

**Container:** `AuditTrail`
```yaml
Partition Key: /PolicyNumber
Indexing Policy: Default (automatic)
Time to Live (TTL): Disabled (retain all history)
Throughput: 400 RU/s (autoscale: 400-4000)
```

#### Document Schema Example
```json
{
  "id": "claim-20260210-abc123",
  "ClaimId": "claim-20260210-abc123",
  "PolicyNumber": "AFL-12345-HEALTH",
  "Timestamp": "2026-02-10T14:30:00Z",
  "ClaimAmount": 5000.00,
  "ClaimType": "Hospitalization",
  "DecisionStatus": "Covered",
  "Explanation": "Hospitalization for appendicitis covered under Section 3.2...",
  "ConfidenceScore": 0.92,
  "ClauseReferences": ["CLAUSE-3.2.1", "CLAUSE-5.1.3"],
  "RequiredDocuments": ["Hospital admission letter", "Medical bills"],
  "ProcessingTimeMs": 1250,
  "ReviewedBy": null
}
```

#### Storage Requirements
- **Documents Per Day:** ~600 claims
- **Document Size:** ~2 KB each
- **Monthly Storage:** 600 × 30 × 2 KB = ~36 MB/month
- **Annual Growth:** ~432 MB/year

#### Configuration Needed
```json
"Azure": {
  "CosmosDB": {
    "Endpoint": "https://<your-cosmos-name>.documents.azure.com:443/",
    "Key": "<primary-key-from-portal>",
    "DatabaseId": "ClaimsDatabase",
    "ContainerId": "AuditTrail"
  }
}
```

#### Usage Pattern
- **Writes:** ~600 claims/day = 20 claims/hour
- **Reads:** ~50 queries/day (claim history lookups)
- **RU Consumption:**
  - Write (2 KB document): ~10 RU
  - Point read: ~1 RU
  - Total: ~6,000 RU/day + ~50 RU/day = ~6,050 RU/day

#### Estimated Monthly Cost
- **Provisioned Throughput:** 400 RU/s × $0.008/hour
- 400 RU/s × $0.008 × 730 hours = **$2,336/month** (too expensive!)

**Recommended: Switch to Autoscale**
- **Autoscale:** 400-4000 RU/s
- Base cost for max 4000 RU/s: $0.012/hour per 100 RU/s
- 4000 RU/s / 100 × $0.012 × 730 hours = ~$350/month
- **With actual low usage:** ~**$25-50/month** (scales down automatically)

**Better Option: Serverless Mode**
- **Serverless:** Pay per request
- 600 writes/day × 10 RU = 6,000 RU/day
- 50 reads/day × 1 RU = 50 RU/day
- Total: ~180,000 RU/month
- Cost: 180K RU × $0.25/million RU = **$0.045/month** (minimum charge ~$5)
- **Subtotal: ~$25/month** (serverless recommended)

---

### 4. Azure Blob Storage 📦

#### Overview
Object storage for uploaded claim documents, supporting documents, and policy PDFs.

#### What to Provision
- **Resource Type:** Storage Account (General Purpose v2)
- **Performance Tier:** Standard
- **Replication:** LRS (Locally Redundant Storage)
- **Access Tier:** Hot (frequently accessed documents)
- **Region:** Same as application

#### Container Structure

**Container:** `claims-documents`
```yaml
Public Access: Private (blob)
Encryption: Microsoft-managed keys
Versioning: Disabled
Soft Delete: Enabled (7 days retention)
```

**Folder Structure:**
```
claims-documents/
├── uploads/                 # User-uploaded documents
│   ├── 2026/02/10/
│   │   ├── claim-abc123.pdf
│   │   └── claim-def456.jpg
├── processed/               # OCR-processed documents
│   ├── 2026/02/10/
│   │   └── claim-abc123-extracted.json
├── policies/                # Policy documents
│   ├── health/
│   │   └── aflac-health-2026.pdf
│   └── motor/
│       └── aflac-motor-2026.pdf
```

#### Features Required
- ✅ SAS Token generation (secure temporary access)
- ✅ CORS enabled (for Angular file upload)
- ✅ HTTPS only
- ✅ Lifecycle management (optional: archive old documents)

#### Configuration Needed
```json
"Azure": {
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net",
    "ContainerName": "claims-documents",
    "UploadPrefix": "uploads/",
    "SasTokenExpiration": 3600
  }
}
```

#### Storage Requirements
- **Documents Per Day:** ~600 claims
- **Average File Size:** 500 KB (PDF/image)
- **Daily Storage:** 600 × 500 KB = ~300 MB/day
- **Monthly Storage:** ~9 GB/month
- **Annual Growth:** ~108 GB/year

#### Usage Pattern
- **Uploads:** ~600 files/day
- **Downloads:** ~100 files/day (SAS tokens)
- **Operations:** ~700 write ops + ~100 read ops/day

#### Estimated Monthly Cost
- **Storage:** 9 GB × $0.018/GB = **$0.16**
- **Write Operations:** 21,000/month ÷ 10,000 × $0.05 = **$0.11**
- **Read Operations:** 3,000/month ÷ 10,000 × $0.004 = **$0.001**
- **Subtotal: ~$5/month** (includes bandwidth)

---

### 5. Azure Document Intelligence 📄

#### Overview
AI-powered OCR service for extracting text, tables, and forms from uploaded claim documents.

#### What to Provision
- **Resource Type:** Azure AI Document Intelligence (formerly Form Recognizer)
- **Pricing Tier:** S0 (Standard)
- **Region:** Same as Azure OpenAI for data residency
- **Features:** Prebuilt models + Custom models (optional)

#### Models Used

**Prebuilt Document Model:**
```yaml
Model ID: prebuilt-document
Capabilities:
  - Text extraction (OCR)
  - Table detection and extraction
  - Key-value pair extraction
  - Document structure analysis
Supported Formats: PDF, JPEG, PNG, BMP, TIFF
Max File Size: 500 MB
Max Pages: 2000
```

#### Configuration Needed
```json
"Azure": {
  "DocumentIntelligence": {
    "Endpoint": "https://<your-resource-name>.cognitiveservices.azure.com/",
    "ApiKey": "<api-key-from-portal>",
    "ModelId": "prebuilt-document"
  }
}
```

#### Usage Pattern
- **Documents Processed:** ~600/day
- **Average Pages:** 3 pages/document
- **Total Pages:** ~1,800 pages/day = ~54,000 pages/month

#### Estimated Monthly Cost
- **Prebuilt Document Model:** $10 per 1,000 pages
- 54,000 pages ÷ 1,000 × $10 = **$540/month**

**Cost Optimization:**
- Use prebuilt-read model for simple text extraction: $1 per 1,000 pages
- Optimized cost: 54,000 ÷ 1,000 × $1 = **$54/month**
- **Subtotal: $54-540/month** (depending on model)

---

### 6. Azure Language Service 📝

#### Overview
NLP service for extracting entities (dates, amounts, names, medical terms) from claim text.

#### What to Provision
- **Resource Type:** Azure AI Language
- **Pricing Tier:** S (Standard)
- **Region:** Any region
- **Features:** Named Entity Recognition (NER), Key Phrase Extraction

#### Features Used

**Named Entity Recognition (NER):**
```yaml
Detects:
  - Person names (claimant, doctor)
  - Dates (accident date, treatment date)
  - Monetary amounts ($5,000)
  - Organizations (hospitals, clinics)
  - Locations (accident location)
  - Medical conditions (if healthcare domain)
Languages: English (en)
```

**Key Phrase Extraction:**
```yaml
Extracts: Important phrases describing claim
Examples: "car accident", "emergency surgery", "total loss"
```

#### Configuration Needed
```json
"Azure": {
  "LanguageService": {
    "Endpoint": "https://<your-resource-name>.cognitiveservices.azure.com/",
    "ApiKey": "<api-key-from-portal>"
  }
}
```

#### Usage Pattern
- **Text Records:** ~600 claims/day
- **Average Text Size:** 500 characters (claim description)
- **Total Text Units:** 600 × 1 unit/day = ~600 units/day = ~18,000 units/month

#### Estimated Monthly Cost
- **Text Analytics (NER + Key Phrases):** $2 per 1,000 text records
- 18,000 ÷ 1,000 × $2 = **$36/month**
- **Subtotal: ~$36/month**

---

### 7. Azure Computer Vision 👁️

#### Overview
AI service for analyzing images in claim documents (damage assessment, document quality).

#### What to Provision
- **Resource Type:** Azure AI Vision (Computer Vision)
- **Pricing Tier:** S1 (Standard)
- **Region:** Any region
- **Features:** Image Analysis, OCR (backup)

#### Features Used

**Image Analysis:**
```yaml
Capabilities:
  - Object detection (damaged vehicle parts)
  - Image quality assessment
  - Scene classification
  - Adult/racy content filtering (compliance)
Visual Features:
  - Objects, Tags, Description
  - ImageType (is it a document/photo?)
  - Adult content scores
```

#### Configuration Needed
```json
"Azure": {
  "ComputerVision": {
    "Endpoint": "https://<your-resource-name>.cognitiveservices.azure.com/",
    "ApiKey": "<api-key-from-portal>",
    "MinConfidence": 0.7
  }
}
```

#### Usage Pattern
- **Images Analyzed:** ~300/day (50% of claims have images)
- **API Calls:** 1 analysis/image = ~300 calls/day = ~9,000 calls/month

#### Estimated Monthly Cost
- **Image Analysis:** $1 per 1,000 transactions (S1 tier)
- 9,000 ÷ 1,000 × $1 = **$9/month**
- **Subtotal: ~$9/month**

---

## Configuration Requirements

### Complete appsettings.json Template

```json
{
  "CloudProvider": "Azure",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://<your-openai>.openai.azure.com/",
      "ApiKey": "<your-openai-api-key>",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4-turbo"
    },
    "AISearch": {
      "Endpoint": "https://<your-search>.search.windows.net/",
      "QueryApiKey": "<your-query-key>",
      "AdminApiKey": "<your-admin-key>",
      "IndexName": "policy-clauses"
    },
    "CosmosDB": {
      "Endpoint": "https://<your-cosmos>.documents.azure.com:443/",
      "Key": "<your-cosmos-primary-key>",
      "DatabaseId": "ClaimsDatabase",
      "ContainerId": "AuditTrail"
    },
    "BlobStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net",
      "ContainerName": "claims-documents",
      "UploadPrefix": "uploads/",
      "SasTokenExpiration": 3600
    },
    "DocumentIntelligence": {
      "Endpoint": "https://<your-docint>.cognitiveservices.azure.com/",
      "ApiKey": "<your-docint-api-key>",
      "ModelId": "prebuilt-read"
    },
    "LanguageService": {
      "Endpoint": "https://<your-language>.cognitiveservices.azure.com/",
      "ApiKey": "<your-language-api-key>"
    },
    "ComputerVision": {
      "Endpoint": "https://<your-vision>.cognitiveservices.azure.com/",
      "ApiKey": "<your-vision-api-key>",
      "MinConfidence": 0.7
    }
  },
  
  "ClaimsValidation": {
    "AutoApprovalThreshold": 5000,
    "ConfidenceThreshold": 0.85
  },
  
  "DocumentProcessing": {
    "MaxFileSizeMB": 10,
    "AllowedContentTypes": [
      "application/pdf",
      "image/jpeg",
      "image/png"
    ],
    "ExtractionTimeoutMinutes": 5,
    "MinimumConfidenceThreshold": 0.7,
    "RequireUserReviewIfConfidenceBelow": 0.85
  }
}
```

### Environment Variables (Alternative to appsettings.json)

For production deployments, use Azure Key Vault or App Service Configuration:

```bash
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://xxx.openai.azure.com/
AZURE_OPENAI_KEY=xxxxx
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-ada-002
AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4-turbo

# Azure AI Search
AZURE_SEARCH_ENDPOINT=https://xxx.search.windows.net/
AZURE_SEARCH_QUERY_KEY=xxxxx
AZURE_SEARCH_ADMIN_KEY=xxxxx
AZURE_SEARCH_INDEX=policy-clauses

# Azure Cosmos DB
AZURE_COSMOS_ENDPOINT=https://xxx.documents.azure.com:443/
AZURE_COSMOS_KEY=xxxxx
AZURE_COSMOS_DATABASE=ClaimsDatabase
AZURE_COSMOS_CONTAINER=AuditTrail

# Azure Blob Storage
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
AZURE_STORAGE_CONTAINER=claims-documents

# Azure Document Intelligence
AZURE_DOCINT_ENDPOINT=https://xxx.cognitiveservices.azure.com/
AZURE_DOCINT_KEY=xxxxx

# Azure Language Service
AZURE_LANGUAGE_ENDPOINT=https://xxx.cognitiveservices.azure.com/
AZURE_LANGUAGE_KEY=xxxxx

# Azure Computer Vision
AZURE_VISION_ENDPOINT=https://xxx.cognitiveservices.azure.com/
AZURE_VISION_KEY=xxxxx
```

---

## Estimated Azure Costs

### Cost Breakdown by Service

| Service | Monthly Cost | 2-Month Cost | 3-Month Cost | Notes |
|---------|--------------|--------------|--------------|-------|
| **Azure OpenAI** | $542 | $1,084 | $1,626 | GPT-4 Turbo + Embeddings |
| **Azure AI Search (S1)** | $250 | $500 | $750 | Standard tier with vectors |
| **Azure Cosmos DB (Serverless)** | $25 | $50 | $75 | Low-usage serverless mode |
| **Azure Blob Storage** | $5 | $10 | $15 | Hot tier, 9GB/month |
| **Document Intelligence** | $54 | $108 | $162 | prebuilt-read model optimized |
| **Language Service** | $36 | $72 | $108 | NER + Key Phrases |
| **Computer Vision** | $9 | $18 | $27 | Image analysis |
| **Data Transfer (Egress)** | $10 | $20 | $30 | Estimated bandwidth |
| **Backup & Monitoring** | $20 | $40 | $60 | Azure Monitor + Log Analytics |
| | | | | |
| **TOTAL (Monthly)** | **$951** | **$1,902** | **$2,853** | |

### Cost Summary

```
╔════════════════════════════════════════════════════════════╗
║           AZURE MONTHLY COST PROJECTIONS                   ║
╠════════════════════════════════════════════════════════════╣
║  1 Month Total:     $951 USD                               ║
║  2 Month Total:     $1,902 USD                             ║
║  3 Month Total:     $2,853 USD                             ║
╠════════════════════════════════════════════════════════════╣
║  Average per month: $951 USD                               ║
║  Daily cost:        ~$32 USD                               ║
║  Cost per claim:    ~$1.58 USD (at 600 claims/day)        ║
╚════════════════════════════════════════════════════════════╝
```

### Cost Optimization Strategies

#### Immediate Savings (No Functionality Loss)

1. **Azure OpenAI - Use GPT-4o instead of GPT-4 Turbo**
   - GPT-4o: $5/$15 per 1M tokens (input/output)
   - Current GPT-4 Turbo: $10/$30 per 1M tokens
   - **Savings: ~$270/month (50% reduction)**

2. **Document Intelligence - Use prebuilt-read instead of prebuilt-document**
   - prebuilt-read: $1/1K pages (simple text extraction)
   - prebuilt-document: $10/1K pages (tables + forms)
   - **Savings: ~$486/month** (if tables not critical)

3. **AI Search - Use Basic tier for development**
   - Basic: $75/month (no vector search scale limits for dev)
   - Standard S1: $250/month
   - **Savings: $175/month** (development only)

4. **Cosmos DB - Use Free Tier (400 RU/s free)**
   - First 1000 RU/s free monthly
   - **Savings: $25/month** (within free quota)

**Total Potential Savings: $956/month**  
**Optimized Monthly Cost: ~$400-500/month**

#### Usage-Based Optimizations

5. **Batch Processing**
   - Process claims in batches during off-peak hours
   - Reduces concurrent API calls and costs

6. **Caching**
   - Cache common policy clauses in Redis
   - Reduce AI Search queries by ~30%
   - **Savings: ~$0** (AI Search is fixed cost)

7. **Reserved Capacity**
   - Azure OpenAI: 1-year commitment saves 15%
   - Cosmos DB: 1-year reserved capacity saves 30%
   - **Savings: ~$100/month** (with commitment)

### Development vs Production Costs

| Environment | Monthly Cost | Configuration |
|-------------|--------------|---------------|
| **Development** | $150-200 | Basic AI Search, GPT-3.5-turbo, minimal throughput |
| **Staging** | $400-500 | Standard S1, GPT-4o, low throughput |
| **Production** | $951 | Standard S1, GPT-4 Turbo, full throughput |
| **Production (Optimized)** | $450-500 | Optimizations applied |

### Cost Scaling Projections

**At Different Claim Volumes:**

| Claims/Day | Monthly Cost | Notes |
|------------|--------------|-------|
| 200 | $350-400 | Low volume, autoscale kicks in |
| 600 | $951 | Current estimate (baseline) |
| 1,200 | $1,450 | Medium volume, increased AI calls |
| 2,400 | $2,200 | High volume, may need S2 search tier |
| 5,000 | $3,500+ | Enterprise volume, dedicated capacity |

---

## Deployment Checklist

### Phase 1: Azure Resource Provisioning (2-3 hours)

```powershell
# Login to Azure
az login

# Set subscription
az account set --subscription "Your-Subscription-Name"

# Create resource group
az group create --name rg-claims-bot-prod --location eastus

# 1. Azure OpenAI
az cognitiveservices account create \
  --name openai-claims-bot \
  --resource-group rg-claims-bot-prod \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Deploy models
az cognitiveservices account deployment create \
  --name openai-claims-bot \
  --resource-group rg-claims-bot-prod \
  --deployment-name text-embedding-ada-002 \
  --model-name text-embedding-ada-002 \
  --model-version "2" \
  --model-format OpenAI \
  --sku-capacity 120 \
  --sku-name "Standard"

az cognitiveservices account deployment create \
  --name openai-claims-bot \
  --resource-group rg-claims-bot-prod \
  --deployment-name gpt-4-turbo \
  --model-name gpt-4 \
  --model-version "turbo-2024-04-09" \
  --model-format OpenAI \
  --sku-capacity 80 \
  --sku-name "Standard"

# 2. Azure AI Search
az search service create \
  --name search-claims-bot \
  --resource-group rg-claims-bot-prod \
  --sku Standard \
  --location eastus

# 3. Azure Cosmos DB
az cosmosdb create \
  --name cosmos-claims-bot \
  --resource-group rg-claims-bot-prod \
  --default-consistency-level Session \
  --locations regionName=eastus failoverPriority=0 \
  --enable-free-tier false

# Create database
az cosmosdb sql database create \
  --account-name cosmos-claims-bot \
  --resource-group rg-claims-bot-prod \
  --name ClaimsDatabase

# Create container
az cosmosdb sql container create \
  --account-name cosmos-claims-bot \
  --resource-group rg-claims-bot-prod \
  --database-name ClaimsDatabase \
  --name AuditTrail \
  --partition-key-path "/PolicyNumber" \
  --throughput 400

# 4. Azure Storage Account
az storage account create \
  --name stclaimsbot \
  --resource-group rg-claims-bot-prod \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2 \
  --access-tier Hot

# Create container
az storage container create \
  --name claims-documents \
  --account-name stclaimsbot \
  --public-access off

# 5. Document Intelligence
az cognitiveservices account create \
  --name docint-claims-bot \
  --resource-group rg-claims-bot-prod \
  --kind FormRecognizer \
  --sku S0 \
  --location eastus

# 6. Language Service
az cognitiveservices account create \
  --name lang-claims-bot \
  --resource-group rg-claims-bot-prod \
  --kind TextAnalytics \
  --sku S \
  --location eastus

# 7. Computer Vision
az cognitiveservices account create \
  --name vision-claims-bot \
  --resource-group rg-claims-bot-prod \
  --kind ComputerVision \
  --sku S1 \
  --location eastus
```

**Checklist:**
- ✅ Resource group created
- ✅ Azure OpenAI provisioned with 2 model deployments
- ✅ AI Search created (Standard tier)
- ✅ Cosmos DB created with database + container
- ✅ Storage account + container created
- ✅ Document Intelligence provisioned
- ✅ Language Service provisioned
- ✅ Computer Vision provisioned

### Phase 2: Retrieve Configuration Values (30 minutes)

```powershell
# Get Azure OpenAI endpoint and key
az cognitiveservices account show \
  --name openai-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query "properties.endpoint" -o tsv

az cognitiveservices account keys list \
  --name openai-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query "key1" -o tsv

# Get AI Search endpoint and keys
az search service show \
  --name search-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query "[endpoint]" -o tsv

az search admin-key show \
  --service-name search-claims-bot \
  --resource-group rg-claims-bot-prod

az search query-key list \
  --service-name search-claims-bot \
  --resource-group rg-claims-bot-prod

# Get Cosmos DB endpoint and key
az cosmosdb show \
  --name cosmos-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query "documentEndpoint" -o tsv

az cosmosdb keys list \
  --name cosmos-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query "primaryMasterKey" -o tsv

# Get Storage connection string
az storage account show-connection-string \
  --name stclaimsbot \
  --resource-group rg-claims-bot-prod \
  --query "connectionString" -o tsv

# Get Cognitive Services keys
az cognitiveservices account keys list \
  --name docint-claims-bot \
  --resource-group rg-claims-bot-prod

az cognitiveservices account keys list \
  --name lang-claims-bot \
  --resource-group rg-claims-bot-prod

az cognitiveservices account keys list \
  --name vision-claims-bot \
  --resource-group rg-claims-bot-prod
```

**Checklist:**
- ✅ OpenAI endpoint + API key retrieved
- ✅ AI Search endpoint + query key + admin key retrieved
- ✅ Cosmos DB endpoint + primary key retrieved
- ✅ Storage connection string retrieved
- ✅ All Cognitive Services keys retrieved

### Phase 3: Configure Application (30 minutes)

1. Update `appsettings.json` with all retrieved values
2. Set `"CloudProvider": "Azure"`
3. Test configuration locally
4. Store secrets in Azure Key Vault (production)

**Checklist:**
- ✅ appsettings.json updated with all endpoints
- ✅ CloudProvider set to "Azure"
- ✅ Local testing successful
- ✅ Secrets moved to Key Vault (production)

### Phase 4: Create AI Search Index (1 hour)

```csharp
// Run PolicyIngestion tool to create index + ingest policies
cd tools/PolicyIngestion
dotnet run
```

**Checklist:**
- ✅ Vector search index created
- ✅ Policy documents ingested
- ✅ Embeddings generated
- ✅ Search queries tested

### Phase 5: Deploy Application (1-2 hours)

**Option A: Azure App Service**
```powershell
# Create App Service Plan
az appservice plan create \
  --name plan-claims-bot \
  --resource-group rg-claims-bot-prod \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name app-claims-bot \
  --resource-group rg-claims-bot-prod \
  --plan plan-claims-bot \
  --runtime "DOTNETCORE:8.0"

# Deploy code
dotnet publish -c Release
az webapp deployment source config-zip \
  --resource-group rg-claims-bot-prod \
  --name app-claims-bot \
  --src publish.zip
```

**Option B: Azure Container Apps**
```powershell
# Create container registry
az acr create \
  --name acrclaimsbot \
  --resource-group rg-claims-bot-prod \
  --sku Basic

# Build and push Docker image
az acr build \
  --registry acrclaimsbot \
  --image claims-bot:latest \
  --file Dockerfile .

# Create Container App environment
az containerapp env create \
  --name env-claims-bot \
  --resource-group rg-claims-bot-prod \
  --location eastus

# Deploy container
az containerapp create \
  --name app-claims-bot \
  --resource-group rg-claims-bot-prod \
  --environment env-claims-bot \
  --image acrclaimsbot.azurecr.io/claims-bot:latest \
  --target-port 8080 \
  --ingress external
```

**Checklist:**
- ✅ App Service or Container App created
- ✅ Application deployed
- ✅ Environment variables configured
- ✅ Health check endpoint responding

### Phase 6: Testing & Validation (2-3 hours)

```powershell
# Test API endpoints
curl https://app-claims-bot.azurewebsites.net/health

# Test claim validation
curl -X POST https://app-claims-bot.azurewebsites.net/api/claims/validate \
  -H "Content-Type: application/json" \
  -d '{
    "PolicyNumber": "AFL-12345-HEALTH",
    "ClaimType": "Hospitalization",
    "ClaimAmount": 5000,
    "Description": "Emergency appendectomy surgery"
  }'

# Test document upload
curl -X POST https://app-claims-bot.azurewebsites.net/api/documents/upload \
  -F "file=@test-claim.pdf"
```

**Checklist:**
- ✅ Health endpoint returns 200
- ✅ Claim validation works end-to-end
- ✅ Document upload successful
- ✅ OCR extraction working
- ✅ Entities detected correctly
- ✅ Audit trail saved in Cosmos DB
- ✅ All Azure services responding

---

## Security & Access Management

### Azure RBAC Roles Required

| Service | Role | Purpose |
|---------|------|---------|
| Azure OpenAI | Cognitive Services OpenAI User | API access |
| AI Search | Search Service Contributor | Index management |
| Cosmos DB | Cosmos DB Account Contributor | Read/write data |
| Blob Storage | Storage Blob Data Contributor | Upload/download |
| Document Intelligence | Cognitive Services User | Document processing |
| Language Service | Cognitive Services User | NLP analysis |
| Computer Vision | Cognitive Services User | Image analysis |

### Managed Identity Setup (Recommended)

```powershell
# Enable managed identity on App Service
az webapp identity assign \
  --name app-claims-bot \
  --resource-group rg-claims-bot-prod

# Get managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name app-claims-bot \
  --resource-group rg-claims-bot-prod \
  --query principalId -o tsv)

# Grant access to Azure OpenAI
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/rg-claims-bot-prod/providers/Microsoft.CognitiveServices/accounts/openai-claims-bot

# Repeat for all services...
```

### Network Security

```yaml
Recommendations:
  - Private Endpoints: Enable for Cosmos DB, Storage, AI services
  - Virtual Network: Deploy App Service in VNet
  - Firewall Rules: Restrict AI Search to known IPs
  - API Key Rotation: 90-day rotation policy
  - Azure Key Vault: Store all secrets centrally
  - Managed Identity: Use instead of API keys where possible
```

---

## Monitoring & Alerts

### Azure Monitor Setup

```powershell
# Enable Application Insights
az monitor app-insights component create \
  --app insights-claims-bot \
  --location eastus \
  --resource-group rg-claims-bot-prod \
  --application-type web

# Link to App Service
az webapp config appsettings set \
  --name app-claims-bot \
  --resource-group rg-claims-bot-prod \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx"
```

### Recommended Alerts

| Alert | Metric | Threshold | Action |
|-------|--------|-----------|--------|
| High Azure OpenAI Cost | Daily spend | > $50 | Email team |
| AI Search Query Errors | Failed queries | > 5% | Page on-call |
| Cosmos DB Throttling | 429 errors | > 10/min | Auto-scale |
| Document Processing Failures | Failed OCR | > 10% | Email ops |
| Application Errors | 5xx responses | > 1% | Page on-call |

### Log Analytics Queries

```kusto
// Cost analysis per service
AzureActivity
| where CategoryValue == "Cost"
| summarize TotalCost=sum(Cost) by ResourceProvider
| order by TotalCost desc

// Claim processing performance
AppRequests
| where Name contains "/claims/validate"
| summarize 
    Count=count(),
    P50=percentile(DurationMs, 50),
    P95=percentile(DurationMs, 95),
    P99=percentile(DurationMs, 99)
| project Count, P50, P95, P99

// Failed document extractions
AppExceptions
| where OuterType contains "DocumentIntelligence"
| summarize Count=count() by ProblemId
| order by Count desc
```

---

## Summary

### What You Need to Get Started

1. **Azure Subscription** with appropriate credits/budget
2. **7 Azure Resources** provisioned (see checklist above)
3. **Configuration values** retrieved and stored in appsettings.json
4. **Policy documents** ready for ingestion
5. **Application deployed** to Azure App Service or Container Apps

### Total Setup Time

- Resource provisioning: 2-3 hours
- Configuration: 1 hour
- Policy ingestion: 1 hour
- Testing & validation: 2-3 hours
- **Total: 6-8 hours** for complete deployment

### Monthly Operating Cost

- **Standard deployment:** $951/month
- **Optimized deployment:** $450-500/month
- **Development environment:** $150-200/month

### Next Steps

1. ✅ Review this document
2. ✅ Get Azure subscription access
3. ✅ Run provisioning scripts (Phase 1)
4. ✅ Configure application (Phase 2-3)
5. ✅ Ingest policy documents (Phase 4)
6. ✅ Deploy and test (Phase 5-6)
7. ✅ Set up monitoring (optional but recommended)

---

**Document Maintained By:** Development Team  
**Last Updated:** February 10, 2026  
**Version:** 1.0
