# Implementation Guide - DynamoDB, OpenSearch & Policy Ingestion

**Date:** January 29, 2026  
**AWS Account:** 123456789012 (your-username)  
**Region:** us-east-1

---

## Prerequisites

✅ AWS credentials configured in appsettings.json  
✅ AWS CLI installed and authenticated  
✅ Code bugs (#1, #2) will be fixed separately

---

## Part 1: Create DynamoDB Audit Table

### Purpose
Store complete audit trail of all claim validation decisions for compliance and regulatory requirements.

### What Gets Stored
- Claim ID and timestamp
- Policy number, amount, description
- Decision status (Covered/Not Covered/Manual Review)
- AI explanation and confidence score
- Clause references used
- Required documents list
- Retrieved policy clauses metadata

### Step-by-Step Instructions

#### Step 1.1: Create the Table

Open PowerShell and run:

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

**Expected Output:**
```json
{
    "TableDescription": {
        "TableName": "ClaimsAuditTrail",
        "TableStatus": "CREATING",
        "CreationDateTime": "2026-01-29T...",
        "AttributeDefinitions": [
            {
                "AttributeName": "ClaimId",
                "AttributeType": "S"
            },
            {
                "AttributeName": "Timestamp",
                "AttributeType": "S"
            }
        ],
        "KeySchema": [
            {
                "AttributeName": "ClaimId",
                "KeyType": "HASH"
            },
            {
                "AttributeName": "Timestamp",
                "KeyType": "RANGE"
            }
        ],
        "BillingModeSummary": {
            "BillingMode": "PAY_PER_REQUEST"
        }
    }
}
```

**What This Means:**
- **Partition Key (HASH):** ClaimId - unique identifier for each claim
- **Sort Key (RANGE):** Timestamp - allows multiple audit records per claim
- **Billing Mode:** PAY_PER_REQUEST - no upfront cost, pay only for what you use
- **Status:** CREATING - table is being provisioned (takes ~10-20 seconds)

---

#### Step 1.2: Wait for Table to Become Active

Check table status:

```powershell
aws dynamodb describe-table `
  --table-name ClaimsAuditTrail `
  --region us-east-1 `
  --query 'Table.TableStatus' `
  --output text
```

**Keep running until output shows:**
```
ACTIVE
```

**Alternative - Watch for active status:**
```powershell
do {
    $status = aws dynamodb describe-table --table-name ClaimsAuditTrail --region us-east-1 --query 'Table.TableStatus' --output text
    Write-Host "Table status: $status"
    if ($status -ne "ACTIVE") { Start-Sleep -Seconds 5 }
} while ($status -ne "ACTIVE")
Write-Host "✓ Table is now ACTIVE"
```

---

#### Step 1.3: Verify Table Structure

Get complete table details:

```powershell
aws dynamodb describe-table `
  --table-name ClaimsAuditTrail `
  --region us-east-1
```

**Verify these properties:**
- ✅ TableStatus: "ACTIVE"
- ✅ TableName: "ClaimsAuditTrail"
- ✅ KeySchema has ClaimId (HASH) and Timestamp (RANGE)
- ✅ BillingMode: "PAY_PER_REQUEST"

---

#### Step 1.4: Test Write Access (Optional)

Insert a test record to confirm permissions:

```powershell
aws dynamodb put-item `
  --table-name ClaimsAuditTrail `
  --item '{
    "ClaimId": {"S": "TEST-001"},
    "Timestamp": {"S": "2026-01-29T12:00:00Z"},
    "PolicyNumber": {"S": "POL-TEST"},
    "ClaimAmount": {"N": "1000"},
    "DecisionStatus": {"S": "TEST"}
  }' `
  --region us-east-1
```

**Expected:** No output = success

**Verify the record was written:**
```powershell
aws dynamodb get-item `
  --table-name ClaimsAuditTrail `
  --key '{
    "ClaimId": {"S": "TEST-001"},
    "Timestamp": {"S": "2026-01-29T12:00:00Z"}
  }' `
  --region us-east-1
