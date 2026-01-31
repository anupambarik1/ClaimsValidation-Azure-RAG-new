# Policy Ingestion - Complete Guide

## **Why Do We Need Policy Ingestion?**

### **Understanding the RAG System Architecture**

Your Claims RAG Bot uses **Retrieval-Augmented Generation (RAG)** which works like this:

```
User asks: "Does my motor policy cover flood damage?"
    ↓
1. YOUR CLAIM → Converted to vector embedding (mathematical representation)
    ↓
2. OPENSEARCH (Vector Database) → Searches 35 policy clauses to find most relevant ones
    ↓
3. BEDROCK LLM (Claude) → Reads the retrieved policy clauses + your question → Generates answer
    ↓
4. RESPONSE: "Based on clause MOT-015, flood damage is covered up to $50,000..."
```

### **The Problem Right Now**

**OpenSearch is EMPTY** - It has zero policy clauses stored. When you ask a question:
- The vector search returns nothing
- The LLM has no policy knowledge to work with
- You get generic answers instead of policy-specific validation

### **What Policy Ingestion Does**

The `PolicyIngestion` tool loads **35 insurance policy clauses** into OpenSearch:
- **20 Motor Insurance clauses** (coverage limits, exclusions, claim procedures)
- **15 Health Insurance clauses** (medical coverage, reimbursement rules, network providers)

**After ingestion**, when you validate a claim:
1. Your claim is converted to a vector
2. OpenSearch finds the 3-5 most relevant policy clauses (vector similarity search)
3. Bedrock LLM reads those clauses and validates your claim against actual policy rules
4. You get **accurate, policy-grounded answers** instead of hallucinated responses

---

## **PART 5: Policy Ingestion - Complete Steps**

### **Step 1: Fix OpenSearch Data Access Permissions**

#### **Why This Step is Needed:**
OpenSearch Serverless uses **data access policies** to control who can read/write to collections. Right now, the policy only allows a Bedrock service role to access the collection. Your IAM user needs permission to write the 35 policy clauses.

#### **Detailed Console Steps:**

1. **Login to AWS Console**
   - Go to: https://console.aws.amazon.com/
   - Region: **us-east-1** (verify top-right corner)

2. **Navigate to OpenSearch Service**
   - Search bar (top): Type **"OpenSearch Service"**
   - Click **"Amazon OpenSearch Service"**

3. **Go to Data Access Control**
   - Left sidebar: Under **"Serverless"** section
   - Click **"Data access control"**
   - You'll see a list of data access policies

4. **Find the Correct Policy**
   - Look for policy named: **`bedrock-knowledge-base-0u8c33`**
   - Type: **data**
   - Description: "Custom data access policy created by Amazon Bedrock..."
   - Click on the **policy name** (blue hyperlink)

5. **Edit the Policy**
   - Click **"Edit"** button (top-right of the policy detail page)
   - You'll see a JSON editor with the policy configuration

6. **Modify the Principal Array**
   
   **Current JSON (approximately):**
   ```json
   [
     {
       "Rules": [
         {
           "Resource": [
             "collection/bedrock-knowledge-base-ltm9gv"
           ],
           "Permission": [
             "aoss:DescribeCollectionItems",
             "aoss:CreateCollectionItems",
             "aoss:UpdateCollectionItems"
           ],
           "ResourceType": "collection"
         },
         {
           "Resource": [
             "index/bedrock-knowledge-base-ltm9gv/*"
           ],
           "Permission": [
             "aoss:CreateIndex",
             "aoss:ReadDocument",
             "aoss:WriteDocument",
             "aoss:UpdateIndex",
             "aoss:DescribeIndex"
           ],
           "ResourceType": "index"
         }
       ],
       "Principal": [
         "arn:aws:iam::123456789012:role/AmazonBedrock-SomeRoleName"
       ],
       "Description": "Custom data access policy..."
     }
   ]
   ```

   **Find this section:**
   ```json
   "Principal": [
     "arn:aws:iam::123456789012:role/AmazonBedrock-SomeRoleName"
   ]
   ```

   **Change it to (add your user):**
   ```json
   "Principal": [
     "arn:aws:iam::123456789012:role/AmazonBedrock-SomeRoleName",
     "arn:aws:iam::123456789012:user/your-username"
   ]
   ```

   **Important:** 
   - Add a **comma** after the first ARN
   - Copy your user ARN exactly: `arn:aws:iam::123456789012:user/your-username`
   - Don't remove the existing role ARN

