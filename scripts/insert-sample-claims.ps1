# Script to insert 20 sample AFLAC health insurance claim records into DynamoDB ClaimsAuditTrail table
# Covers various positive and negative scenarios for Aflac supplemental health insurance

param(
    [string]$Region = "us-east-1",
    [string]$TableName = "ClaimsAuditTrail",
    [string]$Environment = "dev"
)

$tableName = if ($Environment -eq "dev") { "ClaimsAuditTrail-dev" } else { $TableName }

Write-Host "Inserting sample Aflac health insurance claims into table: $tableName in region: $Region" -ForegroundColor Green

# Sample Aflac claim data covering various scenarios
$claims = @(
    # APPROVED CLAIMS (Status: Covered)
    @{
        PolicyNumber = "AFLAC-HOSP-2024-001"
        ClaimAmount = 1500
        ClaimDescription = "Hospital confinement for 3 days due to pneumonia diagnosis and treatment"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Hospital Indemnity benefit applies at $500/day for confined hospital stay. Policy Section 2.1 covers pneumonia as eligible condition. Total benefit: 3 days × $500 = $1,500."
        ConfidenceScore = 0.96
        ClauseReferences = @("AFLAC-HOSP-2.1-CONFINEMENT", "AFLAC-HOSP-3.2-DAILY-BENEFIT")
        RequiredDocuments = @("Hospital Admission Record", "Discharge Summary", "Physician Statement")
    },
    @{
        PolicyNumber = "AFLAC-CANCER-2024-015"
        ClaimAmount = 25000
        ClaimDescription = "Initial diagnosis of breast cancer - Stage 2"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Cancer policy provides lump sum benefit of $25,000 upon first diagnosis of internal cancer. Policy Section 4.1 covers breast cancer diagnosis. Pathology report confirms malignancy."
        ConfidenceScore = 0.98
        ClauseReferences = @("AFLAC-CANCER-4.1-INITIAL-DIAGNOSIS", "AFLAC-CANCER-4.3-INTERNAL-CANCER")
        RequiredDocuments = @("Pathology Report", "Oncologist Diagnosis", "Biopsy Results")
    },
    @{
        PolicyNumber = "AFLAC-ACCIDENT-2024-007"
        ClaimAmount = 750
        ClaimDescription = "Emergency room treatment for fractured wrist from fall at workplace"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Accident policy covers ER treatment ($350) plus fracture benefit ($400). Policy Section 5.2 covers accidental falls and resulting injuries. Total benefit: $750."
        ConfidenceScore = 0.94
        ClauseReferences = @("AFLAC-ACCIDENT-5.2-ER-TREATMENT", "AFLAC-ACCIDENT-6.1-FRACTURE")
        RequiredDocuments = @("ER Report", "X-Ray Results", "Treatment Records")
    },
    @{
        PolicyNumber = "AFLAC-DISABILITY-2024-022"
        ClaimAmount = 6000
        ClaimDescription = "Short-term disability for 12 weeks following knee surgery and recovery"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Short-term disability pays $500/week for medically necessary surgery recovery. Policy Section 7.1 covers orthopedic surgery. 12 weeks × $500 = $6,000. Physician confirms inability to work."
        ConfidenceScore = 0.92
        ClauseReferences = @("AFLAC-DISABILITY-7.1-SURGERY", "AFLAC-DISABILITY-7.3-WEEKLY-BENEFIT")
        RequiredDocuments = @("Surgical Report", "Physician Disability Statement", "Recovery Timeline")
    },
    @{
        PolicyNumber = "AFLAC-CRITICAL-2024-033"
        ClaimAmount = 15000
        ClaimDescription = "Heart attack requiring emergency angioplasty and ICU admission"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Critical Illness policy provides $15,000 lump sum for first heart attack. Policy Section 8.2 covers myocardial infarction with supporting cardiac enzyme tests and ECG findings."
        ConfidenceScore = 0.97
        ClauseReferences = @("AFLAC-CRITICAL-8.2-HEART-ATTACK", "AFLAC-CRITICAL-8.5-FIRST-OCCURRENCE")
        RequiredDocuments = @("Cardiac Enzyme Results", "ECG Report", "Hospital Records")
    },
    @{
        PolicyNumber = "AFLAC-HOSP-2024-041"
        ClaimAmount = 2000
        ClaimDescription = "ICU admission for 2 days following severe allergic reaction and anaphylaxis"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Hospital policy pays enhanced ICU benefit at $1,000/day. Policy Section 2.4 covers intensive care unit confinement. 2 days × $1,000 = $2,000."
        ConfidenceScore = 0.95
        ClauseReferences = @("AFLAC-HOSP-2.4-ICU-BENEFIT", "AFLAC-HOSP-2.1-CONFINEMENT")
        RequiredDocuments = @("ICU Records", "Physician Notes", "Medication Administration Records")
    },
    @{
        PolicyNumber = "AFLAC-ACCIDENT-2024-012"
        ClaimAmount = 2500
        ClaimDescription = "Accidental dismemberment - loss of index finger in machinery accident"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Accident policy provides dismemberment benefit. Policy Section 6.4 covers loss of finger at $2,500. Workplace accident report filed. Surgical amputation confirmed by medical records."
        ConfidenceScore = 0.93
        ClauseReferences = @("AFLAC-ACCIDENT-6.4-DISMEMBERMENT", "AFLAC-ACCIDENT-5.1-COVERED-ACCIDENT")
        RequiredDocuments = @("Surgical Report", "Accident Report", "Medical Records")
    },
    @{
        PolicyNumber = "AFLAC-CANCER-2024-008"
        ClaimAmount = 5000
        ClaimDescription = "Chemotherapy treatment - 10 sessions over 3 months for colon cancer"
        DecisionStatus = "Covered"
        Explanation = "Claim approved. Cancer policy pays $500 per chemotherapy treatment session. Policy Section 4.6 covers chemotherapy administration. 10 sessions × $500 = $5,000."
        ConfidenceScore = 0.96
        ClauseReferences = @("AFLAC-CANCER-4.6-CHEMOTHERAPY", "AFLAC-CANCER-4.1-TREATMENT-BENEFIT")
        RequiredDocuments = @("Treatment Schedule", "Infusion Records", "Oncology Notes")
    },

    # DENIED CLAIMS (Status: Not Covered)
    @{
        PolicyNumber = "AFLAC-CANCER-2024-003"
        ClaimAmount = 25000
        ClaimDescription = "Skin cancer diagnosis - basal cell carcinoma"
        DecisionStatus = "Not Covered"
        Explanation = "Claim denied. Policy Section 9.1 specifically excludes basal cell and squamous cell skin cancers unless metastatic. Pathology confirms non-metastatic basal cell carcinoma. Not eligible for cancer diagnosis benefit."
        ConfidenceScore = 0.91
        ClauseReferences = @("AFLAC-CANCER-9.1-EXCLUSIONS", "AFLAC-CANCER-9.3-SKIN-CANCER")
        RequiredDocuments = @("Pathology Report", "Dermatology Records")
    },
    @{
        PolicyNumber = "AFLAC-HOSP-2024-019"
        ClaimAmount = 1000
        ClaimDescription = "Hospital admission for elective cosmetic rhinoplasty procedure"
        DecisionStatus = "Not Covered"
        Explanation = "Claim denied. Policy Section 9.2 excludes hospital confinement for cosmetic or elective procedures not medically necessary. Rhinoplasty performed for aesthetic purposes only. No medical necessity documented."
        ConfidenceScore = 0.97
        ClauseReferences = @("AFLAC-HOSP-9.2-EXCLUSIONS", "AFLAC-HOSP-9.4-COSMETIC")
        RequiredDocuments = @("Surgical Consent", "Hospital Records")
    },
    @{
        PolicyNumber = "AFLAC-ACCIDENT-2024-018"
        ClaimAmount = 800
        ClaimDescription = "Back injury from lifting heavy box - chronic degenerative disc disease exacerbation"
        DecisionStatus = "Not Covered"
        Explanation = "Claim denied. Policy Section 9.5 excludes coverage for aggravation of pre-existing conditions. Medical records show 5-year history of degenerative disc disease. Injury is exacerbation of pre-existing condition, not covered accident."
        ConfidenceScore = 0.88
        ClauseReferences = @("AFLAC-ACCIDENT-9.5-PREEXISTING", "AFLAC-ACCIDENT-9.1-EXCLUSIONS")
        RequiredDocuments = @("Medical History", "MRI Results", "Physician Notes")
    },
    @{
        PolicyNumber = "AFLAC-DISABILITY-2024-027"
        ClaimAmount = 4000
        ClaimDescription = "Short-term disability claim for mental health depression - 8 weeks"
        DecisionStatus = "Not Covered"
        Explanation = "Claim denied. Policy Section 9.6 excludes disability arising from mental or nervous disorders unless hospitalized. Claimant treated as outpatient only. No inpatient psychiatric hospitalization occurred."
        ConfidenceScore = 0.94
        ClauseReferences = @("AFLAC-DISABILITY-9.6-MENTAL-HEALTH", "AFLAC-DISABILITY-9.1-EXCLUSIONS")
        RequiredDocuments = @("Psychiatrist Records", "Treatment Plan")
    },
    @{
        PolicyNumber = "AFLAC-CRITICAL-2024-031"
        ClaimAmount = 15000
        ClaimDescription = "Stroke claim - TIA (Transient Ischemic Attack) resolved within 4 hours"
        DecisionStatus = "Not Covered"
        Explanation = "Claim denied. Policy Section 8.3 requires neurological deficit lasting more than 24 hours for stroke coverage. TIA symptoms resolved within 4 hours. Does not meet policy definition of covered stroke event."
        ConfidenceScore = 0.92
        ClauseReferences = @("AFLAC-CRITICAL-8.3-STROKE-DEFINITION", "AFLAC-CRITICAL-9.2-TIA-EXCLUSION")
        RequiredDocuments = @("Neurologist Report", "CT Scan", "Hospital Records")
    },

    # MANUAL REVIEW REQUIRED
    @{
        PolicyNumber = "AFLAC-HOSP-2024-025"
        ClaimAmount = 3500
        ClaimDescription = "Hospital stay for 7 days - admitted for observation after car accident, no surgery performed"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy Section 2.1 covers hospital confinement, but observation status vs. formal admission needs clarification. Medical records show 'observation' designation first 48 hours, then changed to admitted status. Requires claim adjuster review."
        ConfidenceScore = 0.71
        ClauseReferences = @("AFLAC-HOSP-2.1-CONFINEMENT", "AFLAC-HOSP-2.6-OBSERVATION")
        RequiredDocuments = @("Hospital Admission Records", "Billing Statements", "Physician Orders")
    },
    @{
        PolicyNumber = "AFLAC-CANCER-2024-038"
        ClaimAmount = 25000
        ClaimDescription = "Melanoma diagnosis - in situ stage, no invasion beyond epidermis"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy covers invasive melanoma per Section 4.1, but excludes carcinoma in situ per Section 9.3. Pathology shows melanoma in situ with focal areas concerning for microinvasion. Requires medical director review of pathology."
        ConfidenceScore = 0.68
        ClauseReferences = @("AFLAC-CANCER-4.1-MELANOMA", "AFLAC-CANCER-9.3-IN-SITU")
        RequiredDocuments = @("Pathology Report", "Second Opinion", "Dermatology Records")
    },
    @{
        PolicyNumber = "AFLAC-ACCIDENT-2024-041"
        ClaimAmount = 1200
        ClaimDescription = "Injury during recreational football game - torn ACL requiring surgery"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy Section 5.1 covers accidental injuries, but Section 9.8 excludes injuries from organized sports. Game described as 'recreational pickup game' but organized league participation unclear. Requires investigation."
        ConfidenceScore = 0.64
        ClauseReferences = @("AFLAC-ACCIDENT-5.1-COVERED-INJURY", "AFLAC-ACCIDENT-9.8-SPORTS")
        RequiredDocuments = @("Injury Report", "Surgical Records", "League Information")
    },
    @{
        PolicyNumber = "AFLAC-DISABILITY-2024-029"
        ClaimAmount = 10000
        ClaimDescription = "Long-term disability claim for fibromyalgia - 20 weeks off work"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy Section 7.2 covers disability from illness, but fibromyalgia is chronic condition with subjective symptoms. Section 9.6 mental/nervous exclusion may partially apply. Independent medical exam needed to verify disability severity."
        ConfidenceScore = 0.59
        ClauseReferences = @("AFLAC-DISABILITY-7.2-ILLNESS", "AFLAC-DISABILITY-9.6-CHRONIC")
        RequiredDocuments = @("Physician Statement", "Functional Capacity Evaluation", "Treatment Records")
    },
    @{
        PolicyNumber = "AFLAC-CRITICAL-2024-051"
        ClaimAmount = 15000
        ClaimDescription = "Kidney failure requiring dialysis - started dialysis 3 months ago, ongoing treatment"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy Section 8.6 covers end-stage renal failure requiring dialysis. However, claimant enrolled in policy 6 months ago and symptoms began 8 months ago. Pre-existing condition investigation needed to determine coverage eligibility."
        ConfidenceScore = 0.66
        ClauseReferences = @("AFLAC-CRITICAL-8.6-RENAL-FAILURE", "AFLAC-CRITICAL-9.1-PREEXISTING")
        RequiredDocuments = @("Dialysis Records", "Nephrology Notes", "Application Date Verification")
    },
    @{
        PolicyNumber = "AFLAC-HOSP-2024-036"
        ClaimAmount = 2500
        ClaimDescription = "Hospital admission for pneumonia - 5 days, but also received outpatient treatment week prior"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. Policy Section 2.1 covers hospital confinement at $500/day. However, outpatient treatment for same condition week prior raises question if admission was medically necessary vs. preventable with proper outpatient care. Medical necessity review needed."
        ConfidenceScore = 0.73
        ClauseReferences = @("AFLAC-HOSP-2.1-CONFINEMENT", "AFLAC-HOSP-2.8-MEDICAL-NECESSITY")
        RequiredDocuments = @("Hospital Records", "Outpatient Treatment Records", "Physician Justification")
    },
    @{
        PolicyNumber = "AFLAC-ACCIDENT-2024-034"
        ClaimAmount = 1500
        ClaimDescription = "Concussion from slip and fall at home - ER visit plus 2 follow-up neurologist visits"
        DecisionStatus = "Manual Review"
        Explanation = "Manual review required. ER benefit ($350) clearly covered under Section 5.2. However, follow-up treatment benefit requires accident to be 'disabling' per Section 6.2. Claimant did not miss work. Determination needed if follow-ups qualify for additional benefits beyond ER."
        ConfidenceScore = 0.70
        ClauseReferences = @("AFLAC-ACCIDENT-5.2-ER", "AFLAC-ACCIDENT-6.2-FOLLOW-UP")
        RequiredDocuments = @("ER Report", "Neurologist Notes", "Work Status Documentation")
    }
)

