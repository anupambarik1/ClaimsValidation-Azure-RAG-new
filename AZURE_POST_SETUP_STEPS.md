# Azure Post-Setup Steps
## Next Steps After Creating Azure Services and Configuring Credentials

**Document Version:** 1.0  
**Last Updated:** February 13, 2026  
**Prerequisite:** All Azure services created and appsettings.json configured with credentials

---

## Overview

After creating all Azure services and configuring credentials in `appsettings.json`, you need to complete these critical steps to make your Claims RAG Bot functional:

1. Switch CloudProvider to Azure
2. **Create AI Search Index & Ingest Policy Documents** (CRITICAL)
3. Verify the setup
4. Test the complete system

---

## Step 1: Switch CloudProvider to Azure

### Update Main API Configuration

**File:** `src/ClaimsRagBot.Api/appsettings.json`

Change the CloudProvider setting from AWS to Azure:

```json
{
  "CloudProvider": "Azure",
  ...
}
```

**Current Status:** Your file currently shows `"CloudProvider": "AWS"`  
**Required Change:** Update to `"CloudProvider": "Azure"`

This tells the application to use Azure services instead of AWS services.

---

## Step 2: Create AI Search Index & Ingest Policy Documents

### ⚠️ CRITICAL STEP - Required for RAG Functionality

**Why This Step is Essential:**

Without this step, your Claims RAG Bot cannot validate claims because:
- The AI Search index will be empty (no policy clauses)
- RAG (Retrieval-Augmented Generation) requires a knowledge base to query
- All claims will return "Manual Review" status instead of "Approved" or "Denied"

**What the RAG Pipeline Does:**
1. User submits claim description
2. System converts description to embedding vector (Azure OpenAI)
3. System searches for similar policy clauses (AI Search vector search)
4. Retrieved clauses are sent to GPT-4 for decision-making
5. AI returns approval/denial with explanation

**Without indexed policies:** No clauses retrieved → No informed decision → Returns "Manual Review"

---

### Run the PolicyIngestion Tool

The PolicyIngestion tool automatically:
- ✅ Creates the correct index schema with vector search configuration
- ✅ Uploads policy documents from TestDocuments/ folder
- ✅ Generates embeddings via Azure OpenAI (text-embedding-ada-002)
- ✅ Indexes all policy clauses (~487 clauses)

#### Step 2.1: Navigate to PolicyIngestion Tool

```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion
```

#### Step 2.2: Update appsettings.json

**Important:** The `appsettings.json` file has been created in the PolicyIngestion folder at:  
`tools\PolicyIngestion\appsettings.json`

**Update this file with your actual Azure credentials:**

```json
{
  "CloudProvider": "Azure",
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://YOUR_OPENAI_RESOURCE.openai.azure.com/",
      "ApiKey": "YOUR_ACTUAL_OPENAI_KEY",
      "EmbeddingDeployment": "text-embedding-ada-002"
    },
    "AISearch": {
      "Endpoint": "https://YOUR_SEARCH_NAME.search.windows.net/",
      "AdminApiKey": "YOUR_ADMIN_API_KEY",
      "IndexName": "policy-clauses"
    }
  }
}
```

**Where to find these values:**
- **OpenAI Endpoint & Key:** Azure Portal → Azure OpenAI resource → Keys and Endpoint
- **AI Search Endpoint & Admin Key:** Azure Portal → AI Search resource → Keys → **Use Admin Key (not Query Key)**
- **Index Name:** Use `policy-clauses` (will be created automatically)

**⚠️ Critical:** Make sure to use the **Admin API Key** from AI Search, not the Query Key. The tool needs admin permissions to create the index.

#### Step 2.3: Run Policy Ingestion

```powershell
dotnet run
```

#### Step 2.4: Expected Output

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

**Expected Duration:** 8-10 minutes (depends on embedding generation speed)

**✅ Checkpoint:** Policy ingestion completed successfully

---

## Step 3: Verify the Setup

### Verification 1: Check Index in Azure Portal

1. **Navigate to AI Search Resource**
   - Go to Azure Portal
   - Navigate to your AI Search resource (e.g., `search-claims-bot`)
   
2. **View Indexes**
   - Click **"Indexes"** in the left menu
   - ✅ You should see `policy-clauses` index
   - Document count should show ~487 documents