7. **Save the Policy**
   - Click **"Save changes"** button (bottom-right)
   - You'll see a success message

8. **Wait for Propagation**
   - **Wait 60 seconds** - AWS needs time to propagate the policy changes across all services
   - This is critical - running the ingestion immediately will still fail

---

### **Step 2: Run the Policy Ingestion Tool**

#### **What This Does:**
The `PolicyIngestion` tool connects to your OpenSearch collection and:
1. Creates an index named `policy-clauses` with vector search configuration
2. Uploads 20 Motor Insurance policy clauses with vector embeddings
3. Uploads 15 Health Insurance policy clauses with vector embeddings

#### **Commands:**

**Open PowerShell and run:**

```powershell
# Navigate to the tool directory
cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\tools\PolicyIngestion

# Run the ingestion tool
dotnet run -- https://your-collection-id.us-east-1.aoss.amazonaws.com policy-clauses
```

#### **Expected Successful Output:**

```
========================================
Starting Policy Ingestion Process
========================================
OpenSearch Endpoint: https://your-collection-id.us-east-1.aoss.amazonaws.com
Index Name: policy-clauses
Region: us-east-1

Step 1: Creating index 'policy-clauses'...
✓ Index created successfully

Step 2: Ingesting Motor Insurance clauses...
Ingesting 20 policy clauses...
✓ Successfully indexed MOT-001: Motor Policy - Vehicle Coverage Basics
✓ Successfully indexed MOT-002: Comprehensive Coverage Definition
✓ Successfully indexed MOT-003: Third-Party Liability Coverage
✓ Successfully indexed MOT-004: Own Damage Coverage Scope
✓ Successfully indexed MOT-005: Flood Damage Coverage
✓ Successfully indexed MOT-006: Theft Coverage
✓ Successfully indexed MOT-007: Accident Coverage
✓ Successfully indexed MOT-008: Claim Filing Timeline
✓ Successfully indexed MOT-009: Required Documentation
✓ Successfully indexed MOT-010: Claim Settlement Process
✓ Successfully indexed MOT-011: Exclusions - Racing Events
✓ Successfully indexed MOT-012: Exclusions - Drunk Driving
✓ Successfully indexed MOT-013: Exclusions - Unlicensed Driver
✓ Successfully indexed MOT-014: Coverage Limits
✓ Successfully indexed MOT-015: Deductible Amount
✓ Successfully indexed MOT-016: Depreciation Rules
✓ Successfully indexed MOT-017: Total Loss Calculation
✓ Successfully indexed MOT-018: Repair Authorization
✓ Successfully indexed MOT-019: Parts Replacement Policy
✓ Successfully indexed MOT-020: Policy Renewal Terms
✓ Successfully indexed 20 clauses

Step 3: Ingesting Health Insurance clauses...
Ingesting 15 policy clauses...
✓ Successfully indexed HLT-001: Health Policy - Coverage Basics
✓ Successfully indexed HLT-002: Hospitalization Coverage
✓ Successfully indexed HLT-003: Outpatient Coverage
✓ Successfully indexed HLT-004: Pre-existing Conditions
✓ Successfully indexed HLT-005: Maternity Coverage
✓ Successfully indexed HLT-006: Critical Illness Coverage
✓ Successfully indexed HLT-007: Claim Submission Timeline
✓ Successfully indexed HLT-008: Required Medical Documents
✓ Successfully indexed HLT-009: Cashless Treatment Process
✓ Successfully indexed HLT-010: Reimbursement Process
✓ Successfully indexed HLT-011: Exclusions - Cosmetic Surgery
✓ Successfully indexed HLT-012: Exclusions - Alternative Medicine
✓ Successfully indexed HLT-013: Network Hospital Benefits
✓ Successfully indexed HLT-014: Room Rent Limits
✓ Successfully indexed HLT-015: Co-payment Requirements
✓ Successfully indexed 15 clauses

========================================
✓ Policy ingestion completed successfully!
Total clauses indexed: 35
========================================
```

#### **If You See Errors:**

**403 Forbidden errors:**
```
✗ Failed to index MOT-001: {"status":403,"error":{"reason":"403 Forbidden","type":"Forbidden"}}
```
- **Cause:** Data access policy not updated correctly OR policy hasn't propagated
- **Fix:** 
  1. Go back to AWS Console → OpenSearch → Data access control
  2. Verify your user ARN is in the Principal array
  3. Wait another 60 seconds and try again

