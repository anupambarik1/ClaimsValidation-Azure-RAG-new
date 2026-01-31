# Real Pending Items - Fresh Analysis

**Date:** January 29, 2026  
**Credentials Status:** ✅ Configured in appsettings.json  
**AWS Account:** 123456789012 (your-username)

---

## What You Already Have ✅

Based on fresh AWS verification:

| Resource | Status | Evidence |
|----------|--------|----------|
| **AWS Credentials** | ✅ Working | Successfully authenticated as `your-username` |
| **S3 Bucket** | ✅ Exists | `claims-documents-rag-dev` is accessible |
| **OpenSearch Collection** | ✅ Exists | `bedrock-knowledge-base-ltm9gv` (ACTIVE) |
| **Bedrock Models** | ✅ Enabled | Claude 3 Sonnet and other Anthropic models accessible |
| **Code Architecture** | ✅ Complete | All services implemented |

---

## What's ACTUALLY Pending (Critical - Blocks Functionality)

### 1. ⚠️ **Fix Model ID Validation Error** (2 minutes)

**File:** `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs`  
**Line:** 75

**Current (WRONG):**
```csharp
ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
```

**Problem:** 
- This model ID format doesn't exist in your AWS account
- Validation error from AWS SDK pattern matching
- Using a cross-region inference profile that's not available

**Available Models in Your Account:**
- `anthropic.claude-3-sonnet-20240229-v1:0` ✅ (Best match)
- `anthropic.claude-sonnet-4-20250514-v1:0` (Newer, if you want latest)
- `anthropic.claude-haiku-4-5-20251001-v1:0` (Faster, cheaper)

**Fix:**
```csharp
ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
```

**Impact if not fixed:** 
- ❌ All claim validations will fail
- ❌ LLM service won't work at runtime

---

### 2. ⚠️ **Remove Hardcoded Credential Overrides** (2 minutes)

**Files & Lines:**
- `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs` - Lines 26-27
- `src/ClaimsRagBot.Infrastructure/Bedrock/EmbeddingService.cs` - Lines 24-25
- `src/ClaimsRagBot.Infrastructure/S3/DocumentUploadService.cs` - Lines 24-25

**Current (WRONG):**
```csharp
var accessKeyId = configuration["AWS:AccessKeyId"];
var secretAccessKey = configuration["AWS:SecretAccessKey"];

accessKeyId = "";        // ← DELETE THIS LINE
secretAccessKey = "";    // ← DELETE THIS LINE
```

**Problem:**
- You've entered credentials in appsettings.json
- But code **overrides them to empty strings**
- SDK falls back to default credential chain, ignoring your config

**Fix:** Delete those 2 lines in each file (6 lines total)

**Impact if not fixed:**
- ⚠️ Your appsettings.json credentials are ignored
- ⚠️ Relies on AWS CLI default profile (may work, but inconsistent)

---

### 3. ⚠️ **Create DynamoDB Audit Table** (1 minute)

**Status:** ❌ Does not exist

**Evidence:**
```
ResourceNotFoundException: Table: ClaimsAuditTrail not found
```

**Your code expects:** `ClaimsAuditTrail` table for audit logging

**Impact if not created:**
- ⚠️ Audit writes fail silently (caught exception)
- ⚠️ No compliance trail
- ⚠️ Can't track claim decisions

**Command to create:**
```powershell
aws dynamodb create-table `
  --table-name ClaimsAuditTrail `
  --attribute-definitions `
    AttributeName=ClaimId,AttributeType=S `
    AttributeName=Timestamp,AttributeType=S `
  --key-schema `
    AttributeName=ClaimId,KeyType=HASH `
    AttributeName=Timestamp,KeyType=RANGE `
  --billing-mode PAY_PER_REQUEST `
  --region us-east-1
```

**Verification:**
```powershell
aws dynamodb describe-table --table-name ClaimsAuditTrail --region us-east-1
```

---

### 4. ⚠️ **OpenSearch Configuration Mismatch** (5 minutes)

**Your Config Says:**
```json
"OpenSearchEndpoint": "https://your-collection-id.us-east-1.aoss.amazonaws.com"
```

**Your AWS Has:**
- Collection: `bedrock-knowledge-base-ltm9gv` (ACTIVE)
- Endpoint: Likely different from what's in config

**Problem:** 
- Endpoint mismatch will cause connection failures
- Code will fall back to mock data
- Not using real vector search

**Fix Options:**

**Option A - Get correct endpoint for existing collection:**
```powershell
aws opensearchserverless batch-get-collection `
  --names bedrock-knowledge-base-ltm9gv `
  --region us-east-1 `
  --query 'collectionDetails[0].collectionEndpoint'
```

