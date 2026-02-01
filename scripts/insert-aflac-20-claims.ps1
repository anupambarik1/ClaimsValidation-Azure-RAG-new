# Insert 20 Aflac Health Insurance Claims into DynamoDB
# Using JSON file approach for reliability

param(
    [string]$Region = "us-east-1",
    [string]$TableName = "ClaimsAuditTrail"
)

Write-Host "`nInserting 20 Aflac claims into $TableName...`n" -ForegroundColor Green

$claims = @(
    # 8 APPROVED CLAIMS
    @{ClaimId="AFLAC-20260201-001";Timestamp="2026-01-12T10:00:00.000Z";PolicyNumber="AFLAC-HOSP-2024-001";ClaimAmount=1500;ClaimDescription="Hospital confinement for 3 days due to pneumonia diagnosis and treatment";DecisionStatus="Covered";Explanation="Claim approved. Hospital Indemnity benefit applies at 500/day for confined hospital stay. Policy Section 2.1 covers pneumonia. Total: 3 days x 500 = 1500";ConfidenceScore=0.96;ClauseRefs=@("AFLAC-HOSP-2.1-CONFINEMENT","AFLAC-HOSP-3.2-DAILY-BENEFIT");ReqDocs=@("Hospital Admission Record","Discharge Summary","Physician Statement")},
    @{ClaimId="AFLAC-20260201-002";Timestamp="2026-01-13T10:00:00.000Z";PolicyNumber="AFLAC-CANCER-2024-015";ClaimAmount=25000;ClaimDescription="Initial diagnosis of breast cancer Stage 2";DecisionStatus="Covered";Explanation="Claim approved. Cancer policy provides lump sum benefit of 25000 upon first diagnosis of internal cancer. Policy Section 4.1 covers breast cancer diagnosis";ConfidenceScore=0.98;ClauseRefs=@("AFLAC-CANCER-4.1-INITIAL-DIAGNOSIS","AFLAC-CANCER-4.3-INTERNAL-CANCER");ReqDocs=@("Pathology Report","Oncologist Diagnosis","Biopsy Results")},
    @{ClaimId="AFLAC-20260201-003";Timestamp="2026-01-14T10:00:00.000Z";PolicyNumber="AFLAC-ACCIDENT-2024-007";ClaimAmount=750;ClaimDescription="Emergency room treatment for fractured wrist from fall at workplace";DecisionStatus="Covered";Explanation="Claim approved. Accident policy covers ER treatment (350) plus fracture benefit (400). Total benefit: 750";ConfidenceScore=0.94;ClauseRefs=@("AFLAC-ACCIDENT-5.2-ER-TREATMENT","AFLAC-ACCIDENT-6.1-FRACTURE");ReqDocs=@("ER Report","X-Ray Results","Treatment Records")},
    @{ClaimId="AFLAC-20260201-004";Timestamp="2026-01-15T10:00:00.000Z";PolicyNumber="AFLAC-DISABILITY-2024-022";ClaimAmount=6000;ClaimDescription="Short-term disability for 12 weeks following knee surgery and recovery";DecisionStatus="Covered";Explanation="Claim approved. Short-term disability pays 500/week for medically necessary surgery recovery. 12 weeks x 500 = 6000";ConfidenceScore=0.92;ClauseRefs=@("AFLAC-DISABILITY-7.1-SURGERY","AFLAC-DISABILITY-7.3-WEEKLY-BENEFIT");ReqDocs=@("Surgical Report","Physician Disability Statement","Recovery Timeline")},
    @{ClaimId="AFLAC-20260201-005";Timestamp="2026-01-16T10:00:00.000Z";PolicyNumber="AFLAC-CRITICAL-2024-033";ClaimAmount=15000;ClaimDescription="Heart attack requiring emergency angioplasty and ICU admission";DecisionStatus="Covered";Explanation="Claim approved. Critical Illness policy provides 15000 lump sum for first heart attack with cardiac enzyme tests and ECG findings";ConfidenceScore=0.97;ClauseRefs=@("AFLAC-CRITICAL-8.2-HEART-ATTACK","AFLAC-CRITICAL-8.5-FIRST-OCCURRENCE");ReqDocs=@("Cardiac Enzyme Results","ECG Report","Hospital Records")},
    @{ClaimId="AFLAC-20260201-006";Timestamp="2026-01-17T10:00:00.000Z";PolicyNumber="AFLAC-HOSP-2024-041";ClaimAmount=2000;ClaimDescription="ICU admission for 2 days following severe allergic reaction and anaphylaxis";DecisionStatus="Covered";Explanation="Claim approved. Hospital policy pays enhanced ICU benefit at 1000/day. 2 days x 1000 = 2000";ConfidenceScore=0.95;ClauseRefs=@("AFLAC-HOSP-2.4-ICU-BENEFIT","AFLAC-HOSP-2.1-CONFINEMENT");ReqDocs=@("ICU Records","Physician Notes","Medication Administration Records")},
    @{ClaimId="AFLAC-20260201-007";Timestamp="2026-01-18T10:00:00.000Z";PolicyNumber="AFLAC-ACCIDENT-2024-012";ClaimAmount=2500;ClaimDescription="Accidental dismemberment loss of index finger in machinery accident";DecisionStatus="Covered";Explanation="Claim approved. Accident policy provides dismemberment benefit. Policy Section 6.4 covers loss of finger at 2500";ConfidenceScore=0.93;ClauseRefs=@("AFLAC-ACCIDENT-6.4-DISMEMBERMENT","AFLAC-ACCIDENT-5.1-COVERED-ACCIDENT");ReqDocs=@("Surgical Report","Accident Report","Medical Records")},
    @{ClaimId="AFLAC-20260201-008";Timestamp="2026-01-19T10:00:00.000Z";PolicyNumber="AFLAC-CANCER-2024-008";ClaimAmount=5000;ClaimDescription="Chemotherapy treatment 10 sessions over 3 months for colon cancer";DecisionStatus="Covered";Explanation="Claim approved. Cancer policy pays 500 per chemotherapy treatment session. 10 sessions x 500 = 5000";ConfidenceScore=0.96;ClauseRefs=@("AFLAC-CANCER-4.6-CHEMOTHERAPY","AFLAC-CANCER-4.1-TREATMENT-BENEFIT");ReqDocs=@("Treatment Schedule","Infusion Records","Oncology Notes")},
    # 5 DENIED CLAIMS
    @{ClaimId="AFLAC-20260201-009";Timestamp="2026-01-20T10:00:00.000Z";PolicyNumber="AFLAC-CANCER-2024-003";ClaimAmount=25000;ClaimDescription="Skin cancer diagnosis basal cell carcinoma";DecisionStatus="Not Covered";Explanation="Claim denied. Policy Section 9.1 specifically excludes basal cell and squamous cell skin cancers unless metastatic. Not eligible for cancer diagnosis benefit";ConfidenceScore=0.91;ClauseRefs=@("AFLAC-CANCER-9.1-EXCLUSIONS","AFLAC-CANCER-9.3-SKIN-CANCER");ReqDocs=@("Pathology Report","Dermatology Records")},
    @{ClaimId="AFLAC-20260201-010";Timestamp="2026-01-21T10:00:00.000Z";PolicyNumber="AFLAC-HOSP-2024-019";ClaimAmount=1000;ClaimDescription="Hospital admission for elective cosmetic rhinoplasty procedure";DecisionStatus="Not Covered";Explanation="Claim denied. Policy Section 9.2 excludes hospital confinement for cosmetic or elective procedures not medically necessary";ConfidenceScore=0.97;ClauseRefs=@("AFLAC-HOSP-9.2-EXCLUSIONS","AFLAC-HOSP-9.4-COSMETIC");ReqDocs=@("Surgical Consent","Hospital Records")},
    @{ClaimId="AFLAC-20260201-011";Timestamp="2026-01-22T10:00:00.000Z";PolicyNumber="AFLAC-ACCIDENT-2024-018";ClaimAmount=800;ClaimDescription="Back injury from lifting heavy box chronic degenerative disc disease exacerbation";DecisionStatus="Not Covered";Explanation="Claim denied. Policy Section 9.5 excludes coverage for aggravation of pre-existing conditions. 5-year history of degenerative disc disease";ConfidenceScore=0.88;ClauseRefs=@("AFLAC-ACCIDENT-9.5-PREEXISTING","AFLAC-ACCIDENT-9.1-EXCLUSIONS");ReqDocs=@("Medical History","MRI Results","Physician Notes")},
    @{ClaimId="AFLAC-20260201-012";Timestamp="2026-01-23T10:00:00.000Z";PolicyNumber="AFLAC-DISABILITY-2024-027";ClaimAmount=4000;ClaimDescription="Short-term disability claim for mental health depression 8 weeks";DecisionStatus="Not Covered";Explanation="Claim denied. Policy Section 9.6 excludes disability arising from mental or nervous disorders unless hospitalized. Claimant treated as outpatient only";ConfidenceScore=0.94;ClauseRefs=@("AFLAC-DISABILITY-9.6-MENTAL-HEALTH","AFLAC-DISABILITY-9.1-EXCLUSIONS");ReqDocs=@("Psychiatrist Records","Treatment Plan")},
    @{ClaimId="AFLAC-20260201-013";Timestamp="2026-01-24T10:00:00.000Z";PolicyNumber="AFLAC-CRITICAL-2024-031";ClaimAmount=15000;ClaimDescription="Stroke claim TIA resolved within 4 hours";DecisionStatus="Not Covered";Explanation="Claim denied. Policy Section 8.3 requires neurological deficit lasting more than 24 hours for stroke coverage. TIA symptoms resolved within 4 hours";ConfidenceScore=0.92;ClauseRefs=@("AFLAC-CRITICAL-8.3-STROKE-DEFINITION","AFLAC-CRITICAL-9.2-TIA-EXCLUSION");ReqDocs=@("Neurologist Report","CT Scan","Hospital Records")},
    # 7 MANUAL REVIEW
    @{ClaimId="AFLAC-20260201-014";Timestamp="2026-01-25T10:00:00.000Z";PolicyNumber="AFLAC-HOSP-2024-025";ClaimAmount=3500;ClaimDescription="Hospital stay for 7 days admitted for observation after car accident no surgery performed";DecisionStatus="Manual Review";Explanation="Manual review required. Observation status vs formal admission needs clarification. Medical records show observation designation first 48 hours then changed to admitted status";ConfidenceScore=0.71;ClauseRefs=@("AFLAC-HOSP-2.1-CONFINEMENT","AFLAC-HOSP-2.6-OBSERVATION");ReqDocs=@("Hospital Admission Records","Billing Statements","Physician Orders")},
    @{ClaimId="AFLAC-20260201-015";Timestamp="2026-01-26T10:00:00.000Z";PolicyNumber="AFLAC-CANCER-2024-038";ClaimAmount=25000;ClaimDescription="Melanoma diagnosis in situ stage no invasion beyond epidermis";DecisionStatus="Manual Review";Explanation="Manual review required. Policy covers invasive melanoma but excludes carcinoma in situ. Pathology shows melanoma in situ with focal areas concerning for microinvasion";ConfidenceScore=0.68;ClauseRefs=@("AFLAC-CANCER-4.1-MELANOMA","AFLAC-CANCER-9.3-IN-SITU");ReqDocs=@("Pathology Report","Second Opinion","Dermatology Records")},
    @{ClaimId="AFLAC-20260201-016";Timestamp="2026-01-27T10:00:00.000Z";PolicyNumber="AFLAC-ACCIDENT-2024-041";ClaimAmount=1200;ClaimDescription="Injury during recreational football game torn ACL requiring surgery";DecisionStatus="Manual Review";Explanation="Manual review required. Policy covers accidental injuries but excludes injuries from organized sports. Game described as recreational pickup game but organized league participation unclear";ConfidenceScore=0.64;ClauseRefs=@("AFLAC-ACCIDENT-5.1-COVERED-INJURY","AFLAC-ACCIDENT-9.8-SPORTS");ReqDocs=@("Injury Report","Surgical Records","League Information")},
    @{ClaimId="AFLAC-20260201-017";Timestamp="2026-01-28T10:00:00.000Z";PolicyNumber="AFLAC-DISABILITY-2024-029";ClaimAmount=10000;ClaimDescription="Long-term disability claim for fibromyalgia 20 weeks off work";DecisionStatus="Manual Review";Explanation="Manual review required. Fibromyalgia is chronic condition with subjective symptoms. Independent medical exam needed to verify disability severity";ConfidenceScore=0.59;ClauseRefs=@("AFLAC-DISABILITY-7.2-ILLNESS","AFLAC-DISABILITY-9.6-CHRONIC");ReqDocs=@("Physician Statement","Functional Capacity Evaluation","Treatment Records")},
    @{ClaimId="AFLAC-20260201-018";Timestamp="2026-01-29T10:00:00.000Z";PolicyNumber="AFLAC-CRITICAL-2024-051";ClaimAmount=15000;ClaimDescription="Kidney failure requiring dialysis started dialysis 3 months ago ongoing treatment";DecisionStatus="Manual Review";Explanation="Manual review required. Claimant enrolled in policy 6 months ago and symptoms began 8 months ago. Pre-existing condition investigation needed to determine coverage eligibility";ConfidenceScore=0.66;ClauseRefs=@("AFLAC-CRITICAL-8.6-RENAL-FAILURE","AFLAC-CRITICAL-9.1-PREEXISTING");ReqDocs=@("Dialysis Records","Nephrology Notes","Application Date Verification")},
    @{ClaimId="AFLAC-20260201-019";Timestamp="2026-01-30T10:00:00.000Z";PolicyNumber="AFLAC-HOSP-2024-036";ClaimAmount=2500;ClaimDescription="Hospital admission for pneumonia 5 days but also received outpatient treatment week prior";DecisionStatus="Manual Review";Explanation="Manual review required. Outpatient treatment for same condition week prior raises question if admission was medically necessary vs preventable with proper outpatient care";ConfidenceScore=0.73;ClauseRefs=@("AFLAC-HOSP-2.1-CONFINEMENT","AFLAC-HOSP-2.8-MEDICAL-NECESSITY");ReqDocs=@("Hospital Records","Outpatient Treatment Records","Physician Justification")},
    @{ClaimId="AFLAC-20260201-020";Timestamp="2026-01-31T10:00:00.000Z";PolicyNumber="AFLAC-ACCIDENT-2024-034";ClaimAmount=1500;ClaimDescription="Concussion from slip and fall at home ER visit plus 2 follow-up neurologist visits";DecisionStatus="Manual Review";Explanation="Manual review required. ER benefit 350 clearly covered. However follow-up treatment benefit requires accident to be disabling. Claimant did not miss work";ConfidenceScore=0.70;ClauseRefs=@("AFLAC-ACCIDENT-5.2-ER","AFLAC-ACCIDENT-6.2-FOLLOW-UP");ReqDocs=@("ER Report","Neurologist Notes","Work Status Documentation")}
)

