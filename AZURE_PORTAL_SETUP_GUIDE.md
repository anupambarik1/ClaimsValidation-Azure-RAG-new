# Azure Portal Setup Guide - Step-by-Step
## Complete Portal Configuration for Claims RAG Bot

**Document Version:** 1.0  
**Last Updated:** February 10, 2026  
**Audience:** Developers with basic Azure knowledge  
**Estimated Time:** 4-6 hours for complete setup

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Provisioning Sequence & Dependencies](#provisioning-sequence--dependencies)
3. [Step 1: Create Resource Group](#step-1-create-resource-group)
4. [Step 2: Azure OpenAI Service](#step-2-azure-openai-service)
5. [Step 3: Azure AI Search](#step-3-azure-ai-search)
6. [Step 4: Azure Cosmos DB](#step-4-azure-cosmos-db)
7. [Step 5: Azure Blob Storage](#step-5-azure-blob-storage)
8. [Step 6: Azure Document Intelligence](#step-6-azure-document-intelligence)
9. [Step 7: Azure Language Service](#step-7-azure-language-service)
10. [Step 8: Azure Computer Vision](#step-8-azure-computer-vision)
11. [Step 9: Collect All Configuration Values](#step-9-collect-all-configuration-values)
12. [Step 10: Create AI Search Index & Ingest Policies](#step-10-create-ai-search-index--ingest-policies)
13. [Step 11: Test Connections](#step-11-test-connections)
14. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### What You Need Before Starting

✅ **Azure Subscription**
- Active Azure subscription with Owner or Contributor role
- Sufficient credits (free trial or paid subscription)
- Budget approval for ~$950/month operational costs

✅ **Access Requirements**
- Ability to create resources in Azure
- Permission to create Azure OpenAI resources (may require approval)
- Access to Azure Portal: https://portal.azure.com

✅ **Development Environment**
- Visual Studio Code or Visual Studio 2022
- .NET 8.0 SDK installed
- Git for version control

✅ **Browser**
- Modern browser (Chrome, Edge, Firefox)
- Pop-up blocker disabled for Azure Portal

### Important Notes

⚠️ **Azure OpenAI Access:** Some organizations require special approval for Azure OpenAI. Apply here: https://aka.ms/oai/access

⚠️ **Region Availability:** Not all services are available in all regions. Recommended regions:
- **East US** (best availability)
- **West Europe** (good alternative)
- **South Central US** (GPT-4 available)

⚠️ **Cost Awareness:** Resources start billing immediately after creation. Review cost estimates before provisioning.

---

## Provisioning Sequence & Dependencies

### ⚠️ **Important: Follow This Order**

Unlike AWS where some services have strict dependencies, Azure services are more independent. However, there's still a **recommended sequence** for efficient setup and testing.

### Recommended Provisioning Order

```
Phase 1: Foundation (20 min)
├── 1. Resource Group ✅ (No dependencies)
├── 2. Azure OpenAI ✅ (Deploy models: embeddings + GPT-4)
├── 3. Azure AI Search ✅ (Needs to exist before policy ingestion)
└── 4. Blob Storage ✅ (For policy documents)

Phase 2: Data & Processing (15 min)
├── 5. Cosmos DB ✅ (Can be done anytime)
├── 6. Document Intelligence ✅ (Can be done anytime)
├── 7. Language Service ✅ (Can be done anytime)
└── 8. Computer Vision ✅ (Can be done anytime)

Phase 3: Configuration & Testing (30 min)
├── 9. Update appsettings.json with all endpoints/keys
├── 10. Create AI Search Index (via REST API or tool)
├── 11. Run PolicyIngestion tool (populates vector DB)
└── 12. Test claim validation
```

### Critical Path (Must Follow This Sequence)

```
Azure OpenAI (with deployments)
    ↓
Azure AI Search (empty index)
    ↓
Policy Ingestion Tool (creates index + populates)
    ↓
Claims Validation Ready
```

**Why this order matters:**
- **Policy Ingestion** needs **Azure OpenAI** (for generating embeddings) + **AI Search** (to store vectors)
- **Claims Validation** needs **populated AI Search index** (for RAG retrieval)
- If you validate claims before policy ingestion, you'll get "Manual Review" status (no policy clauses found)

### Services You Can Provision in Parallel

These have **no dependencies** and can be created in any order:

✅ Cosmos DB  
✅ Blob Storage  
✅ Document Intelligence  
✅ Language Service  
✅ Computer Vision

### Quick Start Sequence (Minimal Setup)

If you want to test **basic claim validation** quickly:

```
1. Resource Group (2 min)
2. Azure OpenAI + model deployments (10 min)
3. Azure AI Search (5 min)
4. Update appsettings.json
5. Run policy ingestion tool (10 min)
6. Test claim validation ✅
```

Add other services later when testing document upload/extraction features.

### Time Estimates

| Phase | Services | Time | Can Parallelize? |
|-------|----------|------|------------------|
| **Phase 1** | Resource Group | 1 min | No (do first) |
| | Azure OpenAI + deployments | 10-15 min | No (do second) |
| | AI Search | 5 min | No (do third) |
| | Blob Storage | 3 min | Yes (with Phase 2) |
| **Phase 2** | Cosmos DB | 5 min | Yes (all together) |
| | Document Intelligence | 2 min | Yes (all together) |
| | Language Service | 2 min | Yes (all together) |
| | Computer Vision | 2 min | Yes (all together) |
| **Phase 3** | Configuration | 10 min | No |
| | Policy ingestion | 10-15 min | No (after config) |
| | Testing | 10 min | No (final step) |
| **Total** | | **60-75 min** | |

### Common Mistakes to Avoid

❌ **Don't:** Create AI Search index manually with wrong schema  
✅ **Do:** Let PolicyIngestion tool create the index (correct schema guaranteed)

❌ **Don't:** Try to validate claims before running policy ingestion  
✅ **Do:** Wait for policy ingestion to complete (vector DB populated)

❌ **Don't:** Deploy Azure OpenAI without creating both model deployments  
✅ **Do:** Deploy both `text-embedding-ada-002` and `gpt-4-turbo` before proceeding

❌ **Don't:** Use different regions for OpenAI and AI Search  
✅ **Do:** Keep services in same region (reduces latency for RAG pipeline)

❌ **Don't:** Forget to copy configuration values to notepad/file  
✅ **Do:** Save all endpoints/keys immediately after creating each service

---

## Step 1: Create Resource Group

### What is a Resource Group?
A logical container that holds related Azure resources. This keeps all your Claims Bot resources organized in one place.

### Portal Steps

1. **Navigate to Azure Portal**
   - Open browser and go to: https://portal.azure.com
   - Sign in with your Azure credentials

2. **Create Resource Group**
   - Click **"Create a resource"** (top left)
   - In search box, type: **"Resource Group"**
   - Click **"Resource Group"** from results
   - Click **"Create"** button

3. **Configure Resource Group**
   ```
   Subscription: [Select your subscription]
   Resource group name: rg-claims-bot-prod
   Region: East US
   ```
   
   **Naming Convention Tips:**
   - `rg-` prefix = resource group
   - `claims-bot` = project name
   - `prod` = environment (use `dev`, `staging`, `prod`)

4. **Add Tags (Optional but Recommended)**
   - Click **"Next: Tags"**
   - Add the following tags:
     ```
     Environment = Production
     Project = ClaimsBot
     Owner = YourName
     CostCenter = YourDepartment
     ```
   - Tags help with cost tracking and resource organization

5. **Create**
   - Click **"Review + create"**
   - Verify details
   - Click **"Create"**
   - Wait for "Deployment complete" message (5-10 seconds)

6. **Verify Creation**
   - Click **"Go to resource group"**
   - You should see an empty resource group
   - Bookmark this page - you'll return here often

**✅ Checkpoint:** Resource group `rg-claims-bot-prod` created successfully

---

## Step 2: Azure OpenAI Service

### What is Azure OpenAI?
Microsoft's managed service providing access to OpenAI's models (GPT-4, embeddings). This is the brain of your claims validation system.

### Portal Steps

#### 2.1 Create Azure OpenAI Resource

1. **Start Creation**
   - Stay in your resource group page
   - Click **"+ Create"** (top left)
   - Search: **"Azure OpenAI"**
   - Select **"Azure OpenAI"** (publisher: Microsoft)
   - Click **"Create"**

2. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Region: East US (or West Europe if East US unavailable)
   Name: openai-claims-bot
   Pricing tier: Standard S0
   ```

   **Region Selection Important:**
   - Check model availability: https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models
   - GPT-4 Turbo is only available in certain regions
   - East US usually has best availability

3. **Network Tab**
   ```
   Network connectivity: All networks, including the internet
   ```
   
   **Production Recommendation:** 
   - For production, select "Selected networks and private endpoints"
   - For now, use "All networks" for easier testing

4. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = OpenAI
   ```

5. **Review + Create**
   - Review all settings
   - Click **"Create"**
   - Wait 2-3 minutes for deployment
   - Click **"Go to resource"** when complete

**✅ Checkpoint:** Azure OpenAI resource created

#### 2.2 Deploy Text Embedding Model

1. **Navigate to Azure OpenAI Studio**
   - From your OpenAI resource page
   - Click **"Go to Azure OpenAI Studio"** (or click the link in the overview)
   - This opens: https://oai.azure.com

2. **Create Embedding Deployment**
   - In Azure OpenAI Studio, click **"Deployments"** (left menu)
   - Click **"+ Create new deployment"**
   
3. **Configure Embedding Deployment**
   ```
   Select a model: text-embedding-ada-002
   Model version: 2 (Default)
   Deployment name: text-embedding-ada-002
   Deployment type: Standard
   Tokens per Minute Rate Limit: 120K
   Content filter: DefaultV2
   Dynamic quota: Enabled
   ```

   **Why this model?**
   - Creates 1536-dimension vectors for semantic search
   - Industry standard for RAG applications
   - Cost-effective at $0.0001 per 1K tokens

4. **Create Deployment**
   - Click **"Create"**
   - Wait 30-60 seconds
   - Deployment status should show "Succeeded"

**✅ Checkpoint:** text-embedding-ada-002 deployed

#### 2.3 Deploy GPT-4 Turbo Model

1. **Create GPT-4 Deployment**
   - Still in "Deployments" page
   - Click **"+ Create new deployment"** again

2. **Configure GPT-4 Deployment**
   ```
   Select a model: gpt-4
   Model version: turbo-2024-04-09 (or latest turbo version)
   Deployment name: gpt-4-turbo
   Deployment type: Standard
   Tokens per Minute Rate Limit: 80K
   Content filter: DefaultV2
   Dynamic quota: Enabled
   ```

   **Alternative if GPT-4 Turbo unavailable:**
   ```
   Model: gpt-4o (GPT-4 Omni - newer, faster, cheaper)
   Deployment name: gpt-4-turbo (keep same name for compatibility)
   ```

3. **Create Deployment**
   - Click **"Create"**
   - Wait 1-2 minutes
   - Both deployments should now be visible

**✅ Checkpoint:** Both models deployed successfully

#### 2.4 Get Keys and Endpoint

1. **Navigate to Keys and Endpoint**
   - Close Azure OpenAI Studio
   - Return to Azure Portal
   - Go to your OpenAI resource: `openai-claims-bot`
   - Click **"Keys and Endpoint"** (left menu)

2. **Copy Configuration Values**
   ```
   KEY 1: [Click "Show Key" then copy]
   Endpoint: https://openai-claims-bot.openai.azure.com/
   Region: eastus
   ```

3. **Save to Notepad**
   - Open Notepad or text editor
   - Create a file: `azure-config.txt`
   - Paste these values:
   ```
   [Azure OpenAI]
   Endpoint: https://openai-claims-bot.openai.azure.com/
   API Key: [your-key-here]
   Embedding Deployment: text-embedding-ada-002
   Chat Deployment: gpt-4-turbo
   ```

**✅ Checkpoint:** OpenAI configuration captured

---

## Step 3: Azure AI Search

### What is Azure AI Search?
Vector database that stores policy clauses as embeddings and enables semantic search for RAG retrieval.

### Portal Steps

#### 3.1 Create AI Search Service

1. **Start Creation**
   - Return to resource group: `rg-claims-bot-prod`
   - Click **"+ Create"**
   - Search: **"Azure AI Search"** (or "Cognitive Search")
   - Click **"Create"**

2. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Service name: search-claims-bot
   Location: East US (same as OpenAI)
   Pricing tier: Standard (S1)
   ```

   **Pricing Tier Selection:**
   - **Free:** No vector search (not suitable)
   - **Basic:** $75/month, limited vectors (good for dev)
   - **Standard (S1):** $250/month, full vector search (recommended)
   
   For development, you can start with **Basic** and upgrade later.

3. **Scale Tab**
   ```
   Replicas: 1
   Partitions: 1
   ```
   
   **What these mean:**
   - Replicas = copies for high availability (1 is fine for dev)
   - Partitions = data shards (1 is fine for 10K documents)

4. **Networking Tab**
   ```
   Connectivity method: Public endpoint
   ```

5. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = AISearch
   ```

6. **Review + Create**
   - Review settings
   - Click **"Create"**
   - Wait 3-5 minutes for deployment
   - Click **"Go to resource"**

**✅ Checkpoint:** AI Search service created

#### 3.2 Get Search Keys

1. **Navigate to Keys**
   - In your AI Search resource
   - Click **"Keys"** (left menu under Settings)

2. **Copy Keys**
   ```
   Primary admin key: [Click "Show" then copy]
   Query key (default): [Copy]
   ```

3. **Get Endpoint**
   - Click **"Overview"** (top of left menu)
   - Copy the URL: `https://search-claims-bot.search.windows.net`

4. **Save to Configuration File**
   ```
   [Azure AI Search]
   Endpoint: https://search-claims-bot.search.windows.net/
   Admin API Key: [your-admin-key]
   Query API Key: [your-query-key]
   Index Name: policy-clauses (we'll create this later)
   ```

**✅ Checkpoint:** AI Search configuration captured

#### 3.3 Enable CORS (For Angular Frontend)

1. **Navigate to CORS Settings**
   - In AI Search resource
   - Click **"CORS"** (left menu under Settings)

2. **Add Allowed Origins**
   ```
   Allowed origins: *
   Allowed headers: *
   Allowed methods: GET, POST, PUT, DELETE, OPTIONS
   Max age: 3600
   ```

   **Production:** Replace `*` with your actual frontend URL (e.g., `https://claims-bot.azurewebsites.net`)

3. **Save Changes**

**✅ Checkpoint:** CORS configured for frontend access

---

## Step 4: Azure Cosmos DB

### What is Cosmos DB?
Globally distributed NoSQL database for storing claims audit trail and decision history.

### Portal Steps

#### 4.1 Create Cosmos DB Account

1. **Start Creation**
   - Return to resource group
   - Click **"+ Create"**
   - Search: **"Azure Cosmos DB"**
   - Select **"Azure Cosmos DB"**
   - Click **"Create"**

2. **Select API**
   - You'll see multiple API options:
   ```
   ○ Azure Cosmos DB for NoSQL (Core SQL)  ← SELECT THIS
   ○ MongoDB
   ○ PostgreSQL
   ○ Apache Cassandra
   ○ Table
   ○ Apache Gremlin
   ```
   - Click **"Create"** under "Azure Cosmos DB for NoSQL"

3. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Account name: cosmos-claims-bot
   Location: East US
   Capacity mode: Serverless (recommended for cost savings)
   Apply Free Tier Discount: Yes (if available)
   Limit total account throughput: No
   ```

   **Capacity Mode - Important Decision:**
   - **Serverless:** Pay per request (~$25/month for our usage)
   - **Provisioned:** Fixed cost ($24/month minimum for 400 RU/s)
   
   For this application, **Serverless** is better unless you have >1000 claims/day.

4. **Global Distribution Tab**
   ```
   Geo-Redundancy: Disable (single region is fine)
   Multi-region Writes: Disable
   Availability Zones: Disable (not needed for dev)
   ```

5. **Networking Tab**
   ```
   Connectivity method: Public endpoint (all networks)
   ```

6. **Backup Policy Tab**
   ```
   Backup policy: Periodic (default is fine)
   Backup interval: 4 hours
   Backup retention: 8 hours
   ```

7. **Encryption Tab**
   ```
   Use default (Microsoft-managed keys)
   ```

8. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = CosmosDB
   ```

9. **Review + Create**
   - Review all settings
   - Click **"Create"**
   - Wait 3-5 minutes for deployment
   - Click **"Go to resource"**

**✅ Checkpoint:** Cosmos DB account created

#### 4.2 Create Database

1. **Open Data Explorer**
   - In Cosmos DB resource
   - Click **"Data Explorer"** (left menu)
   - You'll see an interactive database browser

2. **Create New Database**
   - Click **"New Database"** (top toolbar)
   - OR click **"New Container"** and create database inline

3. **Database Settings**
   ```
   Database id: ClaimsDatabase
   ```
   
   **For Serverless:** No throughput settings needed
   
   **For Provisioned:** 
   ```
   ☑ Provision throughput
   Database throughput: Autoscale
   Max RU/s: 4000
   ```

4. **Create Database**
   - Click **"OK"**
   - Database appears in left sidebar

**✅ Checkpoint:** Database created

#### 4.3 Create Container (Table)

1. **Create Container**
   - In Data Explorer, hover over `ClaimsDatabase`
   - Click **"New Container"** (three dots menu)

2. **Container Settings**
   ```
   Database id: ClaimsDatabase (should be pre-selected)
   Container id: AuditTrail
   Partition key: /PolicyNumber
   Container throughput: (inherit from database or set to serverless)
   ```

   **Why `/PolicyNumber` as partition key?**
   - Most queries will filter by policy number
   - Evenly distributes data
   - Enables efficient point reads

3. **Advanced Settings (Optional)**
   ```
   Indexing policy: Automatic (default)
   Time to Live: Off (retain all history)
   Unique keys: None
   ```

4. **Create**
   - Click **"OK"**
   - Container appears under database

**✅ Checkpoint:** Container created and ready

#### 4.4 Get Connection Details

1. **Navigate to Keys**
   - Click **"Keys"** (left menu under Settings)

2. **Copy Connection Information**
   ```
   URI: https://cosmos-claims-bot.documents.azure.com:443/
   PRIMARY KEY: [Click "Show" then copy]
   PRIMARY CONNECTION STRING: [Optional - copy if needed]
   ```

3. **Save to Configuration File**
   ```
   [Azure Cosmos DB]
   Endpoint: https://cosmos-claims-bot.documents.azure.com:443/
   Primary Key: [your-key]
   Database ID: ClaimsDatabase
   Container ID: AuditTrail
   ```

**✅ Checkpoint:** Cosmos DB fully configured

---

## Step 5: Azure Blob Storage

### What is Blob Storage?
Object storage for uploaded claim documents, policy PDFs, and processed files.

### Portal Steps

#### 5.1 Create Storage Account

1. **Start Creation**
   - Return to resource group
   - Click **"+ Create"**
   - Search: **"Storage account"**
   - Click **"Create"**

2. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Storage account name: stclaimsbot (must be globally unique, lowercase, no hyphens)
   Region: East US
   Performance: Standard
   Redundancy: Locally-redundant storage (LRS)
   ```

   **Storage Account Name Rules:**
   - 3-24 characters
   - Lowercase letters and numbers only
   - Must be globally unique
   - If `stclaimsbot` is taken, try: `stclaimsbot2024`, `stclaimsbot<yourname>`, etc.

3. **Advanced Tab**
   ```
   Security:
     ☑ Require secure transfer for REST API operations
     ☑ Enable infrastructure encryption
     Minimum TLS version: Version 1.2
     Permitted scope for copy operations: From any storage account
   
   Data Lake Storage Gen2:
     ☐ Enable hierarchical namespace (not needed)
   
   Blob storage:
     Access tier: Hot (frequently accessed data)
     ☐ Enable versioning (optional)
     ☐ Enable blob soft delete (optional but recommended)
   
   Azure Files:
     [Leave defaults]
   ```

4. **Networking Tab**
   ```
   Network connectivity:
     ○ Enable public access from all networks (for testing)
     ● Enable public access from selected virtual networks (production)
   
   For now, select: Enable public access from all networks
   
   Routing preference: Microsoft network routing
   ```

5. **Data Protection Tab**
   ```
   Recovery:
     ☑ Enable soft delete for blobs (7 days retention)
     ☑ Enable soft delete for containers (7 days retention)
   
   Tracking:
     ☐ Enable versioning for blobs (costs more storage)
   
   Access control:
     ☐ Enable version-level immutability
   ```

6. **Encryption Tab**
   ```
   Encryption type: Microsoft-managed keys (MMK)
   ☑ Enable support for customer-managed keys: All service types
   ☑ Enable infrastructure encryption
   ```

7. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = BlobStorage
   ```

8. **Review + Create**
   - Click **"Create"**
   - Wait 1-2 minutes
   - Click **"Go to resource"**

**✅ Checkpoint:** Storage account created

#### 5.2 Create Blob Container

1. **Navigate to Containers**
   - In storage account resource
   - Click **"Containers"** (left menu under Data storage)

2. **Create New Container**
   - Click **"+ Container"** (top toolbar)

3. **Container Settings**
   ```
   Name: claims-documents
   Public access level: Private (no anonymous access)
   ```

   **Public Access Levels Explained:**
   - **Private:** Requires authentication (recommended)
   - **Blob:** Public read for blobs only
   - **Container:** Public read for containers and blobs

4. **Create**
   - Click **"Create"**
   - Container appears in list

**✅ Checkpoint:** Blob container created

#### 5.3 Enable CORS

1. **Navigate to CORS Settings**
   - In storage account
   - Click **"Resource sharing (CORS)"** (left menu under Settings)
   - Click **"Blob service"** tab

2. **Add CORS Rule**
   ```
   Allowed origins: * (or your frontend URL)
   Allowed methods: GET, PUT, POST, DELETE, OPTIONS
   Allowed headers: *
   Exposed headers: *
   Max age: 3600
   ```

3. **Save**
   - Click **"Save"** button at top

**✅ Checkpoint:** CORS enabled for file uploads

#### 5.4 Get Connection String

1. **Navigate to Access Keys**
   - Click **"Access keys"** (left menu under Security + networking)

2. **Copy Connection String**
   - Under **key1**, click **"Show"** next to Connection string
   - Click **Copy** icon
   ```
   DefaultEndpointsProtocol=https;AccountName=stclaimsbot;AccountKey=xxxxx;EndpointSuffix=core.windows.net
   ```

3. **Save to Configuration File**
   ```
   [Azure Blob Storage]
   Connection String: [your-connection-string]
   Container Name: claims-documents
   Upload Prefix: uploads/
   SAS Token Expiration: 3600
   ```

**✅ Checkpoint:** Storage configuration captured

---

## Step 6: Azure Document Intelligence

### What is Document Intelligence?
AI service for OCR and document extraction (text, tables, forms) from PDFs and images.

### Portal Steps

#### 6.1 Create Document Intelligence Resource

1. **Start Creation**
   - Return to resource group
   - Click **"+ Create"**
   - Search: **"Document Intelligence"** (or "Form Recognizer")
   - Click **"Create"**

2. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Region: East US
   Name: docint-claims-bot
   Pricing tier: S0 (Standard)
   ```

   **Pricing Tiers:**
   - **F0 (Free):** 500 pages/month free (good for testing)
   - **S0 (Standard):** Pay-as-you-go ($10/1K pages for prebuilt-document)

3. **Network Tab**
   ```
   Type: All networks, including the internet
   ```

4. **Identity Tab**
   ```
   System assigned managed identity: Off (can enable later)
   ```

5. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = DocumentIntelligence
   ```

6. **Review + Create**
   - Click **"Create"**
   - Wait 1-2 minutes
   - Click **"Go to resource"**

**✅ Checkpoint:** Document Intelligence created

#### 6.2 Get Keys and Endpoint

1. **Navigate to Keys and Endpoint**
   - Click **"Keys and Endpoint"** (left menu)

2. **Copy Configuration**
   ```
   KEY 1: [Click "Show" then copy]
   Endpoint: https://docint-claims-bot.cognitiveservices.azure.com/
   Location: eastus
   ```

3. **Save to Configuration File**
   ```
   [Azure Document Intelligence]
   Endpoint: https://docint-claims-bot.cognitiveservices.azure.com/
   API Key: [your-key]
   Model ID: prebuilt-read (for simple text) or prebuilt-document (for tables)
   ```

**✅ Checkpoint:** Document Intelligence configured

#### 6.3 Test Document Intelligence (Optional)

1. **Open Document Intelligence Studio**
   - Go to: https://documentintelligence.ai.azure.com/studio
   - Sign in with Azure credentials

2. **Select Your Resource**
   - Click **"Settings"** (gear icon, top right)
   - Select your subscription and resource: `docint-claims-bot`
   - Click **"Use resource"**

3. **Try a Sample**
   - Click **"Read"** model
   - Upload a sample PDF or use provided samples
   - Click **"Run analysis"**
   - Verify text extraction works

**✅ Checkpoint:** Document Intelligence tested successfully

----------------------------------------------------------------------------------------------------------------------------------------------

## Step 7: Azure Language Service

### What is Language Service?
NLP service for extracting entities (names, dates, amounts) and key phrases from text.

### Portal Steps

#### 7.1 Create Language Service Resource

1. **Start Creation**
   - Return to resource group
   - Click **"+ Create"**
   - Search: **"Language service"** (or "Text Analytics")
   - Select **"Language service"**
   - Click **"Create"**

2. **Select Features**
   - You may see a features selection page
   - Ensure these are checked:
   ```
   ☑ Named Entity Recognition (NER)
   ☑ Key Phrase Extraction
   ☑ Entity Linking (optional)
   ```
   - Click **"Continue to create your resource"**

3. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Region: East US
   Name: lang-claims-bot
   Pricing tier: S (Standard) - $2 per 1000 text records
   ```

   **Pricing Tiers:**
   - **F0 (Free):** 5,000 text records/month (great for testing)
   - **S (Standard):** Pay-as-you-go

4. **Network Tab**
   ```
   Type: All networks, including the internet
   ```

5. **Identity Tab**
   ```
   System assigned managed identity: Off
   ```

6. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = LanguageService
   ```

7. **Review + Create**
   - Click **"Create"**
   - Wait 1-2 minutes
   - Click **"Go to resource"**

**✅ Checkpoint:** Language Service created

#### 7.2 Get Keys and Endpoint

1. **Navigate to Keys and Endpoint**
   - Click **"Keys and Endpoint"** (left menu)

2. **Copy Configuration**
   ```
   KEY 1: [Copy]
   Endpoint: https://lang-claims-bot.cognitiveservices.azure.com/
   Location/Region: eastus
   ```

3. **Save to Configuration File**
   ```
   [Azure Language Service]
   Endpoint: https://lang-claims-bot.cognitiveservices.azure.com/
   API Key: [your-key]
   ```

**✅ Checkpoint:** Language Service configured

#### 7.3 Test Language Service (Optional)

1. **Open Language Studio**
   - Go to: https://language.cognitive.azure.com
   - Sign in with Azure credentials

2. **Select Your Resource**
   - Select subscription and resource: `lang-claims-bot`

3. **Test NER**
   - Click **"Named Entity Recognition"**
   - Enter sample text: "John Smith filed a claim for $5,000 on January 15, 2026 for car accident in New York"
   - Click **"Run"**
   - Verify entities detected: Person (John Smith), Money ($5,000), Date (January 15, 2026), Location (New York)

**✅ Checkpoint:** Language Service tested

---

## Step 8: Azure Computer Vision

### What is Computer Vision?
AI service for analyzing images - detects objects, assesses quality, identifies damage in claim photos.

### Portal Steps

#### 8.1 Create Computer Vision Resource

1. **Start Creation**
   - Return to resource group
   - Click **"+ Create"**
   - Search: **"Computer Vision"**
   - Select **"Computer Vision"**
   - Click **"Create"**

2. **Basics Tab**
   ```
   Subscription: [Your subscription]
   Resource group: rg-claims-bot-prod
   Region: East US
   Name: vision-claims-bot
   Pricing tier: S1 (Standard)
   ```

   **Pricing Tiers:**
   - **F0 (Free):** 5,000 transactions/month (good for testing)
   - **S1 (Standard):** $1 per 1,000 transactions

3. **Network Tab**
   ```
   Type: All networks, including the internet
   ```

4. **Identity Tab**
   ```
   System assigned managed identity: Off
   ```

5. **Tags Tab**
   ```
   Environment = Production
   Project = ClaimsBot
   Service = ComputerVision
   ```

6. **Review + Create**
   - Click **"Create"**
   - Wait 1-2 minutes
   - Click **"Go to resource"**

**✅ Checkpoint:** Computer Vision created

#### 8.2 Get Keys and Endpoint

1. **Navigate to Keys and Endpoint**
   - Click **"Keys and Endpoint"** (left menu)

2. **Copy Configuration**
   ```
   KEY 1: [Copy]
   Endpoint: https://vision-claims-bot.cognitiveservices.azure.com/
   Location: eastus
   ```

3. **Save to Configuration File**
   ```
   [Azure Computer Vision]
   Endpoint: https://vision-claims-bot.cognitiveservices.azure.com/
   API Key: [your-key]
   Min Confidence: 0.7
   ```

**✅ Checkpoint:** Computer Vision configured

#### 8.3 Test Computer Vision (Optional)

1. **Open Vision Studio**
   - Go to: https://portal.vision.cognitive.azure.com
   - Sign in with Azure credentials

2. **Select Your Resource**
   - Select subscription and resource: `vision-claims-bot`

3. **Test Image Analysis**
   - Click **"Analyze images"**
   - Upload a sample image or use URL
   - Select visual features to extract
   - Click **"Analyze"**
   - Review detected objects, tags, descriptions

**✅ Checkpoint:** Computer Vision tested

---

## Step 9: Collect All Configuration Values

### Complete Configuration Document

By now, you should have collected all these values. Let's organize them into your `appsettings.json` format.

#### 9.1 Create Configuration File

Open your project in VS Code and locate:
```
c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api\appsettings.json
```

#### 9.2 Update appsettings.json

Replace the Azure section with your actual values:

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
      "Endpoint": "https://openai-claims-bot.openai.azure.com/",
      "ApiKey": "YOUR_OPENAI_KEY_HERE",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4-turbo"
    },
    "AISearch": {
      "Endpoint": "https://search-claims-bot.search.windows.net/",
      "QueryApiKey": "YOUR_QUERY_KEY_HERE",
      "AdminApiKey": "YOUR_ADMIN_KEY_HERE",
      "IndexName": "policy-clauses"
    },
    "CosmosDB": {
      "Endpoint": "https://cosmos-claims-bot.documents.azure.com:443/",
      "Key": "YOUR_COSMOS_KEY_HERE",
      "DatabaseId": "ClaimsDatabase",
      "ContainerId": "AuditTrail"
    },
    "BlobStorage": {
      "ConnectionString": "YOUR_STORAGE_CONNECTION_STRING_HERE",
      "ContainerName": "claims-documents",
      "UploadPrefix": "uploads/",
      "SasTokenExpiration": 3600
    },
    "DocumentIntelligence": {
      "Endpoint": "https://docint-claims-bot.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_DOCINT_KEY_HERE",
      "ModelId": "prebuilt-read"
    },
    "LanguageService": {
      "Endpoint": "https://lang-claims-bot.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_LANGUAGE_KEY_HERE"
    },
    "ComputerVision": {
      "Endpoint": "https://vision-claims-bot.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_VISION_KEY_HERE",
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

#### 9.3 Verify All Values

**Checklist - Ensure you have filled in:**
- ✅ Azure.OpenAI.Endpoint
- ✅ Azure.OpenAI.ApiKey
- ✅ Azure.AISearch.Endpoint
- ✅ Azure.AISearch.QueryApiKey
- ✅ Azure.AISearch.AdminApiKey
- ✅ Azure.CosmosDB.Endpoint
- ✅ Azure.CosmosDB.Key
- ✅ Azure.BlobStorage.ConnectionString
- ✅ Azure.DocumentIntelligence.Endpoint
- ✅ Azure.DocumentIntelligence.ApiKey
- ✅ Azure.LanguageService.Endpoint
- ✅ Azure.LanguageService.ApiKey
- ✅ Azure.ComputerVision.Endpoint
- ✅ Azure.ComputerVision.ApiKey

**Security Warning:** 
- ⚠️ Never commit this file with real keys to Git
- ⚠️ Add `appsettings.json` to `.gitignore`
- ✅ Use Azure Key Vault for production

#### 9.4 Create appsettings.Development.json

For local development, create a separate file:

```json
{
  "CloudProvider": "Azure",
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://openai-claims-bot.openai.azure.com/",
      "ApiKey": "YOUR_OPENAI_KEY_HERE",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4-turbo"
    }
    // ... rest of configuration
  }
}
```

**✅ Checkpoint:** Configuration files updated with all Azure values

---

## Step 10: Test Connections

### Verify Azure Services are Accessible

#### 10.1 Test from Azure Portal

1. **Azure OpenAI Test**
   - Go to Azure OpenAI Studio: https://oai.azure.com
   - Navigate to **"Chat playground"**
   - Select deployment: `gpt-4-turbo`
   - Send test message: "Hello, test connection"
   - ✅ Should receive response

2. **AI Search Test**
   - Go to Search resource in portal
   - Click **"Search explorer"**
   - Click **"Search"** (empty query)
   - ✅ Should return empty results (no error)

3. **Cosmos DB Test**
   - Go to Cosmos DB resource
   - Click **"Data Explorer"**
   - Expand `ClaimsDatabase` → `AuditTrail`
   - Click **"Items"**
   - ✅ Should show empty container

4. **Blob Storage Test**
   - Go to Storage Account
   - Click **"Containers"**
   - Click `claims-documents`
   - ✅ Should show empty container

#### 10.2 Test from .NET Application

1. **Open Terminal in VS Code**
   ```powershell
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
   ```

2. **Build Application**
   ```powershell
   dotnet build
   ```
   - ✅ Should compile without errors

3. **Run Application**
   ```powershell
   dotnet run
   ```
   - ✅ Should start successfully
   - ✅ Check console for any Azure connection errors

4. **Test Health Endpoint**
   ```powershell
   # In new terminal
   curl http://localhost:5000/health
   ```
   - ✅ Should return 200 OK

#### 10.3 Test Individual Services

**Test Azure OpenAI Connection:**
```powershell
# Create test file: test-openai.ps1
$endpoint = "https://openai-claims-bot.openai.azure.com/openai/deployments/text-embedding-ada-002/embeddings?api-version=2024-02-01"
$headers = @{
    "api-key" = "YOUR_OPENAI_KEY"
    "Content-Type" = "application/json"
}
$body = @{
    input = "test connection"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body
$response
```

**Test AI Search Connection:**
```powershell
$endpoint = "https://search-claims-bot.search.windows.net/indexes?api-version=2023-11-01"
$headers = @{
    "api-key" = "YOUR_ADMIN_KEY"
}

$response = Invoke-RestMethod -Uri $endpoint -Method Get -Headers $headers
$response
```

**Test Cosmos DB Connection:**
```powershell
# Use Azure CLI
az cosmosdb sql database list `
  --account-name cosmos-claims-bot `
  --resource-group rg-claims-bot-prod
```

**✅ Checkpoint:** All services responding correctly

---

## Step 10: Create AI Search Index & Ingest Policies

### ⚠️ Critical Step - Required for RAG Functionality

This step creates the vector search index and populates it with policy document embeddings. **Without this, claim validation will not work.**

### Why This Step is Essential

The Claims RAG Bot uses **Retrieval-Augmented Generation (RAG)**:
1. User submits claim description
2. System converts description to embedding vector (Azure OpenAI)
3. System searches for similar policy clauses (AI Search vector search)
4. Retrieved clauses are sent to GPT-4 for decision-making

**If the index is empty or doesn't exist:** No policy clauses retrieved → AI cannot make informed decision → Returns "Manual Review" status.

---

### Step 10.1: Create AI Search Index

#### Option 1: Using PolicyIngestion Tool (Recommended)

The PolicyIngestion tool automatically:
- Creates the correct index schema with vector search configuration
- Uploads policy documents
- Generates embeddings via Azure OpenAI
- Indexes all policy clauses

**Steps:**

1. **Navigate to PolicyIngestion Project**
   ```powershell
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion
   ```

2. **Update appsettings.json in PolicyIngestion Folder**
   
   Create or update `appsettings.json`:
   ```json
   {
     "CloudProvider": "Azure",
     "Azure": {
       "OpenAI": {
         "Endpoint": "https://openai-claims-bot.openai.azure.com/",
         "ApiKey": "YOUR_OPENAI_KEY",
         "EmbeddingDeployment": "text-embedding-ada-002"
       },
       "AISearch": {
         "Endpoint": "https://search-claims-bot.search.windows.net/",
         "AdminApiKey": "YOUR_ADMIN_KEY",
         "IndexName": "policy-clauses"
       }
     }
   }
   ```

3. **Run Policy Ingestion**
   ```powershell
   dotnet run
   ```

4. **Expected Output**
   ```
   Starting Policy Ingestion...
   Cloud Provider: Azure
   Creating AI Search index 'policy-clauses'...
   Index created successfully.
   
   Processing policy documents from TestDocuments/...
   - Processing: health_insurance_policy.txt
   - Processing: motor_insurance_policy.txt
   - Processing: life_insurance_policy.txt
   
   Generating embeddings for 487 policy clauses...
   Progress: 100/487 (20%)
   Progress: 200/487 (41%)
   Progress: 300/487 (61%)
   Progress: 400/487 (82%)
   Progress: 487/487 (100%)
   
   Uploading to Azure AI Search...
   Batch 1/5 uploaded successfully.
   Batch 2/5 uploaded successfully.
   Batch 3/5 uploaded successfully.
   Batch 4/5 uploaded successfully.
   Batch 5/5 uploaded successfully.
   
   ✅ Policy ingestion completed successfully!
   Total clauses indexed: 487
   Time taken: 8 minutes 32 seconds
   ```

5. **Verify Index Created**
   - Go to AI Search resource in Azure Portal
   - Click **"Indexes"** (left menu)
   - ✅ You should see `policy-clauses` index with 487 documents

---

#### Option 2: Manual Index Creation via REST API

If you prefer to create the index manually first:

**Create Index Definition File** (`create-index.json`):

```json
{
  "name": "policy-clauses",
  "fields": [
    {
      "name": "ClauseId",
      "type": "Edm.String",
      "key": true,
      "filterable": false,
      "searchable": false,
      "retrievable": true
    },
    {
      "name": "Text",
      "type": "Edm.String",
      "searchable": true,
      "filterable": false,
      "retrievable": true,
      "analyzer": "en.microsoft"
    },
    {
      "name": "PolicyType",
      "type": "Edm.String",
      "filterable": true,
      "facetable": true,
      "retrievable": true
    },
    {
      "name": "CoverageType",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "Section",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "Embedding",
      "type": "Collection(Edm.Single)",
      "dimensions": 1536,
      "vectorSearchProfile": "vector-profile-1536",
      "retrievable": true,
      "searchable": true
    }
  ],
  "vectorSearch": {
    "profiles": [
      {
        "name": "vector-profile-1536",
        "algorithm": "hnsw-algorithm"
      }
    ],
    "algorithms": [
      {
        "name": "hnsw-algorithm",
        "kind": "hnsw",
        "hnswParameters": {
          "m": 4,
          "efConstruction": 400,
          "efSearch": 500,
          "metric": "cosine"
        }
      }
    ]
  },
  "semantic": {
    "configurations": [
      {
        "name": "semantic-config",
        "prioritizedFields": {
          "contentFields": [
            {
              "fieldName": "Text"
            }
          ]
        }
      }
    ]
  }
}
```

**Create Index via PowerShell:**

```powershell
$endpoint = "https://search-claims-bot.search.windows.net/indexes?api-version=2023-11-01"
$headers = @{
    "api-key" = "YOUR_ADMIN_KEY"
    "Content-Type" = "application/json"
}

$body = Get-Content create-index.json -Raw

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body
$response
```

Then run the PolicyIngestion tool to populate it.

---

### Step 10.2: Verify Policy Documents Indexed

**Method 1: Azure Portal - Search Explorer**

1. Go to AI Search resource in Azure Portal
2. Click **"Search explorer"** (top toolbar)
3. Run empty search query:
   ```json
   {
     "search": "*",
     "top": 10
   }
   ```
4. Click **"Search"**
5. ✅ Should return policy clauses with embeddings

**Method 2: PowerShell Query**

```powershell
$endpoint = "https://search-claims-bot.search.windows.net/indexes/policy-clauses/docs?api-version=2023-11-01&search=*&$top=5"
$headers = @{
    "api-key" = "YOUR_QUERY_KEY"
}

$response = Invoke-RestMethod -Uri $endpoint -Method Get -Headers $headers
$response.value | ConvertTo-Json -Depth 5
```

**Expected Response:**
```json
{
  "@odata.context": "...",
  "value": [
    {
      "@search.score": 1.0,
      "ClauseId": "CLAUSE-HEALTH-001",
      "Text": "Section 3.2: Emergency hospitalization coverage applies to...",
      "PolicyType": "Health",
      "CoverageType": "Hospitalization",
      "Section": "3.2",
      "Embedding": [0.123, -0.456, 0.789, ...]
    },
    ...
  ]
}
```

**✅ Checkpoint:** Policy clauses successfully indexed with embeddings

---

### Step 10.3: Test Vector Search

Verify that semantic search works:

```powershell
# Test vector search with a sample claim description
$searchEndpoint = "https://search-claims-bot.search.windows.net/indexes/policy-clauses/docs/search?api-version=2023-11-01"
$headers = @{
    "api-key" = "YOUR_QUERY_KEY"
    "Content-Type" = "application/json"
}

$body = @{
    search = "emergency surgery hospitalization"
    top = 5
    select = "ClauseId,Text,PolicyType,Section"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $searchEndpoint -Method Post -Headers $headers -Body $body
$response.value | Format-Table ClauseId, PolicyType, Section
```

✅ Should return relevant clauses about hospitalization and surgery coverage

---

### Step 10.4: Test End-to-End Claim Validation

Now that the vector database is populated, test the complete RAG pipeline:

```powershell
# Make sure your API is running
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
dotnet run

# In a new terminal, test claim validation
curl -X POST http://localhost:5000/api/claims/validate `
  -H "Content-Type: application/json" `
  -d '{
    "policyNumber": "AFL-12345-HEALTH",
    "policyType": "Health",
    "claimAmount": 3000,
    "claimDescription": "Emergency surgery for appendicitis with 3 days hospitalization"
  }'
```

**Expected Response (Success):**
```json
{
  "status": "Covered",
  "explanation": "The claim for emergency appendicitis surgery is covered under Section 3.2 (Emergency Hospitalization) and Section 5.1 (Surgical Procedures). The claim amount of $3,000.00 is within policy limits...",
  "clauseReferences": [
    "CLAUSE-HEALTH-3.2.1",
    "CLAUSE-HEALTH-5.1.3"
  ],
  "requiredDocuments": [
    "Hospital admission letter",
    "Medical bills and invoices",
    "Discharge summary",
    "Surgical procedure report"
  ],
  "confidenceScore": 0.92
}
```

**If you get "Manual Review" status:**
- ✅ Check that policy ingestion completed successfully
- ✅ Verify AI Search index has documents (step 10.2)
- ✅ Verify Azure OpenAI embeddings deployment is working
- ✅ Check application logs for errors

**✅ Checkpoint:** Complete RAG pipeline working - policies retrieved, AI makes informed decision

---

## Step 11: Test Connections

### Complete System Verification

After completing all steps including policy ingestion, perform comprehensive system tests.

#### 11.1 Verify All Azure Resources

**List all resources in resource group:**

```powershell
az resource list --resource-group rg-claims-bot-prod --output table
```

**Expected resources (7 total):**
- ✅ Azure OpenAI Service
- ✅ AI Search Service  
- ✅ Cosmos DB Account
- ✅ Storage Account
- ✅ Document Intelligence
- ✅ Language Service
- ✅ Computer Vision

---

#### 11.2 Verify Vector Database is Populated

```powershell
# Check document count in AI Search index
$endpoint = "https://search-claims-bot.search.windows.net/indexes/policy-clauses/docs/$count?api-version=2023-11-01"
$headers = @{
    "api-key" = "YOUR_QUERY_KEY"
}

$count = Invoke-RestMethod -Uri $endpoint -Method Get -Headers $headers
Write-Host "Total policy clauses indexed: $count"
```

✅ Should show 400+ documents (depending on your policy files)

---

#### 11.3 Test Individual Azure Services

1. **Create Index Definition File**

Save as `create-index.json`:

```json
{
  "name": "policy-clauses",
  "fields": [
    {
      "name": "ClauseId",
      "type": "Edm.String",
      "key": true,
      "filterable": false,
      "searchable": false,
      "retrievable": true
    },
    {
      "name": "Text",
      "type": "Edm.String",
      "searchable": true,
      "filterable": false,
      "retrievable": true,
      "analyzer": "en.microsoft"
    },
    {
      "name": "PolicyType",
      "type": "Edm.String",
      "filterable": true,
      "facetable": true,
      "retrievable": true
    },
    {
      "name": "CoverageType",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "Section",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "Embedding",
      "type": "Collection(Edm.Single)",
      "dimensions": 1536,
      "vectorSearchProfile": "vector-profile-1536",
      "retrievable": true,
      "searchable": true
    }
  ],
  "vectorSearch": {
    "profiles": [
      {
        "name": "vector-profile-1536",
        "algorithm": "hnsw-algorithm"
      }
    ],
    "algorithms": [
      {
        "name": "hnsw-algorithm",
        "kind": "hnsw",
        "hnswParameters": {
          "m": 4,
          "efConstruction": 400,
          "efSearch": 500,
          "metric": "cosine"
        }
      }
    ]
  },
  "semantic": {
    "configurations": [
      {
        "name": "semantic-config",
        "prioritizedFields": {
          "contentFields": [
            {
              "fieldName": "Text"
            }
          ]
        }
      }
    ]
  }
}
```

2. **Create Index via PowerShell**

```powershell
$endpoint = "https://search-claims-bot.search.windows.net/indexes?api-version=2023-11-01"
$headers = @{
    "api-key" = "YOUR_ADMIN_KEY"
    "Content-Type" = "application/json"
}

$body = Get-Content create-index.json -Raw

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body
$response
```

3. **Verify Index Created**
   - Go to AI Search resource in portal
   - Click **"Indexes"** (left menu)
   - ✅ You should see `policy-clauses` index

**✅ Checkpoint:** Vector search index created

#### 11.2 Ingest Policy Documents

1. **Run Policy Ingestion Tool**

```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion

# Update appsettings.json in PolicyIngestion folder with Azure config
dotnet run
```

2. **This tool will:**
   - Load policy documents from `TestDocuments/` folder
   - Split into clauses
   - Generate embeddings via Azure OpenAI
   - Upload to AI Search index

3. **Verify Documents Indexed**
   - Go to AI Search resource
   - Click **"Search explorer"**
   - Run query: `search=*`
   - ✅ Should return policy clauses with embeddings

**✅ Checkpoint:** Policy documents indexed and searchable

---

## Step 12: Deploy Application (Optional)

### Deploy to Azure App Service

#### 12.1 Create App Service

1. **In Azure Portal**
   - Go to resource group
   - Click **"+ Create"**
   - Search: **"Web App"**
   - Click **"Create"**

2. **Basics Tab**
   ```
   Resource group: rg-claims-bot-prod
   Name: app-claims-bot (must be globally unique)
   Publish: Code
   Runtime stack: .NET 8 (LTS)
   Operating System: Linux
   Region: East US
   Pricing plan: Basic B1 (~$13/month) or Free F1 for testing
   ```

3. **Create**
   - Click **"Review + create"**
   - Click **"Create"**
   - Wait for deployment

#### 12.2 Configure App Service

1. **Add Application Settings**
   - Go to App Service resource
   - Click **"Configuration"** (left menu)
   - Click **"+ New application setting"**
   - Add each Azure configuration value as environment variables:
   ```
   CloudProvider = Azure
   Azure__OpenAI__Endpoint = https://...
   Azure__OpenAI__ApiKey = xxx
   (etc for all services)
   ```

2. **Save Configuration**
   - Click **"Save"** at top
   - Click **"Continue"** to restart app

#### 12.3 Deploy Code

**Option 1: VS Code Extension**
1. Install **Azure App Service** extension in VS Code
2. Right-click on App Service
3. Select **"Deploy to Web App"**

**Option 2: Azure CLI**
```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api

# Publish app
dotnet publish -c Release -o ./publish

# Create ZIP
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip

# Deploy
az webapp deployment source config-zip `
  --resource-group rg-claims-bot-prod `
  --name app-claims-bot `
  --src deploy.zip
```

**Option 3: GitHub Actions**
- Set up CI/CD pipeline (recommended for production)

#### 12.4 Test Deployed Application

```powershell
# Test health endpoint
curl https://app-claims-bot.azurewebsites.net/health

# Test claim validation
curl -X POST https://app-claims-bot.azurewebsites.net/api/claims/validate `
  -H "Content-Type: application/json" `
  -d '{
    "PolicyNumber": "AFL-12345-HEALTH",
    "ClaimType": "Hospitalization",
    "ClaimAmount": 5000,
    "Description": "Emergency surgery"
  }'
```

**✅ Checkpoint:** Application deployed and running in Azure

---

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: Azure OpenAI Access Denied

**Error:** "Access denied to Azure OpenAI service"

**Solutions:**
1. Apply for Azure OpenAI access: https://aka.ms/oai/access
2. Check if your subscription has been approved
3. Verify region supports GPT-4 Turbo
4. Check API key is correct (regenerate if needed)

#### Issue 2: AI Search Index Creation Fails

**Error:** "Vector search not supported in this tier"

**Solutions:**
1. Upgrade to Basic or Standard tier (Free doesn't support vectors)
2. Verify API version is 2023-11-01 or later
3. Check dimension is exactly 1536

#### Issue 3: Cosmos DB Connection Timeout

**Error:** "Request timeout to Cosmos DB"

**Solutions:**
1. Check firewall rules allow your IP
2. Verify connection string format
3. Check if serverless mode has cold start (wait 30 seconds)
4. Ensure database and container exist

#### Issue 4: Blob Storage 403 Forbidden

**Error:** "403 Forbidden when uploading to blob"

**Solutions:**
1. Verify connection string includes account key
2. Check container exists
3. Ensure CORS is enabled
4. Verify container access level (should be Private with SAS tokens)

#### Issue 5: Document Intelligence Rate Limit

**Error:** "429 Too Many Requests"

**Solutions:**
1. Check you're on S0 tier (F0 has strict limits)
2. Implement retry logic with exponential backoff
3. Batch process documents during off-peak hours

#### Issue 6: Language Service Returns Empty Results

**Error:** "NER returns no entities"

**Solutions:**
1. Verify text is in English (check language code)
2. Ensure text is >5 characters
3. Check API key is valid
4. Verify endpoint URL is correct

#### Issue 7: Build Errors After Switching to Azure

**Error:** "Service not registered" or "NullReferenceException"

**Solutions:**
1. Ensure CloudProvider is set to "Azure" in config
2. Check all Azure service implementations exist in Infrastructure layer
3. Verify dependency injection in Program.cs
4. Rebuild solution: `dotnet clean && dotnet build`

---

## Security Best Practices

### Recommended Security Enhancements

#### 1. Use Azure Key Vault

Instead of storing keys in appsettings.json:

1. **Create Key Vault**
   ```powershell
   az keyvault create `
     --name kv-claims-bot `
     --resource-group rg-claims-bot-prod `
     --location eastus
   ```

2. **Store Secrets**
   ```powershell
   az keyvault secret set --vault-name kv-claims-bot --name "AzureOpenAI-ApiKey" --value "your-key"
   az keyvault secret set --vault-name kv-claims-bot --name "CosmosDB-Key" --value "your-key"
   # ... etc
   ```

3. **Update appsettings.json**
   ```json
   "Azure": {
     "OpenAI": {
       "ApiKey": "@Microsoft.KeyVault(SecretUri=https://kv-claims-bot.vault.azure.net/secrets/AzureOpenAI-ApiKey)"
     }
   }
   ```

#### 2. Enable Managed Identity

For App Service to access resources without keys:

```powershell
# Enable managed identity
az webapp identity assign --name app-claims-bot --resource-group rg-claims-bot-prod

# Grant access to Key Vault
az keyvault set-policy --name kv-claims-bot `
  --object-id <managed-identity-principal-id> `
  --secret-permissions get list
```

#### 3. Network Security

1. **Private Endpoints** (production)
   - Create VNet
   - Enable private endpoints for Cosmos DB, Storage, AI services
   - Disable public access

2. **Firewall Rules**
   - Restrict AI Search to known IPs
   - Enable Storage firewall
   - Use NSGs for App Service

#### 4. Monitoring

1. **Enable Application Insights**
2. **Set up alerts for:**
   - High costs
   - Failed requests
   - Security events
3. **Review logs regularly**

---

## Next Steps

### You've Successfully Configured Azure! 🎉

**What you've accomplished:**
- ✅ Created 7 Azure resources
- ✅ Configured all services in Azure Portal
- ✅ Updated application configuration
- ✅ Created vector search index
- ✅ Ready to process claims

**Next actions:**

1. **Test End-to-End Flow**
   - Upload a test claim document
   - Verify OCR extraction
   - Check policy clause retrieval
   - Review claim decision

2. **Ingest Production Policies**
   - Add real policy documents to `TestDocuments/`
   - Run PolicyIngestion tool
   - Verify embeddings in AI Search

3. **Deploy Frontend**
   - Update Angular app with API endpoint
   - Deploy to Azure Static Web Apps or App Service

4. **Monitor Costs**
   - Set up cost alerts in Azure Portal
   - Review daily spending
   - Optimize based on usage

5. **Security Hardening**
   - Migrate secrets to Key Vault
   - Enable managed identity
   - Restrict network access

---

## Additional Resources

### Official Documentation

- **Azure OpenAI:** https://learn.microsoft.com/en-us/azure/ai-services/openai/
- **AI Search:** https://learn.microsoft.com/en-us/azure/search/
- **Cosmos DB:** https://learn.microsoft.com/en-us/azure/cosmos-db/
- **Blob Storage:** https://learn.microsoft.com/en-us/azure/storage/blobs/
- **Document Intelligence:** https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/
- **Language Service:** https://learn.microsoft.com/en-us/azure/ai-services/language-service/
- **Computer Vision:** https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/

### Tutorials

- Vector Search in AI Search: https://learn.microsoft.com/en-us/azure/search/vector-search-overview
- RAG with Azure OpenAI: https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/use-your-data
- Cosmos DB Getting Started: https://learn.microsoft.com/en-us/azure/cosmos-db/sql/sql-api-get-started

### Community

- Azure Tech Community: https://techcommunity.microsoft.com/t5/azure/ct-p/Azure
- Stack Overflow: Tag `azure` + specific service
- GitHub Issues: Check each Azure SDK repository

---

**Need Help?**
- Check troubleshooting section above
- Review Azure service health: https://status.azure.com
- Contact Azure support for subscription-specific issues

**Document Created:** February 10, 2026  
**Author:** Development Team  
**Version:** 1.0