Then update `appsettings.json` with the correct endpoint.

**Option B - Create new collection matching your config:**
```powershell
aws opensearchserverless create-collection `
  --name claims-policies-dev `
  --type VECTORSEARCH `
  --region us-east-1
```

Wait for it to become ACTIVE, then run policy ingestion.

**Current Impact:**
- ⚠️ Using mock policy data (hardcoded Motor/Health clauses)
- ⚠️ RAG pipeline works but with fake data
- ⚠️ Not doing real semantic search

---

### 5. ⚠️ **Run OpenSearch Policy Ingestion** (5 minutes)

**After fixing OpenSearch endpoint**, populate it with policy embeddings:

```powershell
cd tools/PolicyIngestion
dotnet run -- <YOUR_CORRECT_OPENSEARCH_ENDPOINT>
```

**What this does:**
1. Creates `policy-clauses` index (1536-dim vectors for Titan)
2. Generates embeddings for ~35 sample policy clauses
3. Indexes them for vector similarity search

**Impact if not done:**
- ⚠️ Empty index = no results
- ⚠️ Falls back to mock data
- ⚠️ RAG doesn't retrieve real policies

---

## Summary - Action Plan (15 minutes total)

### Step 1: Fix Code Bugs (5 minutes)

**LlmService.cs - Line 75:**
```csharp
// Change from:
ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",

// To:
ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
```

**Delete credential overrides in 3 files:**
- LlmService.cs (lines 26-27)
- EmbeddingService.cs (lines 24-25)  
- DocumentUploadService.cs (lines 24-25)

---

### Step 2: Create DynamoDB Table (1 minute)

```powershell
aws dynamodb create-table `
  --table-name ClaimsAuditTrail `
  --attribute-definitions AttributeName=ClaimId,AttributeType=S AttributeName=Timestamp,AttributeType=S `
  --key-schema AttributeName=ClaimId,KeyType=HASH AttributeName=Timestamp,KeyType=RANGE `
  --billing-mode PAY_PER_REQUEST `
  --region us-east-1
```

---

### Step 3: Fix OpenSearch Configuration (5 minutes)

**Get correct endpoint:**
```powershell
aws opensearchserverless batch-get-collection `
  --names bedrock-knowledge-base-ltm9gv `
  --region us-east-1 `
  --query 'collectionDetails[0].collectionEndpoint'
```

**Update appsettings.json** with the returned endpoint.

---

### Step 4: Ingest Policies (5 minutes)

```powershell
cd tools/PolicyIngestion
dotnet run -- <YOUR_OPENSEARCH_ENDPOINT_FROM_STEP3>
```

---

### Step 5: Test (5 minutes)

```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

Open: `http://localhost:5184/swagger`

Test claim validation:
```json
{
  "policyNumber": "POL-12345",
  "claimDescription": "Car accident - front bumper damage",
  "claimAmount": 2500,
  "policyType": "Motor"
}
```

**Success Criteria:**
- ✅ No errors in console
- ✅ Response includes real clause IDs (MOT-001, etc.)
- ✅ DynamoDB has audit record
- ✅ OpenSearch returned real policy data (not mock)

---

## What's NOT Needed (Already Done)

- ❌ AWS credentials setup - **DONE**
- ❌ Bedrock model access - **DONE**
- ❌ S3 bucket creation - **DONE**
- ❌ OpenSearch collection creation - **DONE**
- ❌ Code architecture - **DONE**

---

## After These 5 Steps

You'll have a **fully functional Claims RAG Bot** with:
- ✅ Real AWS Bedrock AI (Claude 3 Sonnet)
- ✅ Real vector search (OpenSearch with Titan embeddings)
- ✅ Document processing (Textract, Comprehend, Rekognition)
- ✅ Audit trail (DynamoDB)
- ✅ Production-ready API

**Total time to working system:** ~15-20 minutes

---

**Generated:** January 29, 2026  
**AWS Account:** 123456789012  
**Region:** us-east-1
