# Fix DynamoDB table and insert claims
$ErrorActionPreference = "Stop"

Write-Host "Step 1: Deleting old table..." -ForegroundColor Yellow
try {
    aws dynamodb delete-table --table-name ClaimsAuditTrail 2>&1 | Out-Null
    Write-Host "Waiting for deletion..." -ForegroundColor Yellow
    Start-Sleep -Seconds 20
} catch {
    Write-Host "Table may not exist, continuing..." -ForegroundColor Gray
}

Write-Host "Step 2: Creating new table with simple key..." -ForegroundColor Yellow
aws dynamodb create-table `
    --table-name ClaimsAuditTrail `
    --attribute-definitions AttributeName=ClaimId,AttributeType=S `
    --key-schema AttributeName=ClaimId,KeyType=HASH `
    --billing-mode PAY_PER_REQUEST `
    2>&1 | Out-Null

Write-Host "Waiting for table to be active..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "Step 3: Verifying key schema..." -ForegroundColor Yellow
$keySchema = aws dynamodb describe-table --table-name ClaimsAuditTrail --query 'Table.KeySchema' 2>&1
Write-Host $keySchema -ForegroundColor Cyan

Write-Host "`nStep 4: Inserting 20 claims..." -ForegroundColor Yellow
$claims = @(
    @("AFLAC-20260201-001","2026-01-12T10:00:00.000Z","AFLAC-HOSP-2024-001","1500","Hospital confinement 3 days pneumonia","Covered","Hospital Indemnity 500/day for 3 days approved","0.96"),
    @("AFLAC-20260201-002","2026-01-13T10:00:00.000Z","AFLAC-CANCER-2024-015","25000","Breast cancer Stage 2 diagnosis","Covered","Cancer policy 25000 lump sum approved","0.98"),
    @("AFLAC-20260201-003","2026-01-14T10:00:00.000Z","AFLAC-ACCIDENT-2024-007","750","ER treatment fractured wrist workplace fall","Covered","Accident policy ER 350 plus fracture 400 approved","0.94"),
    @("AFLAC-20260201-004","2026-01-15T10:00:00.000Z","AFLAC-DISABILITY-2024-022","6000","Disability 12 weeks knee surgery recovery","Covered","Disability 500/week for 12 weeks approved","0.92"),
    @("AFLAC-20260201-005","2026-01-16T10:00:00.000Z","AFLAC-CRITICAL-2024-033","15000","Heart attack emergency angioplasty ICU","Covered","Critical Illness 15000 heart attack approved","0.97"),
    @("AFLAC-20260201-006","2026-01-17T10:00:00.000Z","AFLAC-HOSP-2024-041","2000","ICU 2 days allergic reaction anaphylaxis","Covered","Hospital ICU 1000/day for 2 days approved","0.95"),
    @("AFLAC-20260201-007","2026-01-18T10:00:00.000Z","AFLAC-ACCIDENT-2024-012","2500","Dismemberment loss finger machinery accident","Covered","Accident dismemberment 2500 approved","0.93"),
    @("AFLAC-20260201-008","2026-01-19T10:00:00.000Z","AFLAC-CANCER-2024-008","5000","Chemotherapy 10 sessions colon cancer","Covered","Cancer 500 per session for 10 approved","0.96"),
    @("AFLAC-20260201-009","2026-01-20T10:00:00.000Z","AFLAC-CANCER-2024-003","25000","Basal cell carcinoma skin cancer","Not Covered","Excluded basal cell skin cancer denied","0.91"),
    @("AFLAC-20260201-010","2026-01-21T10:00:00.000Z","AFLAC-HOSP-2024-019","1000","Cosmetic rhinoplasty hospital admission","Not Covered","Cosmetic procedure excluded denied","0.97"),
    @("AFLAC-20260201-011","2026-01-22T10:00:00.000Z","AFLAC-ACCIDENT-2024-018","800","Back injury preexisting disc disease","Not Covered","Preexisting condition excluded denied","0.88"),
    @("AFLAC-20260201-012","2026-01-23T10:00:00.000Z","AFLAC-DISABILITY-2024-027","4000","Mental health depression outpatient 8 weeks","Not Covered","Mental health without hospitalization excluded","0.94"),
    @("AFLAC-20260201-013","2026-01-24T10:00:00.000Z","AFLAC-CRITICAL-2024-031","15000","TIA resolved 4 hours not stroke","Not Covered","TIA does not meet stroke definition denied","0.92"),
    @("AFLAC-20260201-014","2026-01-25T10:00:00.000Z","AFLAC-HOSP-2024-025","3500","Hospital 7 days observation vs admission unclear","Manual Review","Observation status needs clarification","0.71"),
    @("AFLAC-20260201-015","2026-01-26T10:00:00.000Z","AFLAC-CANCER-2024-038","25000","Melanoma in situ possible microinvasion","Manual Review","In situ vs invasive needs medical review","0.68"),
    @("AFLAC-20260201-016","2026-01-27T10:00:00.000Z","AFLAC-ACCIDENT-2024-041","1200","ACL tear recreational vs organized sports unclear","Manual Review","Sports exclusion needs investigation","0.64"),
    @("AFLAC-20260201-017","2026-01-28T10:00:00.000Z","AFLAC-DISABILITY-2024-029","10000","Fibromyalgia 20 weeks subjective symptoms","Manual Review","Chronic subjective condition needs IME","0.59"),
    @("AFLAC-20260201-018","2026-01-29T10:00:00.000Z","AFLAC-CRITICAL-2024-051","15000","Kidney failure dialysis preexisting question","Manual Review","Preexisting condition investigation needed","0.66"),
    @("AFLAC-20260201-019","2026-01-30T10:00:00.000Z","AFLAC-HOSP-2024-036","2500","Pneumonia 5 days prior outpatient treatment","Manual Review","Medical necessity of admission questioned","0.73"),
    @("AFLAC-20260201-020","2026-01-31T10:00:00.000Z","AFLAC-ACCIDENT-2024-034","1500","Concussion ER plus followup not disabling","Manual Review","Follow-up benefit eligibility unclear","0.70")
)

