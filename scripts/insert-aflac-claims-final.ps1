# Insert 20 Aflac Health Insurance Claims into DynamoDB
# This script successfully inserts approved, denied, and manual review claims

param(
    [string]$Region = "us-east-1",
    [string]$TableName = "ClaimsAuditTrail"
)

$total = 0
Write-Host "Inserting 20 Aflac claims into $TableName..." -ForegroundColor Green

# Array of 20 claims: [ClaimId, Timestamp, PolicyNumber, Amount, Description, Status, Explanation, Confidence, Clause1, Clause2]
$claims = @(
    @("AFLAC-20260201-001","2026-01-12T10:00:00.000Z","AFLAC-HOSP-2024-001","1500","Hospital confinement 3 days pneumonia","Covered","Hospital Indemnity 500/day for 3 days approved","0.96","AFLAC-HOSP-2.1","AFLAC-HOSP-3.2"),
    @("AFLAC-20260201-002","2026-01-13T10:00:00.000Z","AFLAC-CANCER-2024-015","25000","Breast cancer Stage 2 diagnosis","Covered","Cancer policy 25000 lump sum approved","0.98","AFLAC-CANCER-4.1","AFLAC-CANCER-4.3"),
    @("AFLAC-20260201-003","2026-01-14T10:00:00.000Z","AFLAC-ACCIDENT-2024-007","750","ER treatment fractured wrist workplace fall","Covered","Accident policy ER 350 plus fracture 400 approved","0.94","AFLAC-ACCIDENT-5.2","AFLAC-ACCIDENT-6.1"),
    @("AFLAC-20260201-004","2026-01-15T10:00:00.000Z","AFLAC-DISABILITY-2024-022","6000","Disability 12 weeks knee surgery recovery","Covered","Disability 500/week for 12 weeks approved","0.92","AFLAC-DISABILITY-7.1","AFLAC-DISABILITY-7.3"),
    @("AFLAC-20260201-005","2026-01-16T10:00:00.000Z","AFLAC-CRITICAL-2024-033","15000","Heart attack emergency angioplasty ICU","Covered","Critical Illness 15000 heart attack approved","0.97","AFLAC-CRITICAL-8.2","AFLAC-CRITICAL-8.5"),
    @("AFLAC-20260201-006","2026-01-17T10:00:00.000Z","AFLAC-HOSP-2024-041","2000","ICU 2 days allergic reaction anaphylaxis","Covered","Hospital ICU 1000/day for 2 days approved","0.95","AFLAC-HOSP-2.4","AFLAC-HOSP-2.1"),
    @("AFLAC-20260201-007","2026-01-18T10:00:00.000Z","AFLAC-ACCIDENT-2024-012","2500","Dismemberment loss finger machinery accident","Covered","Accident dismemberment 2500 approved","0.93","AFLAC-ACCIDENT-6.4","AFLAC-ACCIDENT-5.1"),
    @("AFLAC-20260201-008","2026-01-19T10:00:00.000Z","AFLAC-CANCER-2024-008","5000","Chemotherapy 10 sessions colon cancer","Covered","Cancer 500 per session for 10 approved","0.96","AFLAC-CANCER-4.6","AFLAC-CANCER-4.1"),
    @("AFLAC-20260201-009","2026-01-20T10:00:00.000Z","AFLAC-CANCER-2024-003","25000","Basal cell carcinoma skin cancer","Not Covered","Excluded basal cell skin cancer denied","0.91","AFLAC-CANCER-9.1","AFLAC-CANCER-9.3"),
    @("AFLAC-20260201-010","2026-01-21T10:00:00.000Z","AFLAC-HOSP-2024-019","1000","Cosmetic rhinoplasty hospital admission","Not Covered","Cosmetic procedure excluded denied","0.97","AFLAC-HOSP-9.2","AFLAC-HOSP-9.4"),
    @("AFLAC-20260201-011","2026-01-22T10:00:00.000Z","AFLAC-ACCIDENT-2024-018","800","Back injury preexisting disc disease","Not Covered","Preexisting condition excluded denied","0.88","AFLAC-ACCIDENT-9.5","AFLAC-ACCIDENT-9.1"),
    @("AFLAC-20260201-012","2026-01-23T10:00:00.000Z","AFLAC-DISABILITY-2024-027","4000","Mental health depression outpatient 8 weeks","Not Covered","Mental health without hospitalization excluded","0.94","AFLAC-DISABILITY-9.6","AFLAC-DISABILITY-9.1"),
    @("AFLAC-20260201-013","2026-01-24T10:00:00.000Z","AFLAC-CRITICAL-2024-031","15000","TIA resolved 4 hours not stroke","Not Covered","TIA does not meet stroke definition denied","0.92","AFLAC-CRITICAL-8.3","AFLAC-CRITICAL-9.2"),
    @("AFLAC-20260201-014","2026-01-25T10:00:00.000Z","AFLAC-HOSP-2024-025","3500","Hospital 7 days observation vs admission unclear","Manual Review","Observation status needs clarification","0.71","AFLAC-HOSP-2.1","AFLAC-HOSP-2.6"),
    @("AFLAC-20260201-015","2026-01-26T10:00:00.000Z","AFLAC-CANCER-2024-038","25000","Melanoma in situ possible microinvasion","Manual Review","In situ vs invasive needs medical review","0.68","AFLAC-CANCER-4.1","AFLAC-CANCER-9.3"),
    @("AFLAC-20260201-016","2026-01-27T10:00:00.000Z","AFLAC-ACCIDENT-2024-041","1200","ACL tear recreational vs organized sports unclear","Manual Review","Sports exclusion needs investigation","0.64","AFLAC-ACCIDENT-5.1","AFLAC-ACCIDENT-9.8"),
    @("AFLAC-20260201-017","2026-01-28T10:00:00.000Z","AFLAC-DISABILITY-2024-029","10000","Fibromyalgia 20 weeks subjective symptoms","Manual Review","Chronic subjective condition needs IME","0.59","AFLAC-DISABILITY-7.2","AFLAC-DISABILITY-9.6"),
    @("AFLAC-20260201-018","2026-01-29T10:00:00.000Z","AFLAC-CRITICAL-2024-051","15000","Kidney failure dialysis preexisting question","Manual Review","Preexisting condition investigation needed","0.66","AFLAC-CRITICAL-8.6","AFLAC-CRITICAL-9.1"),
    @("AFLAC-20260201-019","2026-01-30T10:00:00.000Z","AFLAC-HOSP-2024-036","2500","Pneumonia 5 days prior outpatient treatment","Manual Review","Medical necessity of admission questioned","0.73","AFLAC-HOSP-2.1","AFLAC-HOSP-2.8"),
    @("AFLAC-20260201-020","2026-01-31T10:00:00.000Z","AFLAC-ACCIDENT-2024-034","1500","Concussion ER plus followup not disabling","Manual Review","Follow-up benefit eligibility unclear","0.70","AFLAC-ACCIDENT-5.2","AFLAC-ACCIDENT-6.2")
)

