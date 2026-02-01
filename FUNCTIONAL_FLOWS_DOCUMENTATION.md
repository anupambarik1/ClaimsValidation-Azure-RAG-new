# Claims RAG Bot - Functional Flows Documentation

## Table of Contents
1. [Submit Claim via Chat Interface](#1-submit-claim-via-chat-interface)
2. [Upload and Extract Claim from Document](#2-upload-and-extract-claim-from-document)
3. [Manual Claim Form Submission](#3-manual-claim-form-submission)
4. [View Claims Dashboard](#4-view-claims-dashboard)
5. [Filter Claims by Status](#5-filter-claims-by-status)
6. [View Claim Details](#6-view-claim-details)
7. [Specialist Review and Decision Override](#7-specialist-review-and-decision-override)
8. [Search Claims by Policy Number](#8-search-claims-by-policy-number)

---

## 1. Submit Claim via Chat Interface

### Flow Description
User interacts with a chatbot to submit a claim by typing claim details in natural language. The system validates the claim against policy documents using AI.

### User Journey
1. User navigates to `/chat`
2. User types claim details in chat input
3. System processes and validates claim
4. AI decision displayed in chat with explanation
5. Claim saved to audit trail

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/chat/chat.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/chat/chat.component.html`
- **Styling**: `claims-chatbot-ui/src/app/components/chat/chat.component.scss`

**Key UI Elements**:
- Chat message history display
- Message input field
- Send button
- Message bubbles (user/bot)

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`
- **Method**: `validateClaim(claim: ClaimRequest): Observable<ClaimDecision>`

**Data Models** (`claims-chatbot-ui/src/app/models/claim.model.ts`):
- `ClaimRequest` - Input payload
- `ClaimDecision` - AI validation response

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- **Endpoint**: `POST /api/claims/validate`
- **Method**: `ValidateClaim([FromBody] ClaimRequest request)`

**Request Body**:
```csharp
public record ClaimRequest(
    string PolicyNumber,
    string ClaimDescription,
    decimal ClaimAmount,
    string PolicyType // "Motor", "Home", "Health", "Life"
);
```

**Response**:
```csharp
public record ClaimDecision(
    string Status,              // "Covered", "Not Covered", "Manual Review"
    string Explanation,
    List<string> ClauseReferences,
    List<string> RequiredDocuments,
    double ConfidenceScore
);
```

### Application Layer
- **File**: `src/ClaimsRagBot.Application/RAG/ClaimValidationOrchestrator.cs`
- **Method**: `ValidateClaimAsync(ClaimRequest request)`

**Orchestration Steps**:
1. Query OpenSearch for relevant policy clauses
2. Send to Bedrock for AI analysis
3. Save audit record to DynamoDB
4. Return decision

### AWS Services

#### OpenSearch (Vector Search)
- **File**: `src/ClaimsRagBot.Infrastructure/OpenSearch/OpenSearchService.cs`
- **Method**: `SearchPolicyClauses(string query, int maxResults)`
- **Service**: AWS OpenSearch Serverless
- **Purpose**: Retrieve relevant policy clauses using semantic search
- **Index**: Policy clauses with embeddings

#### Bedrock (AI/LLM)
- **File**: `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs`
- **Method**: `AnalyzeClaimAsync(ClaimRequest claim, List<PolicyClause> clauses)`
- **Service**: Amazon Bedrock
- **Model**: Claude 3.5 Sonnet
- **Purpose**: Analyze claim against policy clauses and make coverage decision

#### DynamoDB (Audit Trail)
- **File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- **Method**: `SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses)`
- **Service**: Amazon DynamoDB
- **Table**: `ClaimsAuditTrail`
- **Key Schema**: ClaimId (HASH)
- **Purpose**: Store audit trail of all claim validations

**DynamoDB Item Structure**:
```
{
  "ClaimId": "AFLAC-20260201-001",
  "Timestamp": "2026-01-12T10:00:00Z",
  "PolicyNumber": "AFLAC-HOSP-2024-001",
  "ClaimAmount": 1500,
  "ClaimDescription": "Hospital confinement 3 days",
  "DecisionStatus": "Covered",
  "Explanation": "Hospital Indemnity 500/day approved",
  "ConfidenceScore": 0.96,
  "ClauseReferences": ["CLAUSE-1", "CLAUSE-2"],
  "RequiredDocuments": ["Hospital Records"]
}
```

---

## 2. Upload and Extract Claim from Document

### Flow Description
User uploads a claim document (PDF, image, etc.), system extracts claim information using OCR and NLP, then validates the claim.

### User Journey
1. User navigates to `/chat`
2. User drags/drops document or clicks upload
3. System uploads to S3
4. Textract extracts text
5. Comprehend extracts entities
6. Pre-fills claim form
7. User reviews/submits
8. Claim validated via RAG

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/document-upload/document-upload.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/document-upload/document-upload.component.html`
- **Styling**: `claims-chatbot-ui/src/app/components/document-upload/document-upload.component.scss`

**Key UI Elements**:
- Drag-and-drop zone
- File picker button
- Upload progress indicator
- Extracted data preview

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`

**Methods**:
- `uploadDocument(file: File, userId?: string): Observable<DocumentUploadResult>`
- `extractFromDocument(documentId: string, documentType: DocumentType): Observable<ClaimExtractionResult>`
- `submitDocument(file: File, userId?: string, documentType: DocumentType): Observable<SubmitDocumentResponse>`

**Data Models**:
- `DocumentUploadResult` - S3 upload confirmation
- `ClaimExtractionResult` - Extracted claim data
- `SubmitDocumentResponse` - Combined upload + extraction

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/DocumentsController.cs`

**Endpoints**:
1. `POST /api/documents/upload` - Upload document to S3
2. `POST /api/documents/extract` - Extract claim data from document
3. `POST /api/documents/submit` - Upload + Extract in one call
4. `DELETE /api/documents/{id}` - Delete document from S3

### AWS Services

#### S3 (Document Storage)
- **File**: `src/ClaimsRagBot.Infrastructure/S3/S3Service.cs`
- **Methods**: 
  - `UploadDocumentAsync(Stream fileStream, string fileName, string contentType)`
  - `DeleteDocumentAsync(string documentId)`
- **Service**: Amazon S3
- **Bucket**: Configured in appsettings.json
- **Purpose**: Store uploaded claim documents

#### Textract (OCR)
- **File**: `src/ClaimsRagBot.Infrastructure/Textract/TextractService.cs`
- **Method**: `ExtractTextAsync(string s3Bucket, string s3Key)`
- **Service**: Amazon Textract
- **Features**: Text detection, table extraction, form data extraction
- **Purpose**: Extract text and structured data from documents

#### Comprehend (NLP)
- **File**: `src/ClaimsRagBot.Infrastructure/Comprehend/ComprehendService.cs`
- **Method**: `ExtractEntitiesAsync(string text)`
- **Service**: Amazon Comprehend
- **Purpose**: Extract named entities (amounts, dates, policy numbers) from text

#### Rekognition (Optional - Image Analysis)
- **File**: `src/ClaimsRagBot.Infrastructure/Rekognition/RekognitionService.cs`
- **Method**: `DetectLabelsAsync(string s3Bucket, string s3Key)`
- **Service**: Amazon Rekognition
- **Purpose**: Analyze damage photos for accident claims

---

## 3. Manual Claim Form Submission

### Flow Description
User manually enters claim details into a structured form, system validates and processes the claim.

### User Journey
1. User navigates to `/chat`
2. User fills out claim form fields
3. User clicks submit
4. System validates claim via RAG
5. Results displayed

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.html`
- **Styling**: `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.scss`

**Form Fields**:
- Policy Number (input)
- Claim Type (dropdown)
- Claim Amount (number)
- Claim Description (textarea)
- Submit button

### Frontend Flow
Same as [Flow #1](#1-submit-claim-via-chat-interface) after form submission.

---

## 4. View Claims Dashboard

### Flow Description
Specialist views a list of all claims with status indicators and filtering options.

### User Journey
1. User navigates to `/claims`
2. System loads all claims from DynamoDB
3. Claims displayed in table with status badges
4. User can filter, sort, and select claims

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/claims-list/claims-list.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/claims-list/claims-list.component.html`
- **Styling**: `claims-chatbot-ui/src/app/components/claims-list/claims-list.component.scss`

**Key UI Elements**:
- Data table with columns:
  - Claim ID
  - Policy Number
  - Claimant Name
  - Claim Type
  - Status (color-coded badge)
  - Explanation
  - Timestamp
  - Specialist Review Status
  - Actions (View Details button)
- Filter dropdown (All, Covered, Not Covered, Manual Review)
- Refresh button
- Results count

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`
- **Method**: `getAllClaims(status?: string): Observable<ClaimAuditRecord[]>`

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- **Endpoint**: `GET /api/claims/list?status={filter}`
- **Method**: `GetAllClaims([FromQuery] string? status)`

### Application Layer
- **File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- **Method**: `GetAllClaimsAsync(string? statusFilter)`

### AWS Services

#### DynamoDB (Query)
- **Operation**: Scan with optional FilterExpression
- **Table**: `ClaimsAuditTrail`
- **Filter**: `DecisionStatus = :status` (when status provided)
- **Purpose**: Retrieve all claims, optionally filtered by status

**Code Implementation**:
```csharp
var request = new ScanRequest
{
    TableName = TableName,
    FilterExpression = "DecisionStatus = :status",
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    {
        [":status"] = new AttributeValue { S = statusFilter }
    }
};
```

---

## 5. Filter Claims by Status

### Flow Description
User filters the claims list to show only claims with a specific status.

### User Journey
1. User on `/claims` page
2. User selects filter from dropdown (Covered/Not Covered/Manual Review/All)
3. Table updates to show filtered results
4. Results count updates

### UI Components
Same as [Flow #4](#4-view-claims-dashboard)

**Filter Logic** (`claims-list.component.ts`):
```typescript
filterClaims(): void {
  if (this.selectedStatus === 'All') {
    this.filteredClaims = this.claims;
  } else {
    this.filteredClaims = this.claims.filter(
      claim => claim.decisionStatus === this.selectedStatus
    );
  }
}
```

### Backend API
Same as [Flow #4](#4-view-claims-dashboard) - filter applied client-side or via query parameter.

---

## 6. View Claim Details

### Flow Description
User clicks on a claim to view full details including AI decision, reasons, and specialist review information.

### User Journey
1. User clicks "View Details" or claim row in table
2. Router navigates to `/claims/{claimId}`
3. System loads claim details from DynamoDB
4. Full claim information displayed

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/claim-detail/claim-detail.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/claim-detail/claim-detail.component.html`
- **Styling**: `claims-chatbot-ui/src/app/components/claim-detail/claim-detail.component.scss`

**Display Sections**:
1. **Claim Information Card**:
   - Claim ID
   - Policy Number
   - Claimant Name
   - Claim Type
   - Claim Amount
   - Incident Date
   - Submission Date
   - Current Status

2. **AI Validation Result Card**:
   - Explanation
   - Reasons list
   - Confidence score
   - Clause references

3. **Specialist Review Card** (if reviewed):
   - Reviewed by
   - Review date
   - Specialist notes

4. **Specialist Action Section**:
   - Make/Update Decision button
   - Decision form (collapsible)

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`
- **Method**: `getClaimById(claimId: string): Observable<ClaimAuditRecord>`

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- **Endpoint**: `GET /api/claims/{id}`
- **Method**: `GetClaimById(string id)`

### Application Layer
- **File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- **Method**: `GetByClaimIdAsync(string claimId)`

### AWS Services

#### DynamoDB (GetItem)
- **Operation**: GetItem
- **Table**: `ClaimsAuditTrail`
- **Key**: `{"ClaimId": {"S": "AFLAC-20260201-001"}}`
- **Purpose**: Retrieve single claim record by ClaimId

**Code Implementation**:
```csharp
var request = new GetItemRequest
{
    TableName = TableName,
    Key = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = claimId }
    }
};
var response = await _client.GetItemAsync(request);
return MapToClaimAuditRecord(response.Item);
```

---

## 7. Specialist Review and Decision Override

### Flow Description
Claims specialist reviews AI decision and can override it with their own judgment, adding notes.

### User Journey
1. User on claim details page (`/claims/{id}`)
2. User clicks "Make Decision" or "Update Decision"
3. Decision form appears with:
   - Specialist ID input
   - Status dropdown (Covered/Not Covered/Manual Review)
   - Notes textarea
4. User fills form and clicks "Submit Decision"
5. System updates claim in DynamoDB
6. Success message shown
7. Claim details refresh with updated information

### UI Components
Same as [Flow #6](#6-view-claim-details)

**Form Component** (`claim-detail.component.ts`):
```typescript
decisionUpdate: ClaimDecisionUpdate = {
  newStatus: '',
  specialistNotes: '',
  specialistId: ''
};

submitDecision(): void {
  this.claimsApiService
    .updateClaimDecision(this.claimId, this.decisionUpdate)
    .subscribe({
      next: () => {
        this.successMessage = 'Claim decision updated successfully!';
        this.loadClaimDetails(); // Refresh
      },
      error: (error) => {
        this.errorMessage = 'Failed to update claim decision';
      }
    });
}
```

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`
- **Method**: `updateClaimDecision(claimId: string, update: ClaimDecisionUpdate): Observable<any>`

**Request Model**:
```typescript
export interface ClaimDecisionUpdate {
  newStatus: string;        // "Covered", "Not Covered", "Manual Review"
  specialistNotes: string;  // Specialist's detailed reasoning
  specialistId: string;     // ID of reviewing specialist
}
```

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- **Endpoint**: `PUT /api/claims/{id}/decision`
- **Method**: `UpdateClaimDecision(string id, [FromBody] ClaimDecisionUpdate updateRequest)`

**Request Body**:
```csharp
public record ClaimDecisionUpdate(
    string NewStatus,
    string SpecialistNotes,
    string SpecialistId
);
```

### Application Layer
- **File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- **Method**: `UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId)`

### AWS Services

#### DynamoDB (UpdateItem)
- **Operation**: UpdateItem
- **Table**: `ClaimsAuditTrail`
- **Key**: `{"ClaimId": {"S": "claim-id"}}`
- **Update Expression**: `SET DecisionStatus = :status, SpecialistNotes = :notes, SpecialistId = :specialistId, ReviewedAt = :reviewedAt`
- **Purpose**: Update claim record with specialist decision

**Code Implementation**:
```csharp
var updateRequest = new UpdateItemRequest
{
    TableName = TableName,
    Key = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = claimId }
    },
    UpdateExpression = "SET DecisionStatus = :status, SpecialistNotes = :notes, SpecialistId = :specialistId, ReviewedAt = :reviewedAt",
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    {
        [":status"] = new AttributeValue { S = newStatus },
        [":notes"] = new AttributeValue { S = specialistNotes },
        [":specialistId"] = new AttributeValue { S = specialistId },
        [":reviewedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
    }
};
await _client.UpdateItemAsync(updateRequest);
```

---

## 8. Search Claims by Policy Number

### Flow Description
User searches for all claims associated with a specific policy number.

### User Journey
1. User enters policy number in search field
2. System queries DynamoDB GSI
3. All claims for that policy displayed

### UI Components
- **File**: `claims-chatbot-ui/src/app/components/claim-search/claim-search.component.ts`
- **Template**: `claims-chatbot-ui/src/app/components/claim-search/claim-search.component.html`

### Frontend Services
- **File**: `claims-chatbot-ui/src/app/services/claims-api.service.ts`
- **Method**: `searchByPolicyNumber(policyNumber: string): Observable<ClaimAuditRecord[]>`

### Backend API
- **Controller**: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- **Endpoint**: `GET /api/claims/search/policy/{policyNumber}`
- **Method**: `SearchByPolicyNumber(string policyNumber)`

### Application Layer
- **File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- **Method**: `GetByPolicyNumberAsync(string policyNumber)`

### AWS Services

#### DynamoDB (GSI Query)
- **Operation**: Query
- **Table**: `ClaimsAuditTrail`
- **Index**: `PolicyNumberIndex` (Global Secondary Index)
- **Key Condition**: `PolicyNumber = :policyNumber`
- **Purpose**: Retrieve all claims for a specific policy

**Code Implementation**:
```csharp
var request = new QueryRequest
{
    TableName = TableName,
    IndexName = "PolicyNumberIndex",
    KeyConditionExpression = "PolicyNumber = :policyNumber",
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    {
        [":policyNumber"] = new AttributeValue { S = policyNumber }
    }
};
```

---

## Infrastructure & Configuration

### Application Configuration
- **File**: `src/ClaimsRagBot.Api/appsettings.json`
- **Settings**:
  - AWS region
  - OpenSearch endpoint
  - S3 bucket name
  - DynamoDB table names
  - Bedrock model ID

### Dependency Injection
- **File**: `src/ClaimsRagBot.Api/Program.cs`
- **Services Registered**:
  - `IAuditService` → `AuditService`
  - `IOpenSearchService` → `OpenSearchService`
  - `ILlmService` → `LlmService`
  - `IS3Service` → `S3Service`
  - `ITextractService` → `TextractService`
  - `IComprehendService` → `ComprehendService`
  - `ClaimValidationOrchestrator`

### AWS Infrastructure (CloudFormation)
- **File**: `template.yaml`
- **Resources**:
  - DynamoDB Table: `ClaimsAuditTrail`
  - S3 Bucket: Document storage
  - OpenSearch Domain: Policy clause search
  - IAM Roles: Lambda execution roles
  - Lambda Functions: API handlers

---

## Data Flow Summary

### Claim Validation Flow
```
User Input (UI)
    ↓
ClaimsApiService (Frontend)
    ↓
ClaimsController.ValidateClaim (API)
    ↓
ClaimValidationOrchestrator (Application)
    ↓
    ├─→ OpenSearchService.SearchPolicyClauses (Infrastructure)
    │       ↓
    │   AWS OpenSearch (Vector Search)
    │
    ├─→ LlmService.AnalyzeClaimAsync (Infrastructure)
    │       ↓
    │   AWS Bedrock (Claude AI)
    │
    └─→ AuditService.SaveAsync (Infrastructure)
            ↓
        AWS DynamoDB (Audit Trail)
```

### Document Processing Flow
```
File Upload (UI)
    ↓
DocumentsController.SubmitDocument (API)
    ↓
    ├─→ S3Service.UploadDocumentAsync
    │       ↓
    │   AWS S3 (Storage)
    │
    ├─→ TextractService.ExtractTextAsync
    │       ↓
    │   AWS Textract (OCR)
    │
    ├─→ ComprehendService.ExtractEntitiesAsync
    │       ↓
    │   AWS Comprehend (NLP)
    │
    └─→ [Claim Validation Flow]
```

### Specialist Review Flow
```
Specialist Action (UI)
    ↓
ClaimsApiService.updateClaimDecision (Frontend)
    ↓
ClaimsController.UpdateClaimDecision (API)
    ↓
AuditService.UpdateClaimDecisionAsync (Infrastructure)
    ↓
AWS DynamoDB UpdateItem (Database)
```

---

## Key Interfaces & Models

### Core Interfaces
- **File**: `src/ClaimsRagBot.Core/Interfaces/`
  - `IAuditService.cs` - DynamoDB operations
  - `IOpenSearchService.cs` - Vector search operations
  - `ILlmService.cs` - AI/LLM operations
  - `IS3Service.cs` - Document storage
  - `ITextractService.cs` - OCR operations
  - `IComprehendService.cs` - NLP operations

### Core Models
- **File**: `src/ClaimsRagBot.Core/Models/`
  - `ClaimRequest.cs` - Input claim data
  - `ClaimDecision.cs` - AI validation result
  - `ClaimAuditRecord.cs` - DynamoDB audit record
  - `ClaimDecisionUpdate.cs` - Specialist update payload
  - `PolicyClause.cs` - Retrieved policy clause

---

## Routing Configuration

### Angular Routes
- **File**: `claims-chatbot-ui/src/app/app.config.ts`

```typescript
const routes: Routes = [
  { path: '', redirectTo: '/chat', pathMatch: 'full' },
  { path: 'chat', component: ChatComponent },              // Submit claims
  { path: 'claims', component: ClaimsListComponent },      // Dashboard
  { path: 'claims/:id', component: ClaimDetailComponent }  // Details & Review
];
```

### Navigation
- **File**: `claims-chatbot-ui/src/app/app.component.ts`
- **Nav Links**:
  - "Submit Claim" → `/chat`
  - "Claims Dashboard" → `/claims`

---

## Error Handling

### Frontend
- HTTP interceptors for API errors
- Error messages displayed in components
- Loading states during async operations

### Backend
- Try-catch blocks in controllers
- Logging via `ILogger<T>`
- Consistent error response format:
```csharp
return StatusCode(500, new { 
    error = "Error message",
    details = ex.Message,
    timestamp = DateTime.UtcNow
});
```

### AWS Services
- Retry logic in SDK clients
- Fallback mechanisms
- Error logging to CloudWatch (when deployed)

---

## Security Considerations

### Authentication
- AWS IAM roles for service-to-service auth
- API Gateway authentication (in production)
- Specialist ID validation (to be implemented)

### Authorization
- Role-based access control (RBAC) for specialist actions
- Policy-based permissions for AWS resources

### Data Protection
- HTTPS/TLS for all API calls
- Encryption at rest (DynamoDB, S3)
- Encryption in transit (AWS SDKs)
- PII handling in compliance with regulations

---

## Performance Optimization

### Frontend
- Lazy loading of components
- HTTP caching for static data
- Debouncing search inputs
- Virtual scrolling for large lists

### Backend
- Connection pooling for AWS clients
- Async/await for non-blocking I/O
- Pagination for large result sets

### AWS Services
- OpenSearch query optimization
- DynamoDB provisioned/on-demand capacity
- S3 transfer acceleration (optional)
- Bedrock streaming for faster responses

---

## Monitoring & Logging

### Application Logging
- **File**: Controllers, Services use `ILogger<T>`
- **Levels**: Information, Warning, Error
- **Output**: Console (local), CloudWatch (production)

### AWS Monitoring
- CloudWatch Logs for Lambda functions
- CloudWatch Metrics for DynamoDB, S3
- X-Ray for distributed tracing (optional)

### Audit Trail
- All claim validations logged to DynamoDB
- Specialist actions tracked with timestamp and ID
- Immutable audit records

---

## Deployment

### Local Development
1. Backend: `dotnet run` in `src/ClaimsRagBot.Api`
2. Frontend: `ng serve` in `claims-chatbot-ui`
3. Proxy configured in `proxy.conf.json`

### AWS Deployment
- **File**: `template.yaml` (SAM/CloudFormation)
- **Backend**: Lambda functions or ECS containers
- **Frontend**: S3 + CloudFront or Amplify
- **Database**: DynamoDB tables
- **AI Services**: Bedrock, OpenSearch

### CI/CD
- Build scripts in `package.json` and `.csproj`
- Automated testing with `dotnet test`
- Deployment via AWS SAM CLI or CDK

---

## Testing

### Unit Tests
- Backend: xUnit test projects
- Frontend: Jasmine/Karma tests

### Integration Tests
- API endpoint testing
- AWS service mocking with LocalStack

### End-to-End Tests
- Cypress or Playwright for UI flows
- Postman collections for API testing

---

## Future Enhancements

1. **Authentication**: Cognito integration for user login
2. **Notifications**: SNS/SES for claim status updates
3. **Analytics**: QuickSight dashboards for claim trends
4. **Multi-language**: i18n support
5. **Mobile App**: React Native or Flutter app
6. **Batch Processing**: Step Functions for bulk claim processing
7. **Machine Learning**: SageMaker for custom models
8. **Chatbot**: Lex integration for conversational interface
