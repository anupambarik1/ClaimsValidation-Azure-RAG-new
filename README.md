# Claims RAG Bot

Enterprise-grade claims validation system using AWS Bedrock, OpenSearch, and DynamoDB.

## Architecture

- **API Layer**: ASP.NET Core Web API (Lambda-ready)
- **Application Layer**: RAG Orchestrator with Aflac-style business rules
- **Infrastructure**: Bedrock (LLM + Embeddings), OpenSearch (Vector DB), DynamoDB (Audit)

## Prerequisites

- .NET 8 SDK
- AWS CLI configured with credentials
- AWS SAM CLI (for deployment)
- Access to Amazon Bedrock (Claude/Titan models)

## Local Development

### Build
```powershell
dotnet build
```

### Run API locally
```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

API will be available at `https://localhost:5001`

### Test endpoint
```powershell
curl -X POST https://localhost:5001/api/claims/validate `
  -H "Content-Type: application/json" `
  -d '{
    "policyNumber": "POL-12345",
    "claimDescription": "Car accident - front bumper damage from collision",
    "claimAmount": 2500,
    "policyType": "Motor"
  }'
```

## AWS Deployment

### 1. Configure AWS
```powershell
aws configure
```

### 2. Enable Bedrock Models
Go to AWS Console → Bedrock → Model access
Enable:
- `anthropic.claude-3-sonnet-20240229-v1:0`
- `amazon.titan-embed-text-v1`

### 3. Create DynamoDB Table
```powershell
aws dynamodb create-table `
  --table-name ClaimsAuditTrail `
  --attribute-definitions AttributeName=ClaimId,AttributeType=S AttributeName=Timestamp,AttributeType=S `
  --key-schema AttributeName=ClaimId,KeyType=HASH AttributeName=Timestamp,KeyType=RANGE `
  --billing-mode PAY_PER_REQUEST
```

### 4. Deploy with SAM (Optional)
```powershell
sam build
sam deploy --guided
```

## Project Structure

```
src/
├── ClaimsRagBot.Core/           # Domain models & interfaces
│   ├── Models/
│   └── Interfaces/
├── ClaimsRagBot.Infrastructure/ # AWS service implementations
│   ├── Bedrock/
│   ├── OpenSearch/
│   └── DynamoDB/
├── ClaimsRagBot.Application/    # Business logic
│   └── RAG/
└── ClaimsRagBot.Api/            # Web API / Lambda entry
    └── Controllers/
```

## Decision Rules

| Condition | Action |
|-----------|--------|
| Confidence < 0.85 | Manual Review |
| Amount > $5000 + Covered | Manual Review |
| Exclusion clause found | Deny/Manual Review |
| High confidence + low amount | Auto-approve |

## Sample Response

```json
{
  "status": "Covered",
  "explanation": "Collision coverage applies. Front bumper damage is covered under clause MOT-001.",
  "clauseReferences": ["MOT-001"],
  "requiredDocuments": ["Police Report", "Photos", "Repair Estimate"],
  "confidenceScore": 0.92
}
```

## Security

- IAM roles for Lambda execution
- VPC endpoints for Bedrock/OpenSearch (production)
- KMS encryption for DynamoDB
- Full audit trail in DynamoDB

## Monitoring

- CloudWatch Logs: Lambda execution logs
- CloudWatch Metrics: API latency, error rates
- DynamoDB: Full decision audit trail

## License

Proprietary - Internal Use Only