```

**Expected Output:**
```json
{
    "Item": {
        "ClaimId": {"S": "TEST-001"},
        "Timestamp": {"S": "2026-01-29T12:00:00Z"},
        "PolicyNumber": {"S": "POL-TEST"},
        "ClaimAmount": {"N": "1000"},
        "DecisionStatus": {"S": "TEST"}
    }
}
```

**Clean up test record:**
```powershell
aws dynamodb delete-item `
  --table-name ClaimsAuditTrail `
  --key '{
    "ClaimId": {"S": "TEST-001"},
    "Timestamp": {"S": "2026-01-29T12:00:00Z"}
  }' `
  --region us-east-1
```

---

#### Step 1.5: Enable Point-in-Time Recovery (Recommended)

Protect against accidental data loss:

```powershell
aws dynamodb update-continuous-backups `
  --table-name ClaimsAuditTrail `
  --point-in-time-recovery-specification PointInTimeRecoveryEnabled=true `
  --region us-east-1
```

**Expected Output:**
```json
{
    "ContinuousBackupsDescription": {
        "ContinuousBackupsStatus": "ENABLED",
        "PointInTimeRecoveryDescription": {
            "PointInTimeRecoveryStatus": "ENABLED"
        }
    }
}
```

**What this does:**
- Enables automatic backups with 35-day retention
- Allows table restore to any point in time
- No additional cost for first 1 GB

---

### What Happens Now

When you run claim validation through your API:

1. **Request received** → `/api/claims/validate`
2. **RAG pipeline executes** → Embedding, retrieval, LLM decision
3. **Decision returned** → Status, explanation, confidence
4. **Audit record saved** → DynamoDB write (automatic via `AuditService.cs`)

**View audit records:**
```powershell
# Get all records
aws dynamodb scan --table-name ClaimsAuditTrail --region us-east-1

# Get records for specific claim
aws dynamodb query `
  --table-name ClaimsAuditTrail `
  --key-condition-expression "ClaimId = :claimId" `
  --expression-attribute-values '{":claimId": {"S": "your-claim-id-here"}}' `
  --region us-east-1
```

---

### Troubleshooting

**Error: "ResourceInUseException: Table already exists"**
- Solution: Table already created, skip to Step 1.3 to verify

**Error: "AccessDeniedException"**
- Solution: IAM user needs `dynamodb:CreateTable` permission
- Add policy: `AmazonDynamoDBFullAccess` or create custom policy

**Table stuck in CREATING status (> 2 minutes)**
- Check AWS Console → DynamoDB → Tables
- Look for error messages
- May need to delete and recreate if failed

**Cost Concerns**
- First 25 GB storage: FREE (always)
- First 25 read/write capacity units: FREE per month
- Typical usage for this POC: < $1/month

---

## Part 2: Fix OpenSearch Configuration

### Current Situation

**Your appsettings.json says:**
```json
"OpenSearchEndpoint": "https://your-collection-id.us-east-1.aoss.amazonaws.com"
```

**Your actual OpenSearch collection:**
- Name: `bedrock-knowledge-base-ltm9gv`
- Status: ACTIVE
- Endpoint: **Unknown (need to get it)**

### Two Options

You can either:
- **Option A:** Get the correct endpoint for your existing collection
- **Option B:** Create a new collection matching your config

---

### Option A: Use Existing Collection (Recommended)

#### Step 2A.1: Get Collection Endpoint

```powershell
aws opensearchserverless batch-get-collection `
  --names bedrock-knowledge-base-ltm9gv `
  --region us-east-1 `
  --query 'collectionDetails[0].collectionEndpoint' `
  --output text
```

**Expected Output (example):**
```
https://abc123xyz456.us-east-1.aoss.amazonaws.com
```

**Copy this endpoint** - you'll need it in next steps.

---

#### Step 2A.2: Get Collection ARN

You'll also need this for access policies:

```powershell
aws opensearchserverless batch-get-collection `
  --names bedrock-knowledge-base-ltm9gv `
  --region us-east-1 `
  --query 'collectionDetails[0].arn' `
  --output text
```

**Expected Output (example):**
```
arn:aws:aoss:us-east-1:123456789012:collection/xyz123abc456
```

---

#### Step 2A.3: Verify Collection is VECTORSEARCH Type

```powershell
aws opensearchserverless batch-get-collection `
  --names bedrock-knowledge-base-ltm9gv `
  --region us-east-1 `
  --query 'collectionDetails[0].type' `
  --output text
```

**Expected Output:**
```
VECTORSEARCH
```

