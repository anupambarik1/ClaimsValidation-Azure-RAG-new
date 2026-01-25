# AWS Credentials Configuration

## Quick Setup

### 1. Add Your AWS Credentials to appsettings.json

Open `src/ClaimsRagBot.Api/appsettings.json` or `src/ClaimsRagBot.Api/appsettings.Development.json` and update:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "YOUR_AWS_ACCESS_KEY_ID",
    "SecretAccessKey": "YOUR_AWS_SECRET_ACCESS_KEY",
    "OpenSearchEndpoint": "https://your-opensearch-endpoint.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses"
  }
}
```

**Replace:**
- `AccessKeyId`: Your AWS Access Key ID
- `SecretAccessKey`: Your AWS Secret Access Key
- `Region`: Your preferred AWS region (default: us-east-1)
- `OpenSearchEndpoint`: (Optional) Your OpenSearch endpoint if using OpenSearch

### 2. Get AWS Credentials

To get your AWS credentials:

1. **AWS Console** → **IAM** → **Users** → [Your User] → **Security credentials**
2. Click **Create access key**
3. Choose **Application running outside AWS**
4. Copy the Access Key ID and Secret Access Key

### 3. Required IAM Permissions

Your AWS user/role needs these permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "bedrock:InvokeModel"
      ],
      "Resource": [
        "arn:aws:bedrock:*::foundation-model/anthropic.claude-3-sonnet-20240229-v1:0",
        "arn:aws:bedrock:*::foundation-model/amazon.titan-embed-text-v1"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:Query"
      ],
      "Resource": "arn:aws:dynamodb:*:*:table/ClaimsAuditTrail"
    },
    {
      "Effect": "Allow",
      "Action": [
        "aoss:APIAccessAll"
      ],
      "Resource": "arn:aws:aoss:*:*:collection/*"
    }
  ]
}
```

### 4. Enable Bedrock Models

Before using the application, enable these models in AWS Bedrock:

1. Go to **AWS Console** → **Bedrock** → **Base models** (or **Foundation models**)
2. Find and click on these models:
   - **Claude Sonnet 4** (or Claude 3.5 Sonnet v2 if Sonnet 4 not available)
   - **Titan Embeddings G1 - Text**
3. Click **"Request model access"** or **"Enable"** button on each model
4. Accept any terms and conditions
5. Wait for access to be granted (usually instant)

**Note:** AWS updated the Bedrock UI in 2025-2026. If you don't see "Base models", look for:
- **"Providers"** → **"Anthropic"** → Enable models
- Or a banner/notification about requesting model access

Model access is usually approved instantly.

### 5. Create DynamoDB Table (Optional - for audit trail)

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

### 6. Run the Application

```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

Navigate to: `http://localhost:5184/swagger`

## Security Best Practices

⚠️ **IMPORTANT:**
- **DO NOT** commit credentials to Git
- Add `appsettings.Development.json` to `.gitignore`
- For production, use AWS IAM roles instead of hardcoded credentials
- Consider using AWS Secrets Manager or Parameter Store

## Troubleshooting

### Error: "Failed to resolve bearer token"
✅ **Solution:** Your credentials are missing or invalid. Double-check AccessKeyId and SecretAccessKey in appsettings.json

### Error: "AccessDeniedException" from Bedrock
✅ **Solution:** Enable model access in AWS Console (Bedrock → Model access)

### Error: "ResourceNotFoundException" for DynamoDB
✅ **Solution:** Create the ClaimsAuditTrail table or the app will log error but continue working

### Credentials Work via AWS CLI but not in app?
✅ **Solution:** The app now prioritizes appsettings.json credentials. Ensure they're set correctly.

## Alternative: Use Environment Variables

If you prefer not to use appsettings.json:

```powershell
$env:AWS_ACCESS_KEY_ID="your-access-key"
$env:AWS_SECRET_ACCESS_KEY="your-secret-key"
$env:AWS_DEFAULT_REGION="us-east-1"
```

The app will fall back to the AWS credential chain if appsettings credentials are not provided.
