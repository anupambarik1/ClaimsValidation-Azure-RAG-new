# PolicyIngestion Tool - Azure Setup Summary

## What I Created

Since there was no `appsettings.json` in the PolicyIngestion folder, I've created the necessary files for Azure policy ingestion:

### Files Created:

1. **`tools\PolicyIngestion\appsettings.json`**
   - Configuration file for the PolicyIngestion tool
   - Contains Azure OpenAI and AI Search credentials
   - **You need to update this with your actual Azure credentials**

2. **`tools\PolicyIngestion\AzurePolicyIngestion.cs`**
   - New class that handles Azure AI Search ingestion
   - Creates the index with proper vector search configuration
   - Generates embeddings using Azure OpenAI
   - Processes policy documents from TestDocuments folder

3. **Updated `tools\PolicyIngestion\Program.cs`**
   - Now supports both Azure and AWS
   - Automatically detects CloudProvider from appsettings.json
   - Uses local appsettings.json if available

---

## How to Use

### Step 1: Update appsettings.json with Your Credentials

**File Location:** `tools\PolicyIngestion\appsettings.json`

Replace the placeholder values with your actual Azure credentials:

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

**Where to Get These Values:**

| Value | Location in Azure Portal |
|-------|-------------------------|
| OpenAI Endpoint | Azure OpenAI → Keys and Endpoint → "Endpoint" |
| OpenAI ApiKey | Azure OpenAI → Keys and Endpoint → "KEY 1" |
| AISearch Endpoint | AI Search → Overview → "Url" |
| AISearch AdminApiKey | AI Search → Keys → "Primary admin key" ⚠️ **Use Admin Key, not Query Key** |

---

### Step 2: Run the PolicyIngestion Tool

```powershell
cd C:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion
dotnet run
```

**What It Does:**
1. Loads configuration from local `appsettings.json`
2. Detects CloudProvider = "Azure"
3. Creates AI Search index with vector search configuration
4. Processes all `.txt` files from `TestDocuments/` folder
5. Generates embeddings for each policy clause using Azure OpenAI
6. Uploads clauses to AI Search in batches

---

### Step 3: Expected Output

```
========================================
Policy Ingestion Tool
========================================

📄 Loading configuration from: C:\...\tools\PolicyIngestion\appsettings.json
🌩️  Cloud Provider: Azure

========================================
Azure AI Search Policy Ingestion
========================================

AI Search Endpoint: https://search-claims-bot.search.windows.net/
Index Name: policy-clauses

Step 1: Creating AI Search index...
Creating AI Search index: policy-clauses
✓ Index 'policy-clauses' created successfully

Step 2: Processing policy documents from TestDocuments/...

Processing policy documents from: C:\...\TestDocuments
- Processing: health_insurance_policy.txt
- Processing: life_insurance_policy.txt  
- Processing: motor_insurance_policy.txt

Total clauses extracted: 487

Ingesting 487 policy clauses...
Progress: 10/487 (2%)
Progress: 20/487 (4%)
...
Progress: 487/487 (100%)
✓ Uploaded batch of 87 documents
✓ Successfully ingested 487 clauses

========================================
✅ Policy ingestion completed successfully!
========================================
```

**Time:** ~8-12 minutes (depends on Azure OpenAI API speed)

---

## Troubleshooting

### Error: "Azure:AISearch:Endpoint not configured"

**Solution:** Update `tools\PolicyIngestion\appsettings.json` with your AI Search endpoint

---

### Error: "Unauthorized" or "403 Forbidden"

**Cause:** Using Query API Key instead of Admin API Key

**Solution:** 
1. Go to Azure Portal → AI Search → Keys
2. Copy **"Primary admin key"** (not Query key)
3. Update `AdminApiKey` in appsettings.json

---

### Error: "Azure OpenAI deployment not found"

**Cause:** Embedding deployment name doesn't match

**Solution:**
1. Go to Azure Portal → Azure OpenAI → Deployments
2. Check the exact name of your text-embedding deployment
3. Update `EmbeddingDeployment` in appsettings.json to match

---

### Error: "TestDocuments folder not found"

**Cause:** Tool can't find the policy documents

**Solution:**
- Make sure you're running from the PolicyIngestion folder
- TestDocuments should be at: `../../TestDocuments` relative to PolicyIngestion folder
- Full path: `C:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\TestDocuments`

---

## Next Steps After Successful Ingestion

1. **Verify Index in Azure Portal:**
   - Go to Azure Portal → AI Search → Indexes
   - Should see `policy-clauses` with ~487 documents

2. **Update Main API Configuration:**
   ```json
   // In src/ClaimsRagBot.Api/appsettings.json
   {
     "CloudProvider": "Azure",
     ...
   }
   ```

3. **Test the API:**
   ```powershell
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
   dotnet run
   ```

4. **Test Claim Validation:**
   ```powershell
   $body = @{
       policyNumber = "AFL-12345-HEALTH"
       policyType = "Health"
       claimAmount = 3000
       claimDescription = "Emergency surgery for appendicitis"
   } | ConvertTo-Json

   Invoke-RestMethod -Uri "http://localhost:5000/api/claims/validate" `
       -Method POST -ContentType "application/json" -Body $body
   ```

   **Expected:** Should return `"status": "Covered"` with explanation (not "Manual Review")

---

## Summary

✅ Created `appsettings.json` in PolicyIngestion folder  
✅ Created `AzurePolicyIngestion.cs` for Azure support  
✅ Updated `Program.cs` to support both Azure and AWS  

**Next Action:** Update the `appsettings.json` file with your actual Azure credentials and run `dotnet run`
