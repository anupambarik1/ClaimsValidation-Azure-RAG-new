# Quick Start Guide - Claims RAG Bot

## Run the PoC Locally (No AWS Required for Basic Testing)

### 1. Build the solution
```powershell
dotnet build
```

### 2. Run the API
```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

The API will start at `http://localhost:5184` (or the port specified in launchSettings.json)

### 3. Test with sample claims

#### Option A: Using Swagger UI (Recommended)
1. Navigate to `http://localhost:5184/swagger` in your web browser
2. Click on the `POST /api/claims/validate` endpoint
3. Click "Try it out"
4. Enter sample claim data:
```json
{
  "policyNumber": "POL-12345",
  "claimDescription": "Car accident - front bumper damage",
  "claimAmount": 2500,
  "policyType": "Motor"
}
```
5. Click "Execute" to see the response

The Swagger UI provides:
- Interactive API testing
- Automatic request/response validation
- API documentation
- Request/response examples

#### Option B: Using PowerShell script
```powershell
# In a new terminal
./test-local.ps1
```

#### Option C: Using curl
```powershell
curl -X POST http://localhost:5184/api/claims/validate `
  -H "Content-Type: application/json" `
  -d '{
    "policyNumber": "POL-12345",
    "claimDescription": "Car accident - front bumper damage",
    "claimAmount": 2500,
    "policyType": "Motor"
  }'
```

## What Works Without AWS

✅ **Currently Working:**
- API structure and routing
- Clean architecture layers
- Business logic and decision rules
- Mock policy clause retrieval

⚠️ **Requires AWS Setup:**
- Bedrock LLM calls (needs AWS credentials + model access)
- DynamoDB audit logging (needs table creation)
- OpenSearch vector retrieval (mock data used currently)

## Next Steps to Enable Full AWS Integration

### 1. Configure AWS Credentials

**Option A: Using appsettings.json (Recommended for Development)**

Edit `src/ClaimsRagBot.Api/appsettings.json` and add your credentials:
```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "YOUR_ACCESS_KEY_ID",
    "SecretAccessKey": "YOUR_SECRET_ACCESS_KEY"
  }
}
```

See [AWS_CREDENTIALS_SETUP.md](AWS_CREDENTIALS_SETUP.md) for detailed instructions.

**Option B: Using AWS CLI**
```powershell
aws configure
# Enter: Access Key ID, Secret Access Key, Region (us-east-1)
```

### 2. Enable Bedrock Models
- Go to AWS Console → Bedrock → Model access
- Request access to:
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

### 4. Run with AWS Services
Once AWS is configured, the same API will automatically use real Bedrock LLM and DynamoDB audit.

## Expected Behavior

### Test Case: Motor Insurance Collision
**Input:**
```json
{
  "policyNumber": "POL-12345",
  "claimDescription": "Car accident - front bumper damage",
  "claimAmount": 2500
}
```

**Expected Output:**
```json
{
  "status": "Covered",
  "explanation": "Collision coverage applies...",
  "clauseReferences": ["MOT-001"],
  "confidenceScore": 0.92
}
```

### Test Case: High Amount Claim
**Input:** ClaimAmount = $45,000

**Expected Output:**
```json
{
  "status": "Manual Review",
  "explanation": "Amount exceeds auto-approval limit..."
}
```

## Troubleshooting

### API not starting?
```powershell
# Check if port 5001 is in use
netstat -ano | findstr :5001

# Run on different port
dotnet run --urls "https://localhost:5555"
```

### AWS Credentials error?
```powershell
# Verify credentials
aws sts get-caller-identity
```

### Bedrock access denied?
- Ensure model access is enabled in AWS Console
- Check IAM permissions for `bedrock:InvokeModel`