3. **Test Search Explorer**
   - Click on the `policy-clauses` index
   - Click **"Search explorer"** (top toolbar)
   - Run empty search query:
     ```json
     {
       "search": "*",
       "top": 10
     }
     ```
   - Click **"Search"**
   - ✅ Should return 10 policy clauses with embeddings

---

### Verification 2: PowerShell Query Test

Test that the index is queryable via REST API:

```powershell
# Replace with your actual values
$searchEndpoint = "https://YOUR_SEARCH_NAME.search.windows.net"
$queryKey = "YOUR_QUERY_API_KEY"
$indexName = "policy-clauses"

$uri = "$searchEndpoint/indexes/$indexName/docs?api-version=2023-11-01&search=*&`$top=5"
$headers = @{
    "api-key" = $queryKey
}

$response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers
Write-Host "Documents found: $($response.value.Count)"
$response.value | Select-Object ClauseId, PolicyType, Section | Format-Table
```

**Expected Output:**
```
Documents found: 5

ClauseId              PolicyType Section
--------              ---------- -------
CLAUSE-HEALTH-001     Health     3.2
CLAUSE-HEALTH-002     Health     5.1
CLAUSE-MOTOR-001      Motor      2.3
...
```

**✅ Checkpoint:** Index is queryable and contains policy clauses

---

### Verification 3: Test Vector Search

Verify that semantic/vector search works correctly:

```powershell
$searchEndpoint = "https://YOUR_SEARCH_NAME.search.windows.net"
$queryKey = "YOUR_QUERY_API_KEY"

$uri = "$searchEndpoint/indexes/policy-clauses/docs/search?api-version=2023-11-01"
$headers = @{
    "api-key" = $queryKey
    "Content-Type" = "application/json"
}

