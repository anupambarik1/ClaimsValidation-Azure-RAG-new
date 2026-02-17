# Complete Claim Submission Workflow

## Overview
The Claims RAG Bot now supports a **complete end-to-end workflow** for claim submission with supporting documents.

## Workflow Steps

### 1. Initial Claim Submission
User can submit a claim in two ways:

#### Option A: Upload Claim Document
1. Go to **"Upload Document"** tab
2. Upload claim form (PDF, JPG, PNG, or TXT)
3. System automatically extracts claim data using OCR + NER + LLM
4. User reviews extracted data (Policy Number, Amount, Description)

#### Option B: Manual Entry
1. Go to **"Claim Form"** tab
2. Fill in claim details manually:
   - Policy Number
   - Policy Type (Motor, Home, Health, Life)
   - Claim Amount
   - Claim Description
3. Submit the form

### 2. Initial Validation
- System validates claim against policy clauses using **RAG (Retrieval-Augmented Generation)**
- Returns validation result with:
  - ✅ **Decision**: Covered / Not Covered / Manual Review
  - 📊 **Confidence Score**: AI confidence level
  - 📝 **Explanation**: Reasoning for the decision
  - 📋 **Relevant Policy Clauses**: Matching clauses from policy database
  - 📎 **Required Documents**: List of supporting documents needed

### 3. Supporting Documents Upload (NEW!)
If the validation requires supporting documents:

1. **Pending Claim Badge** appears in the header showing:
   - Number of documents uploaded
   - "Finalize Claim" button
   - "Cancel" option

2. User uploads each required document:
   - Hospital admission records
   - Discharge summary
   - Itemized bills
   - Emergency department records
   - Diagnostic test results
   - Physician treatment notes
   - Police reports (for motor claims)
   - Repair estimates (for motor/home claims)
   - Photos/evidence

3. Each upload is tracked and confirmed

### 4. Claim Finalization (NEW!)
Once all documents are uploaded:

1. Click **"Finalize Claim"** button in the header
2. System sends all data to backend:
   ```typescript
   {
     claimData: ClaimRequest,        // Original claim details
     supportingDocumentIds: string[], // All uploaded document IDs
     notes?: string                   // Optional notes
   }
   ```

3. Backend processes the complete claim:
   - Re-validates with supporting document context
   - Saves to audit trail (Cosmos DB)
   - Returns final decision

4. User receives **final claim decision** with:
   - ✅ Approval/rejection status
   - 📊 Updated confidence score
   - 📝 Complete reasoning
   - 📋 All relevant policy clauses
   - ✅ Confirmation of submitted documents

## API Endpoints

### New Endpoint Added
```http
POST /api/claims/finalize
Content-Type: application/json

{
  "claimData": {
    "policyNumber": "POL-2024-15678",
    "claimDescription": "Emergency appendectomy surgery",
    "claimAmount": 4250.00,
    "policyType": "Health"
  },
  "supportingDocumentIds": [
    "doc-123-admission",
    "doc-124-discharge",
    "doc-125-bill",
    "doc-126-ed-records",
    "doc-127-diagnostic",
    "doc-128-physician-notes"
  ],
  "notes": "All required documents submitted"
}
```

### Existing Endpoints
- `POST /api/documents/submit` - Upload & extract claim document
- `POST /api/documents/upload` - Upload supporting document only
- `POST /api/claims/validate` - Initial claim validation
- `GET /api/claims/search/{claimId}` - Search claim by ID
- `GET /api/claims/list` - List all claims

## Frontend Components Updated

### ChatComponent (`chat.component.ts`)
**New Features:**
- `pendingClaim`: Tracks claim awaiting supporting docs
- `supportingDocuments`: Array of uploaded document IDs
- `awaitingSupportingDocs`: Flag for workflow state
- `finalizeClaim()`: Submits complete claim package
- `cancelPendingClaim()`: Cancels pending workflow

### ClaimsApiService (`claims-api.service.ts`)
**New Method:**
```typescript
finalizeClaim(request: FinalizeClaimRequest): Observable<ClaimDecision>
```

### Models (`claim.model.ts`)
**New Interface:**
```typescript
export interface FinalizeClaimRequest {
  claimData: ClaimRequest;
  supportingDocumentIds?: string[];
  notes?: string;
}
```

## Backend Components Updated

### ClaimsController (`ClaimsController.cs`)
**New Endpoint:**
```csharp
[HttpPost("finalize")]
public async Task<ActionResult<ClaimDecision>> FinalizeClaim(
    [FromBody] FinalizeClaimRequest request)
```

### Models
**New Model:**
```csharp
// FinalizeClaimRequest.cs
public record FinalizeClaimRequest
{
    public required ClaimRequest ClaimData { get; init; }
    public List<string>? SupportingDocumentIds { get; init; }
    public string? Notes { get; init; }
}
```

## User Experience Flow