$inserted = 0
foreach ($c in $claims) {
    $item = @{
        "ClaimId" = @{"S" = $c[0]}
        "Timestamp" = @{"S" = $c[1]}
        "PolicyNumber" = @{"S" = $c[2]}
        "ClaimAmount" = @{"N" = $c[3]}
        "ClaimDescription" = @{"S" = $c[4]}
        "DecisionStatus" = @{"S" = $c[5]}
        "Explanation" = @{"S" = $c[6]}
        "ConfidenceScore" = @{"N" = $c[7]}
        "ClauseReferences" = @{"L" = @(@{"S" = "CLAUSE-1"}, @{"S" = "CLAUSE-2"})}
        "RequiredDocuments" = @{"L" = @(@{"S" = "Medical Records"}, @{"S" = "Hospital Records"})}
    } | ConvertTo-Json -Compress -Depth 10
    
    $item | Out-File -FilePath ".\temp-item.json" -Encoding ASCII -NoNewline
    
    aws dynamodb put-item --table-name ClaimsAuditTrail --item file://temp-item.json 2>&1 | Out-Null
    if ($?) {
        $inserted++
        Write-Host "$($c[0]) ✓" -ForegroundColor Green
    } else {
        Write-Host "$($c[0]) ✗" -ForegroundColor Red
    }
}

if (Test-Path ".\temp-item.json") { Remove-Item ".\temp-item.json" }

Write-Host "`n✅ Inserted $inserted / 20 claims" -ForegroundColor Green
Write-Host "`nNow restart your backend: cd src\ClaimsRagBot.Api; dotnet run" -ForegroundColor Yellow
