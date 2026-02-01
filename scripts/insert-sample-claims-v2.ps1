# Script to insert 20 sample claim records into DynamoDB ClaimsAuditTrail table
# Covers various positive and negative scenarios for testing

param(
    [string]$Region = "us-east-1",
    [string]$Environment = "dev"
)

$tableName = "ClaimsAuditTrail-$Environment"

Write-Host "Inserting sample claims into table: $tableName in region: $Region" -ForegroundColor Green
Write-Host ""

$successCount = 0
$failCount = 0

# Function to insert a single claim
function Insert-Claim {
    param(
        [int]$Index,
        [string]$PolicyNumber,
        [decimal]$ClaimAmount,
        [string]$ClaimDescription,
        [string]$DecisionStatus,
        [string]$Explanation,
        [double]$ConfidenceScore,
        [string[]]$ClauseReferences,
        [string[]]$RequiredDocuments
    )
    
    $claimId = "CLAIM-$(Get-Date -Format 'yyyyMMdd')-$($Index.ToString('D3'))"
    $timestamp = (Get-Date).AddDays(-$Index).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    
    # Build clause references array
    $clauseList = ($ClauseReferences | ForEach-Object { "{`"S`":`"$_`"}" }) -join ","
    
    # Build required documents array
    $docsList = ($RequiredDocuments | ForEach-Object { "{`"S`":`"$_`"}" }) -join ","
    
    # Build the JSON item
    $itemJson = @"
{
    "ClaimId": {"S": "$claimId"},
    "Timestamp": {"S": "$timestamp"},
    "PolicyNumber": {"S": "$PolicyNumber"},
    "ClaimAmount": {"N": "$ClaimAmount"},
    "ClaimDescription": {"S": "$ClaimDescription"},
    "DecisionStatus": {"S": "$DecisionStatus"},
    "Explanation": {"S": "$Explanation"},
    "ConfidenceScore": {"N": "$ConfidenceScore"},
    "ClauseReferences": {"L": [$clauseList]},
    "RequiredDocuments": {"L": [$docsList]},
    "RetrievedClauses": {"S": "[{\"ClauseId\":\"$($ClauseReferences[0])\",\"Score\":0.85}]"}
}
"@
    
    Write-Host "[$Index/20] Inserting $claimId ($DecisionStatus)..." -NoNewline
    
    try {
        $result = aws dynamodb put-item --table-name $tableName --item $itemJson --region $Region 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host " ✓" -ForegroundColor Green
            return $true
        } else {
            Write-Host " ✗" -ForegroundColor Red
            Write-Host "  Error: $result" -ForegroundColor DarkRed
            return $false
        }
    }
    catch {
        Write-Host " ✗ Exception: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Insert all claims
if (Insert-Claim -Index 1 -PolicyNumber "POL-AUTO-2024-001" -ClaimAmount 2500 -ClaimDescription "Vehicle collision damage to front bumper and hood from rear-end accident on Highway 101" -DecisionStatus "Covered" -Explanation "Claim approved. Collision coverage applies. Vehicle damage from rear-end collision is covered under comprehensive auto policy Section 3.2. Damage assessment confirms repair costs within policy limits." -ConfidenceScore 0.95 -ClauseReferences @("AUTO-3.2-COLLISION", "AUTO-4.1-DEDUCTIBLE") -RequiredDocuments @("Police Report", "Repair Estimate", "Photos of Damage")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 2 -PolicyNumber "POL-HOME-2024-015" -ClaimAmount 8500 -ClaimDescription "Water damage to basement from burst pipe during winter freeze" -DecisionStatus "Covered" -Explanation "Claim approved. Sudden and accidental water damage from burst pipes is covered under homeowner's policy Section 5.3. Property damage protection applies for emergency plumbing failures." -ConfidenceScore 0.92 -ClauseReferences @("HOME-5.3-WATER-DAMAGE", "HOME-6.1-EMERGENCY-REPAIRS") -RequiredDocuments @("Plumber Invoice", "Damage Photos", "Repair Estimate")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 3 -PolicyNumber "POL-AUTO-2024-007" -ClaimAmount 1200 -ClaimDescription "Windshield replacement due to rock chip that spread into large crack" -DecisionStatus "Covered" -Explanation "Claim approved. Glass damage coverage applies under comprehensive auto policy Section 3.5. No deductible for windshield replacement as per policy terms." -ConfidenceScore 0.98 -ClauseReferences @("AUTO-3.5-GLASS-COVERAGE") -RequiredDocuments @("Glass Repair Invoice", "Before/After Photos")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 4 -PolicyNumber "POL-HEALTH-2024-022" -ClaimAmount 3200 -ClaimDescription "Emergency room visit for broken arm from bicycle accident requiring X-rays and cast" -DecisionStatus "Covered" -Explanation "Claim approved. Emergency medical services covered under health policy Section 2.1. Orthopedic treatment for accidental injury falls within covered benefits. Deductible and co-pay apply." -ConfidenceScore 0.94 -ClauseReferences @("HEALTH-2.1-EMERGENCY", "HEALTH-3.4-ORTHOPEDIC") -RequiredDocuments @("ER Report", "X-Ray Results", "Treatment Summary")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 5 -PolicyNumber "POL-HOME-2024-033" -ClaimAmount 4800 -ClaimDescription "Stolen laptop, jewelry, and electronics during home burglary" -DecisionStatus "Covered" -Explanation "Claim approved. Theft coverage applies under homeowner's policy Section 7.2. Personal property stolen during burglary is covered up to policy limits. Police report filed within required timeframe." -ConfidenceScore 0.89 -ClauseReferences @("HOME-7.2-THEFT", "HOME-8.1-PERSONAL-PROPERTY") -RequiredDocuments @("Police Report", "Itemized List", "Purchase Receipts")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 6 -PolicyNumber "POL-AUTO-2024-012" -ClaimAmount 750 -ClaimDescription "Theft of catalytic converter from parked vehicle" -DecisionStatus "Covered" -Explanation "Claim approved. Vehicle parts theft covered under comprehensive auto policy Section 3.4. Catalytic converter replacement and related repairs fall within comprehensive coverage." -ConfidenceScore 0.91 -ClauseReferences @("AUTO-3.4-THEFT", "AUTO-3.2-COMPREHENSIVE") -RequiredDocuments @("Police Report", "Repair Invoice")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 7 -PolicyNumber "POL-HEALTH-2024-045" -ClaimAmount 15000 -ClaimDescription "Surgical procedure for appendectomy with 2-day hospital stay" -DecisionStatus "Covered" -Explanation "Claim approved. Emergency surgical procedures covered under health policy Section 2.3. Appendectomy is medically necessary and performed by in-network provider. Hospital stay within policy limits." -ConfidenceScore 0.96 -ClauseReferences @("HEALTH-2.3-SURGERY", "HEALTH-4.1-HOSPITAL") -RequiredDocuments @("Hospital Bill", "Surgical Report", "Discharge Summary")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 8 -PolicyNumber "POL-HOME-2024-008" -ClaimAmount 6200 -ClaimDescription "Roof damage from hailstorm with multiple shingle replacements needed" -DecisionStatus "Covered" -Explanation "Claim approved. Weather-related damage covered under homeowner's policy Section 4.2. Hail damage to roof is a covered peril. Inspection confirms storm damage matches reported incident date." -ConfidenceScore 0.93 -ClauseReferences @("HOME-4.2-STORM-DAMAGE", "HOME-4.5-ROOF-REPAIR") -RequiredDocuments @("Weather Report", "Roof Inspection", "Contractor Estimate")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 9 -PolicyNumber "POL-AUTO-2024-003" -ClaimAmount 3200 -ClaimDescription "Vehicle damage from driving through flood waters against barricades" -DecisionStatus "Not Covered" -Explanation "Claim denied. Policy Section 9.2 excludes coverage for intentional disregard of road safety warnings. Driver knowingly drove through barricaded flood zone, constituting reckless behavior not covered by policy." -ConfidenceScore 0.88 -ClauseReferences @("AUTO-9.2-EXCLUSIONS", "AUTO-9.5-RECKLESS") -RequiredDocuments @("Police Report", "Witness Statements")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 10 -PolicyNumber "POL-HOME-2024-019" -ClaimAmount 12000 -ClaimDescription "Foundation damage from gradual settling over several years" -DecisionStatus "Not Covered" -Explanation "Claim denied. Policy Section 10.3 excludes coverage for gradual deterioration and normal wear and tear. Foundation settling is maintenance-related and not a sudden, accidental event. Pre-existing condition." -ConfidenceScore 0.91 -ClauseReferences @("HOME-10.3-EXCLUSIONS", "HOME-10.5-MAINTENANCE") -RequiredDocuments @("Structural Inspection", "Engineering Report")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 11 -PolicyNumber "POL-AUTO-2024-018" -ClaimAmount 5500 -ClaimDescription "Engine damage from driving with low oil for extended period" -DecisionStatus "Not Covered" -Explanation "Claim denied. Policy Section 9.4 excludes coverage for mechanical breakdown due to lack of maintenance. Engine failure resulted from negligent maintenance, not covered accident. No evidence of sudden failure." -ConfidenceScore 0.94 -ClauseReferences @("AUTO-9.4-MAINTENANCE", "AUTO-9.1-MECHANICAL") -RequiredDocuments @("Mechanic Report", "Service History")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 12 -PolicyNumber "POL-HEALTH-2024-031" -ClaimAmount 8900 -ClaimDescription "Cosmetic rhinoplasty procedure for aesthetic enhancement" -DecisionStatus "Not Covered" -Explanation "Claim denied. Policy Section 8.2 excludes elective cosmetic procedures not medically necessary. Rhinoplasty performed solely for aesthetic reasons without medical indication is not covered under health benefits." -ConfidenceScore 0.97 -ClauseReferences @("HEALTH-8.2-COSMETIC", "HEALTH-8.1-EXCLUSIONS") -RequiredDocuments @("Surgical Consent", "Medical Records")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 13 -PolicyNumber "POL-HOME-2024-027" -ClaimAmount 4200 -ClaimDescription "Damage to property from earthquake in California" -DecisionStatus "Not Covered" -Explanation "Claim denied. Standard homeowner's policy Section 10.1 excludes earthquake damage. Separate earthquake insurance required for this coverage. No earthquake rider on current policy." -ConfidenceScore 0.99 -ClauseReferences @("HOME-10.1-EARTHQUAKE", "HOME-10.7-ADDITIONAL-COVERAGE") -RequiredDocuments @("Damage Assessment", "Seismic Report")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 14 -PolicyNumber "POL-AUTO-2024-025" -ClaimAmount 18500 -ClaimDescription "Total loss of vehicle from fire of undetermined origin in parking garage" -DecisionStatus "Manual Review" -Explanation "Manual review required. Fire damage typically covered under comprehensive Section 3.3, but fire origin investigation incomplete. Arson investigation pending. Claim amount exceeds auto-approval threshold." -ConfidenceScore 0.72 -ClauseReferences @("AUTO-3.3-FIRE", "AUTO-9.6-INVESTIGATION") -RequiredDocuments @("Fire Marshal Report", "Police Report", "Vehicle Valuation")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 15 -PolicyNumber "POL-HEALTH-2024-038" -ClaimAmount 45000 -ClaimDescription "Experimental cancer treatment using new immunotherapy protocol" -DecisionStatus "Manual Review" -Explanation "Manual review required. Treatment may be covered under Section 2.5 if deemed medically necessary. However, experimental treatment exclusion in Section 8.4 may apply. Requires medical director review and clinical assessment." -ConfidenceScore 0.68 -ClauseReferences @("HEALTH-2.5-CANCER", "HEALTH-8.4-EXPERIMENTAL") -RequiredDocuments @("Oncologist Report", "Treatment Protocol", "Medical Necessity Letter")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 16 -PolicyNumber "POL-HOME-2024-041" -ClaimAmount 22000 -ClaimDescription "Tree fell on house during storm but tree showed signs of prior disease" -DecisionStatus "Manual Review" -Explanation "Manual review required. Storm damage generally covered under Section 4.2, but tree maintenance exclusion Section 10.4 may apply if disease was observable. Arborist report needed to determine if homeowner negligence contributed." -ConfidenceScore 0.65 -ClauseReferences @("HOME-4.2-STORM", "HOME-10.4-TREE-MAINTENANCE") -RequiredDocuments @("Arborist Report", "Storm Documentation", "Property Photos")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 17 -PolicyNumber "POL-AUTO-2024-029" -ClaimAmount 7800 -ClaimDescription "Vehicle damage from accident where driver fell asleep at wheel returning from 20-hour shift" -DecisionStatus "Manual Review" -Explanation "Manual review required. Collision coverage Section 3.2 typically applies, but policy exclusion Section 9.3 for driving under fatigue may be applicable. Circumstances suggest negligence. Requires claims adjuster investigation." -ConfidenceScore 0.61 -ClauseReferences @("AUTO-3.2-COLLISION", "AUTO-9.3-FATIGUE") -RequiredDocuments @("Police Report", "Medical Evaluation", "Employment Records")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 18 -PolicyNumber "POL-HEALTH-2024-051" -ClaimAmount 12500 -ClaimDescription "Physical therapy for chronic back pain - 60 sessions requested" -DecisionStatus "Manual Review" -Explanation "Manual review required. Physical therapy covered under Section 3.6, but requested sessions exceed typical treatment protocol. May require utilization review. Pre-authorization not obtained for extended course." -ConfidenceScore 0.70 -ClauseReferences @("HEALTH-3.6-PHYSICAL-THERAPY", "HEALTH-5.2-PREAUTH") -RequiredDocuments @("Treatment Plan", "Progress Notes", "Medical Necessity Statement")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 19 -PolicyNumber "POL-HOME-2024-036" -ClaimAmount 9500 -ClaimDescription "Mold remediation after discovering growth in walls - no recent water damage event reported" -DecisionStatus "Manual Review" -Explanation "Manual review required. Mold coverage Section 5.4 applies only when resulting from covered peril. No documented water damage event. Gradual mold growth may fall under maintenance exclusion Section 10.5. Investigation needed." -ConfidenceScore 0.59 -ClauseReferences @("HOME-5.4-MOLD", "HOME-10.5-MAINTENANCE") -RequiredDocuments @("Mold Inspection", "Remediation Estimate", "Timeline Documentation")) { $successCount++ } else { $failCount++ }
Start-Sleep -Milliseconds 200

if (Insert-Claim -Index 20 -PolicyNumber "POL-AUTO-2024-034" -ClaimAmount 4300 -ClaimDescription "Damage from pothole impact causing bent rim and suspension damage" -DecisionStatus "Manual Review" -Explanation "Manual review required. Road hazard coverage Section 3.6 may apply, but standard collision deductible could also apply per Section 4.1. Municipality liability investigation pending. Conflicting policy interpretations require adjuster review." -ConfidenceScore 0.74 -ClauseReferences @("AUTO-3.6-ROAD-HAZARD", "AUTO-4.1-DEDUCTIBLE") -RequiredDocuments @("Repair Estimate", "Road Condition Photos", "Municipality Report")) { $successCount++ } else { $failCount++ }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Insertion Summary:" -ForegroundColor Cyan
Write-Host "  Total Claims: 20" -ForegroundColor White
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Claims Breakdown:" -ForegroundColor Yellow
Write-Host "  Covered (Approved): 8" -ForegroundColor Green
Write-Host "  Not Covered (Denied): 5" -ForegroundColor Red
Write-Host "  Manual Review: 7" -ForegroundColor Magenta
Write-Host ""

if ($successCount -eq 20) {
    Write-Host "All records inserted successfully! ✓" -ForegroundColor Green
    Write-Host "You can now query the table using:" -ForegroundColor Cyan
    Write-Host "  aws dynamodb scan --table-name $tableName --region $Region" -ForegroundColor Gray
} else {
    Write-Host "Some records failed to insert. Please check AWS credentials and table name." -ForegroundColor Yellow
}