**If output is NOT "VECTORSEARCH":**
- This collection cannot be used for RAG (wrong type)
- Use Option B to create a new collection

---

#### Step 2A.4: Check Data Access Policy

Your IAM user needs permission to access the collection:

```powershell
aws opensearchserverless list-access-policies `
  --type data `
  --region us-east-1 `
  --query 'accessPolicySummaries[?contains(name, `bedrock-knowledge-base`)].name' `
  --output text
```

**If no policy exists or doesn't include your user:**

Create a data access policy:

```powershell
# First, get your IAM user ARN
aws sts get-caller-identity --query 'Arn' --output text
# Output example: arn:aws:iam::123456789012:user/your-username

# Create access policy (replace YOUR_USER_ARN with output from above)
aws opensearchserverless create-access-policy `
  --name claims-rag-data-policy `
  --type data `
  --policy '[{
    "Rules": [{
      "ResourceType": "collection",
      "Resource": ["collection/bedrock-knowledge-base-ltm9gv"],
      "Permission": ["aoss:*"]
    }, {
      "ResourceType": "index",
      "Resource": ["index/bedrock-knowledge-base-ltm9gv/*"],
      "Permission": ["aoss:*"]
    }],
    "Principal": ["YOUR_USER_ARN"]
  }]' `
  --region us-east-1
```

---

#### Step 2A.5: Update appsettings.json

**File:** `src/ClaimsRagBot.Api/appsettings.json`

**Change:**
```json
"OpenSearchEndpoint": "https://your-collection-id.us-east-1.aoss.amazonaws.com",
```

**To:**
```json
"OpenSearchEndpoint": "https://YOUR_ACTUAL_ENDPOINT_FROM_STEP_2A1.us-east-1.aoss.amazonaws.com",
```

**Example:**
```json
"OpenSearchEndpoint": "https://abc123xyz456.us-east-1.aoss.amazonaws.com",
```

**Also change index name to match your collection:**
```json
"OpenSearchIndexName": "policy-clauses",
```

Save the file.

---

### Option B: Create New Collection (Alternative)

If you prefer a dedicated collection for this project:

#### Step 2B.1: Create Network Policy

```powershell
aws opensearchserverless create-security-policy `
  --name claims-policies-network `
  --type network `
  --policy '[{
    "Rules": [{
      "ResourceType": "collection",
      "Resource": ["collection/claims-policies-dev"]
    }],
    "AllowFromPublic": true
  }]' `
  --region us-east-1
```

---

#### Step 2B.2: Create Encryption Policy

```powershell
aws opensearchserverless create-security-policy `
  --name claims-policies-encryption `
  --type encryption `
  --policy '{
    "Rules": [{
      "ResourceType": "collection",
      "Resource": ["collection/claims-policies-dev"]
    }],
    "AWSOwnedKey": true
  }' `
  --region us-east-1
```

---

#### Step 2B.3: Create Data Access Policy

```powershell
# Get your IAM user ARN
$userArn = aws sts get-caller-identity --query 'Arn' --output text

# Create policy
aws opensearchserverless create-access-policy `
  --name claims-policies-data `
  --type data `
  --policy "[{
    \"Rules\": [{
      \"ResourceType\": \"collection\",
      \"Resource\": [\"collection/claims-policies-dev\"],
      \"Permission\": [\"aoss:*\"]
    }, {
      \"ResourceType\": \"index\",
      \"Resource\": [\"index/claims-policies-dev/*\"],
      \"Permission\": [\"aoss:*\"]
    }],
    \"Principal\": [\"$userArn\"]
  }]" `
  --region us-east-1
```

---

#### Step 2B.4: Create Collection

```powershell
aws opensearchserverless create-collection `
  --name claims-policies-dev `
  --type VECTORSEARCH `
  --description "Vector database for Claims RAG Bot policy clauses" `
  --region us-east-1