**Connection timeout:**
```
Failed to connect to OpenSearch endpoint
```
- **Cause:** Wrong endpoint URL or network issue
- **Fix:** Verify endpoint matches your `appsettings.json`

---

### **Step 3: Verify Successful Ingestion**

#### **Option A: Query the Index (Recommended)**

```powershell
# Install the OpenSearch client if needed
# (This is just for verification - not required for the app to work)
```

#### **Option B: Test via the API**

Once ingestion is complete, test the full RAG pipeline:

1. **Start the API:**
   ```powershell
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack\src\ClaimsRagBot.Api
   dotnet run
   ```

2. **Send a test claim validation request:**
   ```powershell
   # Open another PowerShell window
   cd c:\Anupam_projects\NGA_AAP_ClaimsAutobot\AWS-stack
   
   # Test with a real claim
   $body = @{
       claimType = "Motor"
       claimAmount = 25000
       incidentDescription = "My car was damaged in a flood. The engine and interior were completely submerged."
       policyNumber = "POL-2024-001"
   } | ConvertTo-Json
   
   Invoke-RestMethod -Uri "http://localhost:5000/api/claims/validate" -Method POST -Body $body -ContentType "application/json"
   ```

3. **Expected Response:**
   ```json
   {
     "claimId": "CLM-2026-001",
     "isValid": true,
     "validationResult": "APPROVED",
     "reasoning": "Based on policy clause MOT-005 (Flood Damage Coverage), your claim is approved. Motor insurance policies cover flood damage up to the policy limit. Your claim amount of $25,000 is within the covered limit...",
     "confidence": 0.92,
     "relevantPolicyClauses": [
       "MOT-005: Flood Damage Coverage - Motor insurance covers damage caused by natural floods...",
       "MOT-014: Coverage Limits - Maximum claim amount for flood damage is $50,000...",
       "MOT-009: Required Documentation - Must submit photos, police report..."
     ]
   }
   ```

**Key Indicators of Success:**
- ✅ `relevantPolicyClauses` contains actual policy clause IDs (MOT-005, MOT-014)
- ✅ `reasoning` references specific policy clauses by ID
- ✅ No console warnings about "OpenSearch returned no results, using mock data"

---

## **What Happens Without Policy Ingestion?**

If you skip this step and run the API:

### **Before Ingestion (OpenSearch Empty):**
```json
{
  "claimId": "CLM-2026-001",
  "isValid": true,
  "validationResult": "APPROVED",
  "reasoning": "Based on general motor insurance principles, flood damage is typically covered...",
  "confidence": 0.45,
  "relevantPolicyClauses": []
}
```
- ❌ No specific policy clauses referenced
- ❌ Generic, potentially incorrect answer
- ❌ Low confidence score
- ❌ Console shows: "⚠️ OpenSearch returned no results, using LLM-only validation"

### **After Ingestion (OpenSearch with 35 Clauses):**
```json
{
  "claimId": "CLM-2026-001",
  "isValid": true,
  "validationResult": "APPROVED",
  "reasoning": "According to MOT-005 (Flood Damage Coverage), your policy explicitly covers flood damage...",
  "confidence": 0.92,
  "relevantPolicyClauses": [
    "MOT-005: Flood Damage Coverage",
    "MOT-014: Coverage Limits - $50,000 max",
    "MOT-009: Required Documents"
  ]
}
```
- ✅ Specific policy clauses with IDs
- ✅ Accurate, policy-grounded answer
- ✅ High confidence score
- ✅ Console shows: "✓ Retrieved 3 relevant policy clauses from OpenSearch"

---

## **Summary**

**Yes, you ARE using actual AWS RAG and OpenSearch**, but OpenSearch needs data to search through.

**The policy ingestion step:**
1. Populates your OpenSearch vector database with 35 insurance policy clauses
2. Enables semantic search to find relevant policies for any claim
3. Provides the LLM with actual policy knowledge instead of generic responses
4. Transforms your system from "generic chatbot" to "policy-aware validation engine"

**Without this step:** Your RAG system is like a library with no books - the search infrastructure works, but there's nothing to search.

**With this step:** Your RAG system can accurately validate claims against real policy rules using vector similarity search + LLM reasoning.