$successCount = 0
$failCount = 0

foreach ($claim in $claims) {
    # Build DynamoDB item structure
    $item = @{
        ClaimId = @{ S = $claim.ClaimId }
        Timestamp = @{ S = $claim.Timestamp }
        PolicyNumber = @{ S = $claim.PolicyNumber }
        ClaimAmount = @{ N = $claim.ClaimAmount.ToString() }
        ClaimDescription = @{ S = $claim.ClaimDescription }
        DecisionStatus = @{ S = $claim.DecisionStatus }
        Explanation = @{ S = $claim.Explanation }
        ConfidenceScore = @{ N = $claim.ConfidenceScore.ToString() }
        ClauseReferences = @{ L = @($claim.ClauseRefs | ForEach-Object { @{ S = $_ } }) }
        RequiredDocuments = @{ L = @($claim.ReqDocs | ForEach-Object { @{ S = $_ } }) }
        RetrievedClauses = @{ S = "[{`"ClauseId`":`"$($claim.ClauseRefs[0])`",`"Score`":0.85}]" }
    }
    
    # Convert to JSON and save to temp file
    $json = $item | ConvertTo-Json -Depth 10 -Compress
    $json | Out-File -FilePath ".\temp-claim.json" -Encoding ASCII -NoNewline
    
    # Insert into DynamoDB
    Write-Host "Inserting $($claim.ClaimId) ($($claim.DecisionStatus))..." -NoNewline
    $result = aws dynamodb put-item --table-name $TableName --region $Region --item file://temp-claim.json 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host " ✗" -ForegroundColor Red
        $failCount++
    }
    
    Start-Sleep -Milliseconds 200
}

# Clean up temp file
Remove-Item ".\temp-claim.json" -ErrorAction SilentlyContinue

# Summary
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Total Claims: 20" -ForegroundColor White
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { 'Red' } else { 'Green' })
Write-Host "==========================================" -ForegroundColor Cyan

$approved = ($claims | Where-Object { $_.DecisionStatus -eq "Covered" }).Count
$denied = ($claims | Where-Object { $_.DecisionStatus -eq "Not Covered" }).Count
$manual = ($claims | Where-Object { $_.DecisionStatus -eq "Manual Review" }).Count

Write-Host "`nBreakdown by Status:" -ForegroundColor Yellow
Write-Host "  Covered (Approved): $approved" -ForegroundColor Green
Write-Host "  Not Covered (Denied): $denied" -ForegroundColor Red
Write-Host "  Manual Review: $manual" -ForegroundColor Magenta
Write-Host ""