Write-Host "`nPreparing to insert $($claims.Count) claim records..." -ForegroundColor Cyan

$successCount = 0
$failCount = 0

foreach ($i in 0..($claims.Count - 1)) {
    $claim = $claims[$i]
    $claimId = "CLAIM-$(Get-Date -Format 'yyyyMMdd')-$($i.ToString('D3'))"
    $timestamp = (Get-Date).AddDays(-($claims.Count - $i)).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    
    # Build the DynamoDB item JSON
    $clauseRefsJson = $claim.ClauseReferences | ForEach-Object { @{ S = $_ } }
    $requiredDocsJson = $claim.RequiredDocuments | ForEach-Object { @{ S = $_ } }
    
    $itemObj = @{
        ClaimId = @{ S = $claimId }
        Timestamp = @{ S = $timestamp }
        PolicyNumber = @{ S = $claim.PolicyNumber }
        ClaimAmount = @{ N = $claim.ClaimAmount.ToString() }
        ClaimDescription = @{ S = $claim.ClaimDescription }
        DecisionStatus = @{ S = $claim.DecisionStatus }
        Explanation = @{ S = $claim.Explanation }
        ConfidenceScore = @{ N = $claim.ConfidenceScore.ToString() }
        ClauseReferences = @{ L = $clauseRefsJson }
        RequiredDocuments = @{ L = $requiredDocsJson }
        RetrievedClauses = @{ S = "[{`"ClauseId`":`"$($claim.ClauseReferences[0])`",`"Score`":0.85}]" }
    }
    
    $item = $itemObj | ConvertTo-Json -Depth 10 -Compress

    try {
        Write-Host "[$($i+1)/$($claims.Count)] Inserting $claimId ($($claim.DecisionStatus))..." -NoNewline
        
        aws dynamodb put-item `
            --table-name $tableName `
            --item $item `
            --region $Region `
            2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host " ✓" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host " ✗ Failed" -ForegroundColor Red
            $failCount++
        }
    }
    catch {
        Write-Host " ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
    
    Start-Sleep -Milliseconds 200  # Rate limiting
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Insertion Summary:" -ForegroundColor Cyan
Write-Host "  Total Claims: $($claims.Count)" -ForegroundColor White
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "========================================`n" -ForegroundColor Cyan

# Summary by status
$approvedCount = ($claims | Where-Object { $_.DecisionStatus -eq "Covered" }).Count
$deniedCount = ($claims | Where-Object { $_.DecisionStatus -eq "Not Covered" }).Count
$manualCount = ($claims | Where-Object { $_.DecisionStatus -eq "Manual Review" }).Count

Write-Host "Claims Breakdown:" -ForegroundColor Yellow
Write-Host "  Covered (Approved): $approvedCount" -ForegroundColor Green
Write-Host "  Not Covered (Denied): $deniedCount" -ForegroundColor Red
Write-Host "  Manual Review: $manualCount" -ForegroundColor Magenta
Write-Host ""

if ($successCount -eq $claims.Count) {
    Write-Host "All records inserted successfully! ✓" -ForegroundColor Green
    Write-Host "You can now query the table using:" -ForegroundColor Cyan
    Write-Host "  aws dynamodb scan --table-name $tableName --region $Region" -ForegroundColor Gray
} else {
    Write-Host "Some records failed to insert. Please check AWS credentials and table name." -ForegroundColor Yellow
}
