# Insert Aflac Claims into DynamoDB
param([string]$Region = "us-east-1", [string]$TableName = "ClaimsAuditTrail-dev")
Write-Host "Inserting claims into $TableName..." -ForegroundColor Green
$total = 0
$date = (Get-Date).ToString("yyyyMMdd")
# Claim 1 - Approved Hospital
aws dynamodb put-item --table-name $TableName --region $Region --item '{\"ClaimId\":{\"S\":\"AFL-'$date'-001\"},\"Timestamp\":{\"S\":\"2026-01-20T10:00:00.000Z\"},\"PolicyNumber\":{\"S\":\"AFLAC-HOSP-2024-001\"},\"ClaimAmount\":{\"N\":\"1500\"},\"ClaimDescription\":{\"S\":\"Hospital confinement for 3 days due to pneumonia\"},\"DecisionStatus\":{\"S\":\"Covered\"},\"Explanation\":{\"S\":\"Claim approved. Hospital benefit applies at 500/day for 3 days\"},\"ConfidenceScore\":{\"N\":\"0.96\"},\"ClauseReferences\":{\"L\":[{\"S\":\"AFLAC-HOSP-2.1\"},{\"S\":\"AFLAC-HOSP-3.2\"}]},\"RequiredDocuments\":{\"L\":[{\"S\":\"Hospital Admission\"},{\"S\":\"Discharge Summary\"}]},\"RetrievedClauses\":{\"S\":\"[{\\\"ClauseId\\\":\\\"AFLAC-HOSP-2.1\\\",\\\"Score\\\":0.85}]\"}}' | Out-Null
if ($?) { Write-Host "✓ Claim 1 - Hospital pneumonia (Covered)" -ForegroundColor Green; $total++ }
# Claim 2 - Approved Cancer
aws dynamodb put-item --table-name $TableName --region $Region --item '{\"ClaimId\":{\"S\":\"AFL-'$date'-002\"},\"Timestamp\":{\"S\":\"2026-01-21T10:00:00.000Z\"},\"PolicyNumber\":{\"S\":\"AFLAC-CANCER-2024-015\"},\"ClaimAmount\":{\"N\":\"25000\"},\"ClaimDescription\":{\"S\":\"Initial diagnosis of breast cancer Stage 2\"},\"DecisionStatus\":{\"S\":\"Covered\"},\"Explanation\":{\"S\":\"Claim approved. Cancer policy provides 25000 lump sum for internal cancer diagnosis\"},\"ConfidenceScore\":{\"N\":\"0.98\"},\"ClauseReferences\":{\"L\":[{\"S\":\"AFLAC-CANCER-4.1\"},{\"S\":\"AFLAC-CANCER-4.3\"}]},\"RequiredDocuments\":{\"L\":[{\"S\":\"Pathology Report\"},{\"S\":\"Biopsy Results\"}]},\"RetrievedClauses\":{\"S\":\"[{\\\"ClauseId\\\":\\\"AFLAC-CANCER-4.1\\\",\\\"Score\\\":0.92}]\"}}' | Out-Null
if ($?) { Write-Host "✓ Claim 2 - Breast cancer (Covered)" -ForegroundColor Green; $total++ }
Write-Host "`nTotal inserted: $total/2" -ForegroundColor Cyan