$body = @{
    search = "emergency surgery hospitalization"
    top = 5
    select = "ClauseId,Text,PolicyType,Section"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $body
$response.value | Format-Table ClauseId, PolicyType, Section
```

**Expected:** Should return relevant clauses about hospitalization and surgery coverage

**✅ Checkpoint:** Vector search working correctly

---

## Step 4: Test the Complete System

### Test 1: Start the API

```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**✅ API is running on http://localhost:5000**

---

### Test 2: Test Claim Validation (Covered Claim)

In a new PowerShell terminal:

```powershell
$body = @{
    policyNumber = "AFL-12345-HEALTH"
    policyType = "Health"
    claimAmount = 3000
    claimDescription = "Emergency surgery for appendicitis with 3 days hospitalization"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/claims/validate" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 5
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

**✅ Checkpoint:** RAG pipeline working - AI retrieved relevant policy clauses and made informed decision

---

### Test 3: Test Claim Validation (Denied Claim)

Test with a claim that should be denied:

```powershell
$body = @{
    policyNumber = "AFL-12345-HEALTH"
    policyType = "Health"
    claimAmount = 2500
    claimDescription = "Cosmetic rhinoplasty surgery for aesthetic purposes"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/claims/validate" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 5
```

**Expected Response:**
```json
{
  "status": "Denied",
  "explanation": "Cosmetic procedures performed solely for aesthetic purposes are explicitly excluded from coverage as per Section 8.2.1 (Exclusions - Cosmetic Procedures)...",
  "clauseReferences": [
    "CLAUSE-HEALTH-8.2.1"
  ],
  "requiredDocuments": [],
  "confidenceScore": 0.95
}
```

**✅ Checkpoint:** AI correctly denies non-covered claims

---

### Test 4: Test Document Upload

Test document upload and extraction:

```powershell
# Create a test file
"Sample claim document for testing" | Out-File -FilePath test-claim.txt

# Upload the file
$uri = "http://localhost:5000/api/documents/upload"
$form = @{
    file = Get-Item -Path "test-claim.txt"
}

# Note: This requires multipart/form-data which is more complex in PowerShell
# Easier to test via Postman or curl
```

**Alternative - Using curl (if available):**
```powershell
curl -X POST http://localhost:5000/api/documents/upload `
    -F "file=@test-claim.txt"
```

**Expected Response:**
```json
{
  "documentId": "doc-123456",
  "fileName": "test-claim.txt",
  "uploadedAt": "2026-02-13T10:30:00Z",
  "status": "Uploaded"
}
```

---

### Test 5: Test Document Extraction

```powershell
$body = @{
    documentId = "doc-123456"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/documents/extract" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 5
```

**Expected Response:**
```json
{
  "documentId": "doc-123456",
  "extractedText": "Sample claim document for testing",
  "entities": [
    {
      "type": "Date",
      "text": "2026-02-13",
      "confidence": 0.95
    }
  ],
  "confidence": 0.88
}
```

**✅ Checkpoint:** Document processing pipeline working (Azure Document Intelligence + Language Service)

---

## Troubleshooting

### Issue: Claims Return "Manual Review" Status

**Symptoms:**
- All claims return `"status": "Manual Review"` instead of "Covered" or "Denied"
- No clause references in response

**Root Cause:** Policy index is empty or not accessible

**Solutions:**

1. **Verify Policy Ingestion Completed:**
   ```powershell
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion
   dotnet run
   ```

2. **Check Index Exists:**
   - Azure Portal → AI Search → Indexes → Should see `policy-clauses`

3. **Check Document Count:**
   - Click on index → Should show ~487 documents

4. **Verify API Key:**
   - Check that Admin API key is correct in PolicyIngestion appsettings.json

---

### Issue: PolicyIngestion Tool Fails

**Error:** "Unauthorized" or "403 Forbidden"

**Solution:**
- Use **Admin API Key**, not Query API Key, in PolicyIngestion appsettings.json
- Find Admin Key: Azure Portal → AI Search → Keys → Primary admin key

**Error:** "Azure OpenAI deployment not found"

**Solution:**
- Verify deployment name is exactly `text-embedding-ada-002`
- Check in Azure Portal → Azure OpenAI → Deployments
- If different name, update `EmbeddingDeployment` in appsettings.json

---

### Issue: API Doesn't Start

**Error:** "Unable to connect to Azure services"

**Solution:**
1. Check `appsettings.json` has `"CloudProvider": "Azure"`
2. Verify all Azure endpoints and keys are correct
3. Test connectivity to Azure services:
   ```powershell
   # Test OpenAI endpoint
   $uri = "https://YOUR_OPENAI.openai.azure.com/openai/deployments?api-version=2023-05-15"
   $headers = @{ "api-key" = "YOUR_KEY" }
   Invoke-RestMethod -Uri $uri -Headers $headers
   ```

---

### Issue: Document Upload Fails

**Error:** "Unable to upload to Azure Blob Storage"

**Solution:**
1. Verify Blob Storage connection string in appsettings.json
2. Check container exists: `claims-documents`
3. Create container if missing:
   ```powershell
   # Via Azure Portal: Storage Account → Containers → + Container
   # Name: claims-documents
   # Public access: Private
   ```

---

## Summary Checklist

After completing all steps, verify:

- ✅ **Step 1:** `appsettings.json` has `"CloudProvider": "Azure"`
- ✅ **Step 2:** PolicyIngestion tool ran successfully (487 clauses indexed)
- ✅ **Step 3.1:** AI Search index visible in Azure Portal with documents
- ✅ **Step 3.2:** PowerShell query returns policy clauses
- ✅ **Step 3.3:** Vector search returns relevant results
- ✅ **Step 4.2:** Claim validation returns "Covered" with explanation (not "Manual Review")
- ✅ **Step 4.3:** Denied claims correctly rejected with reasons
- ✅ **Step 4.4:** Document upload works
- ✅ **Step 4.5:** Document extraction returns text and entities

---

## What's Next?

Once all tests pass, you can:

1. **Deploy to Azure App Service** (for production)
2. **Set up the Angular chatbot UI** (in `claims-chatbot-ui/` folder)
3. **Configure CI/CD pipeline** (Azure DevOps or GitHub Actions)
4. **Add authentication** (Azure AD B2C)
5. **Set up monitoring** (Application Insights)

---

## Key Points to Remember

🔴 **Without Policy Ingestion (Step 2):**
- RAG system has no knowledge base
- All claims return "Manual Review"
- System cannot make intelligent decisions

🟢 **With Policy Ingestion Completed:**
- AI retrieves relevant policy clauses
- Makes informed approval/denial decisions
- Provides explanations with clause references
- System is fully functional

---

## Support & Resources

- **Azure Portal Setup Guide:** `AZURE_PORTAL_SETUP_GUIDE.md`
- **Azure Services Guide:** `AZURE_SERVICES_GUIDE.md`
- **Architecture Documentation:** `COMPLETE_SYSTEM_ARCHITECTURE.md`
- **API Documentation:** See `src/ClaimsRagBot.Api/ClaimsRagBot.Api.http` for sample requests

---

**Document Status:** Complete  
**Last Verified:** February 13, 2026