### Visual Indicators
1. **Pending Claim Badge** (in header):
   - Shows when claim is awaiting documents
   - Displays document count: "3 docs uploaded"
   - Prominent "Finalize Claim" button
   - Cancel option available

2. **Chat Messages**:
   - Clear instructions after initial validation
   - Confirmation after each document upload
   - Success message after finalization

### Example Chat Flow
```
User: [Uploads claim document]

Bot: Document processed successfully!
     Policy: POL-2024-15678
     Amount: $4,250
     Confidence: 92%

User: [Clicks "Validate Claim"]

Bot: Claim Validation Result:
     Decision: ✅ COVERED
     Confidence: 88.5%
     
     Required Documents:
     • Hospital admission and discharge summary
     • Itemized hospital bill
     • Emergency department records
     • Diagnostic test results
     • Physician treatment notes
     
     📎 Please upload the required supporting documents.
     Once all documents are uploaded, click "Finalize Claim".

[PENDING CLAIM BADGE APPEARS: "0 docs uploaded" | Finalize Claim | X]

User: [Uploads admission record via Upload Document tab]

Bot: 📄 Supporting document uploaded successfully!
     Document ID: doc-abc-123
     Total documents uploaded: 1
     
     You can upload more documents or click "Finalize Claim".

[BADGE UPDATES: "1 docs uploaded" | Finalize Claim | X]

User: [Uploads discharge, bill, ED records, diagnostic, physician notes]

[BADGE SHOWS: "6 docs uploaded" | Finalize Claim | X]

User: [Clicks "Finalize Claim"]

Bot: 🎉 Claim Finalized!
     Decision: ✅ COVERED
     Confidence: 91.2%
     
     Explanation: [AI reasoning with all context]
     
     Supporting Documents: 6 document(s) submitted and verified.
     
     ✅ Your claim has been submitted and saved in our system.
```

## Test Documents Provided

### Claim Document
- `claim-1-emergency-room.txt` - Main claim form (Sarah Johnson, $4,250)

### Supporting Documents
1. `claim-1-hospital-admission.txt` - Admission record
2. `claim-1-discharge-summary.txt` - Discharge summary  
3. `claim-1-itemized-bill.txt` - $33,550 itemized hospital bill
4. `claim-1-ed-records.txt` - Emergency department records
5. `claim-1-diagnostic-results.txt` - CT scan report
6. `claim-1-physician-notes.txt` - Operative and progress notes

## Testing the Complete Flow

1. **Restart API** to load new `/finalize` endpoint:
   ```powershell
   cd src/ClaimsRagBot.Api
   dotnet run
   ```

2. **Start Angular Frontend**:
   ```powershell
   cd claims-chatbot-ui
   npm start
   ```

3. **Test Workflow**:
   - Upload `claim-1-emergency-room.txt`
   - Click "Validate Claim"
   - Observe "Required Documents" list
   - Upload all 6 supporting documents
   - Watch badge update: "6 docs uploaded"
   - Click "Finalize Claim"
   - Verify final decision received

## What Was Missing Before

### ❌ Previous Incomplete Flow:
1. Upload claim → Extract data → Validate
2. System shows "Required Documents" list
3. **END** (no way to upload or finalize!)

### ✅ Complete Flow Now:
1. Upload claim → Extract data → Validate
2. System shows "Required Documents" list
3. **Upload each supporting document**
4. **Click "Finalize Claim"**
5. **Receive final validation with all context**
6. **Claim saved to audit trail**

## Benefits of Complete Workflow

1. **Full Document Lifecycle**: Upload → Validate → Support → Finalize
2. **Audit Trail**: All documents linked to claim in Cosmos DB
3. **Better Decisions**: LLM has access to supporting document context
4. **User Clarity**: Clear visual feedback on workflow state
5. **Compliance**: Complete documentation package for each claim
6. **Flexibility**: Can cancel and restart if needed

## Files Modified/Created

### Backend
- ✅ `ClaimsController.cs` - Added `/finalize` endpoint
- ✅ `FinalizeClaimRequest.cs` - New DTO model

### Frontend
- ✅ `claims-api.service.ts` - Added `finalizeClaim()` method
- ✅ `claim.model.ts` - Added `FinalizeClaimRequest` interface
- ✅ `chat.component.ts` - Added workflow state tracking & finalization
- ✅ `chat.component.html` - Added pending claim badge & buttons
- ✅ `chat.component.scss` - Styled pending claim badge

### Documentation
- ✅ `COMPLETE_CLAIM_WORKFLOW.md` - This file!

### Test Data
- ✅ `claim-1-hospital-admission.txt`
- ✅ `claim-1-discharge-summary.txt`
- ✅ `claim-1-itemized-bill.txt`
- ✅ `claim-1-ed-records.txt`
- ✅ `claim-1-diagnostic-results.txt`
- ✅ `claim-1-physician-notes.txt`

---

**Status**: ✅ COMPLETE - Full end-to-end claim workflow implemented!
