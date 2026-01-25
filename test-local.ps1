# Test Claims RAG Bot locally (without AWS)
# This PowerShell script tests the API with sample claims

Write-Host "Starting Claims RAG Bot Tests..." -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:5001"

# Test 1: Health Check
Write-Host "Test 1: Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/claims/health" -Method Get -SkipCertificateCheck
    Write-Host "✓ Health check passed" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json)
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Motor - Collision (Should be Covered)
Write-Host "Test 2: Motor Insurance - Collision Coverage" -ForegroundColor Yellow
$claim1 = @{
    policyNumber = "POL-12345"
    claimDescription = "Car accident - front bumper damage from collision with another vehicle"
    claimAmount = 2500
    policyType = "Motor"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/claims/validate" -Method Post -Body $claim1 -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Status: $($response.status)" -ForegroundColor Green
    Write-Host "  Confidence: $($response.confidenceScore)"
    Write-Host "  Explanation: $($response.explanation)"
} catch {
    Write-Host "✗ Test failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Motor - High Amount (Should be Manual Review)
Write-Host "Test 3: Motor Insurance - High Amount Claim" -ForegroundColor Yellow
$claim2 = @{
    policyNumber = "POL-22222"
    claimDescription = "Total loss from major collision"
    claimAmount = 45000
    policyType = "Motor"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/claims/validate" -Method Post -Body $claim2 -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Status: $($response.status)" -ForegroundColor Green
    Write-Host "  Confidence: $($response.confidenceScore)"
    Write-Host "  Explanation: $($response.explanation)"
} catch {
    Write-Host "✗ Test failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Motor - Racing (Should be Denied)
Write-Host "Test 4: Motor Insurance - Racing (Exclusion)" -ForegroundColor Yellow
$claim3 = @{
    policyNumber = "POL-11111"
    claimDescription = "Damage from racing event at local track"
    claimAmount = 8000
    policyType = "Motor"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/claims/validate" -Method Post -Body $claim3 -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Status: $($response.status)" -ForegroundColor Green
    Write-Host "  Confidence: $($response.confidenceScore)"
    Write-Host "  Explanation: $($response.explanation)"
} catch {
    Write-Host "✗ Test failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Health Insurance
Write-Host "Test 5: Health Insurance - Hospital Confinement" -ForegroundColor Yellow
$claim4 = @{
    policyNumber = "HLT-12345"
    claimDescription = "Hospitalized for 5 days due to appendicitis surgery"
    claimAmount = 1000
    policyType = "Health"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/claims/validate" -Method Post -Body $claim4 -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Status: $($response.status)" -ForegroundColor Green
    Write-Host "  Confidence: $($response.confidenceScore)"
    Write-Host "  Explanation: $($response.explanation)"
} catch {
    Write-Host "✗ Test failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "Tests completed!" -ForegroundColor Cyan