```

**Expected Output:**
```json
{
    "createCollectionDetail": {
        "id": "xyz123abc456",
        "name": "claims-policies-dev",
        "status": "CREATING",
        "type": "VECTORSEARCH",
        "arn": "arn:aws:aoss:us-east-1:123456789012:collection/xyz123abc456"
    }
}
```

---

#### Step 2B.5: Wait for Collection to Become Active

This takes **5-10 minutes**. Monitor status:

```powershell
do {
    $status = aws opensearchserverless batch-get-collection --names claims-policies-dev --region us-east-1 --query 'collectionDetails[0].status' --output text
    Write-Host "Collection status: $status"
    if ($status -ne "ACTIVE") { Start-Sleep -Seconds 30 }
} while ($status -ne "ACTIVE")
Write-Host "✓ Collection is now ACTIVE"
```

---

#### Step 2B.6: Get Collection Endpoint

```powershell
aws opensearchserverless batch-get-collection `
  --names claims-policies-dev `
  --region us-east-1 `
  --query 'collectionDetails[0].collectionEndpoint' `
  --output text
```

**Update appsettings.json with this endpoint.**

---

### Verify OpenSearch Configuration

Regardless of which option you chose, verify access:

```powershell
# Test connection (should return 200 OK)
curl -Method GET "YOUR_OPENSEARCH_ENDPOINT/_cluster/health" | ConvertFrom-Json
```

**Expected Output:**
```json
{
    "cluster_name": "...",
    "status": "green",
    "number_of_nodes": 2
}
```

**If you get 403 Forbidden:**
- Data access policy doesn't include your IAM user
- Re-run Step 2A.4 or 2B.3

---

## Part 3: Run OpenSearch Policy Ingestion

### Purpose

Load sample insurance policy clauses into OpenSearch with vector embeddings for semantic search.

### What Gets Loaded

- **~20 Motor Insurance clauses**
  - Collision coverage, theft, vandalism, liability, exclusions
  - Clause IDs: MOT-001 through MOT-020
  
- **~15 Health Insurance clauses**
  - Hospitalization, surgeries, pre-existing conditions, exclusions
  - Clause IDs: HTH-001 through HTH-015

Each clause includes:
- Text content (the actual policy language)
- Vector embedding (1536 dimensions from Titan)
- Metadata (coverage type, policy type, clause ID)

---

### Prerequisites Check

Before running ingestion:

✅ **DynamoDB table created** (Part 1 complete)  
✅ **OpenSearch endpoint configured** (Part 2 complete)  
✅ **Bedrock Titan Embeddings enabled** (verify below)

**Verify Titan Embeddings access:**
```powershell
aws bedrock list-foundation-models `
  --region us-east-1 `
  --by-provider amazon `
  --query 'modelSummaries[?contains(modelId, `titan-embed`)].modelId' `
  --output text
```

**Expected Output:**
```
amazon.titan-embed-text-v1
amazon.titan-embed-text-v2:0
```

**If empty:**
- Go to AWS Console → Bedrock → Model access
- Enable "Titan Text Embeddings V1" or "Titan Text Embeddings V2"
- Wait 1-2 minutes for activation

---

### Step-by-Step Ingestion

#### Step 3.1: Navigate to Ingestion Tool

```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion
```

---

#### Step 3.2: Verify Tool Builds

```powershell
dotnet build
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**If build fails:**
- Check .NET 8 SDK is installed: `dotnet --version`
- Ensure all NuGet packages restored: `dotnet restore`

---

#### Step 3.3: Get OpenSearch Endpoint

Use the endpoint from Part 2:

**If you used Option A (existing collection):**
```powershell
$endpoint = aws opensearchserverless batch-get-collection --names bedrock-knowledge-base-ltm9gv --region us-east-1 --query 'collectionDetails[0].collectionEndpoint' --output text
Write-Host "Endpoint: $endpoint"
```

**If you used Option B (new collection):**
```powershell
$endpoint = aws opensearchserverless batch-get-collection --names claims-policies-dev --region us-east-1 --query 'collectionDetails[0].collectionEndpoint' --output text
Write-Host "Endpoint: $endpoint"
```

**Copy the endpoint URL** (e.g., `https://abc123xyz456.us-east-1.aoss.amazonaws.com`)

---

#### Step 3.4: Run Ingestion Tool

**Syntax:**
```powershell
dotnet run -- <OPENSEARCH_ENDPOINT> [INDEX_NAME]
```

**Example:**
```powershell
dotnet run -- https://abc123xyz456.us-east-1.aoss.amazonaws.com policy-clauses
```

**Or using the variable from Step 3.3:**
```powershell
dotnet run -- $endpoint policy-clauses
```

---

#### Step 3.5: Monitor Ingestion Progress

**Expected Console Output:**

