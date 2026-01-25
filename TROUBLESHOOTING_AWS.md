# Troubleshooting: "The security token included in the request is invalid"

## What I Fixed

Updated all AWS service clients to use proper configuration objects (`AmazonBedrockRuntimeConfig`, `AmazonDynamoDBConfig`) and added detailed error logging.

## Most Common Causes & Solutions

### 1. **Model Access Not Enabled** ⭐ MOST COMMON ISSUE

**Error:** "The security token included in the request is invalid"  
**Cause:** Bedrock models are not enabled for your AWS account

**Solution (Updated for 2026):**
1. Go to AWS Console: https://console.aws.amazon.com/bedrock
2. In the left navigation, click **"Base models"** or **"Foundation models"**
3. Find **"Claude Sonnet 4"** (or Claude 3.5 Sonnet v2)
4. Click on the model card
5. Click **"Request model access"** or **"Enable"** button
6. Accept terms and conditions if prompted
7. Wait for access to be granted (usually instant, can take up to a few minutes)
8. Repeat for **"Titan Embeddings G1 - Text"**

**Alternative (if Base models UI changed):**
- Look for **"Providers"** in left sidebar → **"Anthropic"** → Enable models
- Or check AWS Console top navigation for a **"Model access"** link/notification

### 2. **IAM Permissions Missing**

**Error:** "The security token included in the request is invalid" or "Access Denied"  
**Cause:** IAM user lacks Bedrock permissions

**Solution:**
Add this policy to your IAM user:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "bedrock:InvokeModel",
        "bedrock:InvokeModelWithResponseStream"
      ],
      "Resource": "*"
    }
  ]
}
```

**How to add:**
1. AWS Console → IAM → Users → [Your User]
2. Click **Add permissions** → **Attach policies directly**
3. Click **Create policy** → JSON tab → Paste above
4. Name it: `BedrockInvokeModelPolicy`
5. Attach to your user

### 3. **Wrong Region**

**Error:** "The security token included in the request is invalid"  
**Cause:** Bedrock not available in your selected region

**Bedrock Availability (as of Jan 2026):**
- ✅ `us-east-1` (N. Virginia) - RECOMMENDED
- ✅ `us-west-2` (Oregon)
- ✅ `ap-southeast-1` (Singapore)
- ✅ `eu-central-1` (Frankfurt)

**Solution:**
Update `appsettings.json`:
```json
"AWS": {
  "Region": "us-east-1"
}
```

### 4. **Incorrect Credentials Format**

**Symptoms:**
- Access Key doesn't start with `AKIA`
- Secret Key has spaces or line breaks
- Copy/paste errors

**Solution:**
Regenerate credentials:
1. AWS Console → IAM → Users → [Your User] → Security credentials
2. **Deactivate** old access key
3. Click **Create access key**
4. Select **Application running outside AWS**
5. Copy credentials carefully (no spaces!)
6. Update `appsettings.json` immediately

### 5. **Account Issues**

**Possible causes:**
- IAM user deleted/disabled
- Access keys deactivated
- AWS account suspended
- MFA required but not provided

**Check:**
```powershell
# If you have AWS CLI installed:
aws sts get-caller-identity --region us-east-1
```

Expected output:
```json
{
    "UserId": "AIDAEXAMPLE...",
    "Account": "123456789012",
    "Arn": "arn:aws:iam::123456789012:user/YourUser"
}
```

## Testing Your Fix

1. **Restart the application:**
   ```powershell
   # Stop current instance (Ctrl+C)
   cd src/ClaimsRagBot.Api
   dotnet run
   ```

2. **Check console output:**
   Look for:
   ```
   [Bedrock] Using credentials from appsettings for region: us-east-1
   ```

3. **Test API:**
   ```powershell
   curl -X POST http://localhost:5184/api/claims/validate `
     -H "Content-Type: application/json" `
     -d '{
       "policyNumber": "POL-12345",
       "claimDescription": "Test claim",
       "claimAmount": 1000,
       "policyType": "Motor"
     }'
   ```

4. **Check detailed errors:**
   The API now returns detailed error messages in the response

## Still Not Working?

### Option A: Verify Model Access Status
```
AWS Console → Bedrock → Model access
```
Status must show "Access granted" (green checkmark)

### Option B: Check IAM Policy Simulator
```
AWS Console → IAM → Policy Simulator
Select: bedrock:InvokeModel
Resource: *
```
Should show "allowed"

### Option C: Try Different Model
Update `LlmService.cs` line 63:
```csharp
ModelId = "anthropic.claude-instant-v1"  // Try a different model
```

## Quick Checklist

- [ ] Model access enabled in Bedrock console
- [ ] IAM user has `bedrock:InvokeModel` permission
- [ ] Using supported region (us-east-1 recommended)
- [ ] Access Key ID starts with `AKIA`
- [ ] Secret Access Key is 40 characters
- [ ] No spaces/line breaks in credentials
- [ ] Application restarted after config changes
- [ ] Checked application logs for detailed error

## Need More Help?

Run the application and check the console output. The detailed error messages will now show:
- Exact AWS error code
- Status code
- Helpful troubleshooting steps

Common error codes:
- `UnrecognizedClientException` → Credentials invalid or model access not enabled
- `AccessDeniedException` → IAM permissions missing
- `ValidationException` → Model ID wrong or not available in region
- `ResourceNotFoundException` → Model not found in region
