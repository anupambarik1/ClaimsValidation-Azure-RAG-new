# Claim Search Feature - Implementation Summary

## ✅ Feature Complete: Search Claims by Claim ID or Policy Number

### Backend Changes

**1. New Model Created**
- `ClaimAuditRecord.cs` - Model for claim audit records stored in DynamoDB

**2. Updated Interface**
- `IAuditService.cs` - Added search methods:
  - `GetByClaimIdAsync(string claimId)` - Find single claim by ID
  - `GetByPolicyNumberAsync(string policyNumber)` - Find all claims for a policy

**3. Enhanced Service**
- `AuditService.cs` - Implemented search functionality:
  - GetByClaimIdAsync: Uses GetItem for single record retrieval
  - GetByPolicyNumberAsync: Uses Query with GSI (PolicyNumberIndex)
  - MapToClaimAuditRecord: Helper method to convert DynamoDB items

**4. New API Endpoints**
- `ClaimsController.cs` - Added two search endpoints:
  - `GET /api/claims/search/{claimId}` - Search by Claim ID
  - `GET /api/claims/search/policy/{policyNumber}` - Search by Policy Number

### Frontend Changes

**1. Updated Models**
- `claim.model.ts` - Added `ClaimAuditRecord` interface matching backend

**2. Enhanced API Service**
- `ClaimsApiService` - Added search methods:
  - `searchByClaimId(claimId: string)`
  - `searchByPolicyNumber(policyNumber: string)`

**3. New Component Created**
- `ClaimSearchComponent` with full Material UI:
  - Radio button toggle between search types (Claim ID / Policy Number)
  - Search form with validation
  - Loading spinner during search
  - Error message display
  - Single result display (Claim ID search)
  - Multiple results list (Policy Number search)
  - Color-coded status chips
  - Formatted currency and dates

**4. Integrated into Chat UI**
- Added "Search Claims" tab in chat component
- Material Design consistent with rest of application

### Features

**Search Options:**
- **By Claim ID**: Returns single detailed claim record
- **By Policy Number**: Returns all claims for that policy (sorted by date, most recent first)

**Display Information:**
- Claim ID and Status (color-coded: Covered=Primary, Not Covered=Warn, Manual Review=Accent)
- Policy Number and Timestamp
- Claim Amount (formatted as currency)
- Confidence Score (displayed as percentage)
- Full claim description
- AI decision explanation
- Referenced policy clauses (chips)
- Required documents (bulleted list)

### Database Requirements

⚠️ **Important**: The Policy Number search requires a DynamoDB Global Secondary Index (GSI):
- **Index Name**: `PolicyNumberIndex`
- **Partition Key**: `PolicyNumber` (String)
- **Sort Key**: `Timestamp` (String) - for date sorting

Without this GSI, searches by Policy Number will fail. The Claim ID search works immediately as it uses the primary key.

### Usage

**For End Users:**
1. Click on "Search Claims" tab
2. Select search type (Claim ID or Policy Number)
3. Enter the search value
4. Click "Search" button
5. View detailed results

**Testing:**
1. Start backend: `cd src/ClaimsRagBot.Api; dotnet run`
2. Start frontend: `cd claims-chatbot-ui; npm start`
3. Submit a claim to get a Claim ID
4. Use the Search tab to find it by Claim ID or Policy Number

### API Examples

```http
# Search by Claim ID
GET http://localhost:5184/api/claims/search/{claimId}

# Search by Policy Number
GET http://localhost:5184/api/claims/search/policy/POL-12345
```

## Next Steps

1. Create DynamoDB GSI for PolicyNumber searches
2. Test search functionality with real data
3. Consider adding additional filters (date range, status, amount range)
4. Add pagination for policy searches with many results