```
=== Policy Ingestion Tool ===

OpenSearch Endpoint: https://abc123xyz456.us-east-1.aoss.amazonaws.com
Index Name: policy-clauses

Step 1: Creating OpenSearch index...
[OpenSearch] Creating index: policy-clauses
[OpenSearch] Index created successfully
✓ Index creation completed

Step 2: Ingesting Motor Insurance clauses...
[Bedrock] Generating embedding for clause: MOT-001
[Bedrock] Generating embedding for clause: MOT-002
[Bedrock] Generating embedding for clause: MOT-003
...
[OpenSearch] Indexed clause: MOT-001
[OpenSearch] Indexed clause: MOT-002
[OpenSearch] Indexed clause: MOT-003
...
✓ Ingested 20 Motor Insurance clauses

Step 3: Ingesting Health Insurance clauses...
[Bedrock] Generating embedding for clause: HTH-001
[Bedrock] Generating embedding for clause: HTH-002
...
[OpenSearch] Indexed clause: HTH-001
[OpenSearch] Indexed clause: HTH-002
...
✓ Ingested 15 Health Insurance clauses

✓ Policy ingestion completed!

Next steps:
1. Update appsettings.json with OpenSearch endpoint
2. Run the API: cd src/ClaimsRagBot.Api && dotnet run
3. Test claims validation
```

**Time:** ~2-3 minutes (depends on Bedrock API rate limits)

---

#### Step 3.6: Verify Data Loaded

**Check index exists:**
```powershell
# Get list of indices
curl -Method GET "$endpoint/_cat/indices?v" `
  -Headers @{
    "x-amz-security-token" = (aws sts get-session-token --query 'Credentials.SessionToken' --output text)
  }
```

**Check document count:**
```powershell
curl -Method GET "$endpoint/policy-clauses/_count" `
  -Headers @{
    "Content-Type" = "application/json"
  } | ConvertFrom-Json
```

**Expected Output:**
```json
{
  "count": 35,
  "_shards": {
    "total": 1,
    "successful": 1,
    "skipped": 0,
    "failed": 0
  }
}
```

**Verify you have ~35 documents** (20 Motor + 15 Health)

---

#### Step 3.7: Test Vector Search (Optional)

Query for a specific clause:

```powershell
$searchQuery = @{
  query = @{
    match = @{
      clauseId = "MOT-001"
    }
  }
} | ConvertTo-Json -Depth 10

curl -Method POST "$endpoint/policy-clauses/_search" `
  -Headers @{"Content-Type" = "application/json"} `
  -Body $searchQuery | ConvertFrom-Json
```

**Expected:** Should return the MOT-001 clause document with embedding vector.

---

### What Happens Now

When you run claim validation:

1. **User submits claim** → "Car accident - bumper damage"
2. **Generate embedding** → Bedrock Titan converts to 1536-dim vector
3. **Vector search** → OpenSearch finds similar policy clauses using KNN
4. **Top 5 results** → e.g., MOT-001 (collision), MOT-004 (comprehensive), etc.
5. **LLM reasoning** → Claude analyzes claim against retrieved clauses
6. **Decision returned** → "Covered" with clause citations

**No more mock data fallback!** Real semantic search based on meaning, not keywords.

---

### Troubleshooting

#### Error: "Index already exists"

**Message:**
```
resource_already_exists_exception: index [policy-clauses/...] already exists
```

**Solution:**
```powershell
# Delete existing index
curl -Method DELETE "$endpoint/policy-clauses"

# Re-run ingestion
dotnet run -- $endpoint policy-clauses
```

---

#### Error: "Forbidden" or "AccessDenied"

**Message:**
```
security_exception: no permissions for [indices:data/write/index]
```

**Solution:**
- Re-run data access policy creation (Part 2, Step 2A.4 or 2B.3)
- Ensure policy includes both collection AND index permissions
- Verify Principal is your IAM user ARN

---

#### Error: "ValidationException" from Bedrock

**Message:**
```
ValidationException: The provided model identifier is invalid
```

**Solution:**
- Verify Titan Embeddings model is enabled
- Check model ID in `PolicyIngestionService.cs` (should be `amazon.titan-embed-text-v1`)
- Confirm your region supports Bedrock (us-east-1 does)

---

#### Error: "Connection refused" or "Timeout"

