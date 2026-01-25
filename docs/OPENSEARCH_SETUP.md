# OpenSearch Serverless Setup Guide

## Prerequisites
- AWS CLI configured with credentials
- AWS account with OpenSearch Serverless access

## Step 1: Create OpenSearch Serverless Collection

### Option A: Using AWS SAM (Recommended)
```powershell
sam deploy --guided
```

This will create:
- OpenSearch Serverless collection
- Network, encryption, and access policies
- DynamoDB audit table
- Lambda function with proper IAM roles

### Option B: Using AWS Console

1. **Go to OpenSearch Service Console**
   - Navigate to "Serverless" → "Collections"
   - Click "Create collection"

2. **Configure Collection**
   - Name: `claims-policies-dev`
   - Collection type: `Vector search`
   - Encryption: AWS owned key

3. **Network Settings**
   - Access type: Public
   - (For production: use VPC access)

4. **Data Access**
   - Create data access policy
   - Principal: Your Lambda IAM role ARN
   - Permissions: `aoss:*` for collection and indexes

5. **Create Collection**
   - Wait for collection to become Active (~5-10 minutes)
   - Note the collection endpoint

## Step 2: Ingest Policy Documents

### Get OpenSearch Endpoint
```powershell
# From SAM deployment output
sam list stack-outputs --stack-name <your-stack-name>

# Or from AWS CLI
aws opensearchserverless batch-get-collection --names claims-policies-dev
```

### Run Ingestion Tool
```powershell
cd tools/PolicyIngestion
dotnet run -- https://abc123.us-east-1.aoss.amazonaws.com
```

This will:
1. Create the `policy-clauses` index with KNN vector configuration
2. Generate embeddings using Bedrock Titan
3. Index all sample policy clauses (Motor + Health)

## Step 3: Configure API

Update `src/ClaimsRagBot.Api/appsettings.json`:
```json
{
  "AWS": {
    "Region": "us-east-1",
    "OpenSearchEndpoint": "https://abc123.us-east-1.aoss.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses"
  }
}
```

## Step 4: Test RAG Pipeline

```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

Test with real vector retrieval:
```powershell
curl -X POST https://localhost:5001/api/claims/validate `
  -H "Content-Type: application/json" `
  -k `
  -d '{
    "policyNumber": "POL-12345",
    "claimDescription": "Front bumper damaged in parking lot collision",
    "claimAmount": 2500,
    "policyType": "Motor"
  }'
```

## Verify OpenSearch Integration

### Check Index Exists
```powershell
$endpoint = "https://abc123.us-east-1.aoss.amazonaws.com"
curl "$endpoint/policy-clauses/_search?size=1"
```

### Test Vector Search
```powershell
curl -X POST "$endpoint/policy-clauses/_search" `
  -H "Content-Type: application/json" `
  -d '{
    "size": 3,
    "query": {
      "match": {
        "text": "collision damage"
      }
    }
  }'
```

## Costs (Estimated for PoC)

| Service | Usage | Cost/Month |
|---------|-------|------------|
| OpenSearch Serverless | 1 OCU search, 1 OCU indexing | ~$350 |
| Bedrock (Titan Embeddings) | ~1000 requests | ~$0.10 |
| Bedrock (Claude Sonnet) | ~1000 requests | ~$3.00 |
| DynamoDB | On-demand, minimal usage | ~$0.50 |
| **Total** | | **~$354** |

**Note:** OpenSearch Serverless has minimum 4 OCU requirement (~$700/mo). Consider using managed OpenSearch for lower-cost PoC.

## Troubleshooting

### Collection Not Active
```powershell
aws opensearchserverless batch-get-collection --names claims-policies-dev
# Wait until status is ACTIVE
```

### Access Denied
- Verify IAM role has `aoss:*` permissions
- Check data access policy includes your principal ARN
- Ensure network policy allows public access (or VPC if applicable)

### Ingestion Fails
```powershell
# Verify Bedrock model access
aws bedrock list-foundation-models --region us-east-1

# Check AWS credentials
aws sts get-caller-identity
```

### No Results from Vector Search
- Verify documents were ingested: `curl $endpoint/policy-clauses/_count`
- Check embedding dimension matches (1536 for Titan)
- Ensure KNN is enabled in index settings

## Production Considerations

1. **VPC Access**: Move to VPC endpoints instead of public access
2. **Fine-grained Access Control**: Use SAML or IAM identity center
3. **Cost Optimization**: Consider reserved capacity or managed OpenSearch
4. **Monitoring**: Enable CloudWatch metrics and alarms
5. **Backup**: Regular snapshots of policy documents
6. **Version Control**: Track policy clause versions in DynamoDB