foreach ($c in $claims) {
    # Build JSON structure for DynamoDB item
    $item = "{`"ClaimId`":{`"S`":`"$($c[0])`"},`"Timestamp`":{`"S`":`"$($c[1])`"},`"PolicyNumber`":{`"S`":`"$($c[2])`"},`"ClaimAmount`":{`"N`":`"$($c[3])`"},`"ClaimDescription`":{`"S`":`"$($c[4])`"},`"DecisionStatus`":{`"S`":`"$($c[5])`"},`"Explanation`":{`"S`":`"$($c[6])`"},`"ConfidenceScore`":{`"N`":`"$($c[7])`"},`"ClauseReferences`":{`"L`":[{`"S`":`"$($c[8])`"},{`"S`":`"$($c[9])`"}]},`"RequiredDocuments`":{`"L`":[{`"S`":`"Hospital Records`"},{`"S`":`"Medical Reports`"}]},`"RetrievedClauses`":{`"S`":`"[{\\\`"ClauseId\\\`":\\\`"$($c[8])\\\`",\\\`"Score\\\`":0.85}]`"}}"
    
    # Save to temp JSON file
    $item | Out-File -FilePath ".\temp-claim.json" -Encoding ASCII -NoNewline
    
    # Insert into DynamoDB
    aws dynamodb put-item --table-name $TableName --region $Region --item file://temp-claim.json 2>$null
    
    if ($?) {
        $total++
        Write-Host "$($c[0]) ✓" -ForegroundColor Green
    } else {
        Write-Host "$($c[0]) ✗" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 150
}

# Cleanup
Remove-Item ".\temp-claim.json" -ErrorAction SilentlyContinue

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Inserted $total/20 claims successfully" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Breakdown:" -ForegroundColor Yellow
Write-Host "  8 Covered (Approved)" -ForegroundColor Green
Write-Host "  5 Not Covered (Denied)" -ForegroundColor Red
Write-Host "  7 Manual Review" -ForegroundColor Magenta
Write-Host ""