**Message:**
```
HttpRequestException: No connection could be made
```

**Solution:**
- Check network policy allows public access (Part 2, Step 2B.1)
- Verify endpoint URL is correct (no typos)
- Ensure collection is ACTIVE status

---

#### Ingestion runs but search returns no results

**Check:**
1. Verify documents indexed: `curl -Method GET "$endpoint/policy-clauses/_count"`
2. Check index mapping has embedding field: `curl -Method GET "$endpoint/policy-clauses/_mapping"`
3. Ensure appsettings.json has correct endpoint

---

### Cost Impact

**Bedrock Titan Embeddings:**
- $0.0001 per 1,000 input tokens
- ~35 clauses × 100 tokens each = 3,500 tokens
- Cost: **$0.0004** (less than 1 cent)

**OpenSearch Serverless:**
- Minimum 2 OCU (OpenSearch Compute Units) × $0.24/hour
- Monthly: ~$350 (always running)
- **Note:** This is the base cost regardless of data size

**Total for ingestion:** < 1 cent (one-time)  
**Ongoing OpenSearch cost:** ~$350/month (can be shared across projects)

---

## Final Verification

After completing all 3 parts:

### Test 1: Check All AWS Resources

```powershell
# DynamoDB
aws dynamodb describe-table --table-name ClaimsAuditTrail --region us-east-1 --query 'Table.TableStatus'
# Expected: "ACTIVE"

# OpenSearch
aws opensearchserverless list-collections --region us-east-1 --query 'collectionSummaries[*].{Name:name,Status:status}'
# Expected: At least one ACTIVE collection

# OpenSearch index
curl -Method GET "$endpoint/policy-clauses/_count" | ConvertFrom-Json
# Expected: {"count": 35, ...}
```

---

### Test 2: Run Full API Test

```powershell
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
dotnet run
```

**In another terminal or browser:**
```
http://localhost:5184/swagger
```

**Submit test claim:**
```json
{
  "policyNumber": "POL-2024-001",
  "claimDescription": "Front bumper damaged in parking lot collision with another vehicle",
  "claimAmount": 2500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "The claim is covered under collision coverage as per policy clause MOT-001...",
  "clauseReferences": ["MOT-001", "MOT-004"],
  "requiredDocuments": [
    "Police accident report",
    "Repair estimate",
    "Photos of damage"
  ],
  "confidenceScore": 0.92
}
```

---

### Test 3: Verify Audit Trail

```powershell
# Wait 5 seconds after claim submission, then:
aws dynamodb scan --table-name ClaimsAuditTrail --region us-east-1 --query 'Items[0]'
```

**Expected:** JSON object with your claim details, decision, timestamp, etc.

---

### Test 4: Verify Real Vector Search (Not Mock)

**Check API console output for:**
```
[OpenSearch] Querying for similar clauses...
[OpenSearch] Found 5 matching clauses
[OpenSearch] Top match: MOT-001 (score: 0.89)
```

**NOT:**
```
OpenSearch query failed, falling back to mock data
```

**If you see fallback message:**
- OpenSearch endpoint is wrong
- Index is empty
- Network/permissions issue

---

## Success Criteria

✅ DynamoDB table exists and is ACTIVE  
✅ OpenSearch collection exists and is ACTIVE  
✅ OpenSearch index contains 35 policy documents  
✅ Claim validation returns real clause IDs (MOT-xxx, HTH-xxx)  
✅ Audit records saved to DynamoDB  
✅ No "falling back to mock data" messages  
✅ Confidence scores are reasonable (0.7-0.95)  

---

## Next Steps After Completion

1. **Test edge cases** - Try claims that should be denied
2. **Test document extraction** - Upload PDF claim forms
3. **Review audit data** - Analyze claim patterns
4. **Tune confidence thresholds** - Adjust in appsettings.json
5. **Add more policies** - Expand beyond samples
6. **Deploy to Lambda** - Use SAM template (optional)

---

**Estimated Total Time:**
- Part 1 (DynamoDB): 2 minutes
- Part 2 (OpenSearch): 5-10 minutes
- Part 3 (Ingestion): 5 minutes
- **Total: 15-20 minutes**

**Generated:** January 29, 2026  
**For:** ClaimsValidation-AWS-RAG-new  
**AWS Account:** 123456789012
