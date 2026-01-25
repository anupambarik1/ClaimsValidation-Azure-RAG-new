# Claims RAG Bot - Test Cases & Scenarios

## Overview

This document provides comprehensive test scenarios covering positive, negative, edge cases, and business rule validations for the Claims RAG Bot system.

## Test Environment Setup

**Base URL:** `http://localhost:5184`  
**Endpoint:** `POST /api/claims/validate`  
**Content-Type:** `application/json`

---

## 1. POSITIVE TEST CASES

### 1.1 Standard Collision Claim - Auto Approved

**Scenario:** Valid motor insurance claim for collision damage within auto-approval limit

**Request:**
```json
{
  "policyNumber": "POL-2024-001",
  "claimDescription": "Car accident on Highway 101 - front bumper and headlight damage due to rear-end collision",
  "claimAmount": 2500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Collision damage is covered under comprehensive motor insurance. Amount within deductible limits.",
  "clauseReferences": ["MOT-001", "MOT-004"],
  "requiredDocuments": [
    "Police accident report",
    "Repair estimate from certified shop",
    "Photos of damage",
    "Driver's license copy"
  ],
  "confidenceScore": 0.92
}
```

**Expected HTTP Status:** `200 OK`

---

### 1.2 Minor Damage Claim - Low Amount

**Scenario:** Small claim amount for windshield repair

**Request:**
```json
{
  "policyNumber": "POL-2024-002",
  "claimDescription": "Windshield cracked by rock on highway during normal driving",
  "claimAmount": 350,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Windshield damage from road debris is covered under comprehensive coverage.",
  "clauseReferences": ["MOT-003"],
  "requiredDocuments": [
    "Photos of windshield damage",
    "Repair shop estimate"
  ],
  "confidenceScore": 0.95
}
```

**Expected HTTP Status:** `200 OK`

---

### 1.3 Theft Claim - Covered

**Scenario:** Vehicle theft claim

**Request:**
```json
{
  "policyNumber": "POL-2024-003",
  "claimDescription": "Vehicle stolen from parking lot at night. Reported to police immediately.",
  "claimAmount": 4800,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Vehicle theft is covered under comprehensive motor insurance policy.",
  "clauseReferences": ["MOT-002", "MOT-005"],
  "requiredDocuments": [
    "Police theft report",
    "Vehicle registration",
    "Keys and documentation",
    "Last service record"
  ],
  "confidenceScore": 0.89
}
```

**Expected HTTP Status:** `200 OK`

---

### 1.4 Fire Damage Claim

**Scenario:** Vehicle damaged in fire

**Request:**
```json
{
  "policyNumber": "POL-2024-004",
  "claimDescription": "Engine caught fire while driving due to electrical fault. Fire department was called.",
  "claimAmount": 4500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Fire damage from electrical fault is covered under comprehensive coverage.",
  "clauseReferences": ["MOT-002", "MOT-008"],
  "requiredDocuments": [
    "Fire department report",
    "Mechanic's assessment",
    "Photos of damage",
    "Vehicle inspection report"
  ],
  "confidenceScore": 0.91
}
```

**Expected HTTP Status:** `200 OK`

---

### 1.5 Natural Disaster - Hail Damage

**Scenario:** Vehicle damaged during hailstorm

**Request:**
```json
{
  "policyNumber": "POL-2024-005",
  "claimDescription": "Severe hailstorm caused extensive dents and broken windows throughout the vehicle",
  "claimAmount": 3200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Natural disaster damage including hail is covered under comprehensive coverage.",
  "clauseReferences": ["MOT-002", "MOT-006"],
  "requiredDocuments": [
    "Weather report for date of incident",
    "Photos of hail damage",
    "Repair estimate",
    "Insurance card"
  ],
  "confidenceScore": 0.94
}
```

**Expected HTTP Status:** `200 OK`

---

## 2. MANUAL REVIEW CASES (GUARDRAILS TRIGGERED)

### 2.1 High Amount Claim - Exceeds Auto-Approval Threshold

**Scenario:** Claim amount exceeds $5,000 threshold requiring manual review

**Request:**
```json
{
  "policyNumber": "POL-2024-006",
  "claimDescription": "Total loss - vehicle rolled over on icy road and is not repairable",
  "claimAmount": 18500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Amount $18500 exceeds auto-approval limit. Collision coverage applies for rollover damage, but manual review required due to high claim amount.",
  "clauseReferences": ["MOT-001", "MOT-009"],
  "requiredDocuments": [
    "Police accident report",
    "Vehicle valuation report",
    "Photos of total loss",
    "Tow truck receipt",
    "Title documentation"
  ],
  "confidenceScore": 0.88
}
```

**Expected HTTP Status:** `200 OK`  
**Validation:** Status should be "Manual Review" due to business rule

---

### 2.2 Low Confidence - Ambiguous Scenario

**Scenario:** Unclear circumstances that AI cannot confidently assess

**Request:**
```json
{
  "policyNumber": "POL-2024-007",
  "claimDescription": "Vehicle damage occurred while parked. Cause unknown. Found scratches and dents when returned.",
  "claimAmount": 2100,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Confidence below threshold. Unable to determine exact cause of damage. Comprehensive coverage may apply but requires investigation.",
  "clauseReferences": ["MOT-002"],
  "requiredDocuments": [
    "Photos of damage",
    "Parking receipt or evidence",
    "Witness statements if available",
    "Security camera footage"
  ],
  "confidenceScore": 0.72
}
```

**Expected HTTP Status:** `200 OK`  
**Validation:** Confidence < 0.85 triggers manual review

---

### 2.3 Multiple Incident Types

**Scenario:** Claim involving multiple types of damage

**Request:**
```json
{
  "policyNumber": "POL-2024-008",
  "claimDescription": "Vehicle hit by falling tree during storm, then hit by another car while disabled on road",
  "claimAmount": 4200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Multiple incidents in single claim (natural disaster + collision). Both appear covered but require manual assessment for proper claim splitting and deductibles.",
  "clauseReferences": ["MOT-001", "MOT-002", "MOT-006"],
  "requiredDocuments": [
    "Weather report",
    "Police report for second accident",
    "Photos of all damage",
    "Timeline of incidents",
    "Witness statements"
  ],
  "confidenceScore": 0.81
}
```

**Expected HTTP Status:** `200 OK`

---

### 2.4 Pre-existing Damage Suspected

**Scenario:** Claim where AI suspects pre-existing damage

**Request:**
```json
{
  "policyNumber": "POL-2024-009",
  "claimDescription": "Minor fender bender resulted in extensive rust damage and frame issues being discovered",
  "claimAmount": 3800,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Collision covered, but extent of damage (rust, frame issues) suggests possible pre-existing conditions. Manual inspection required.",
  "clauseReferences": ["MOT-001", "MOT-010"],
  "requiredDocuments": [
    "Police accident report",
    "Vehicle history report",
    "Previous inspection records",
    "Photos of current damage",
    "Mechanic's detailed assessment"
  ],
  "confidenceScore": 0.76
}
```

**Expected HTTP Status:** `200 OK`

---

### 2.5 Commercial Use Question

**Scenario:** Personal policy used for potential commercial purpose

**Request:**
```json
{
  "policyNumber": "POL-2024-010",
  "claimDescription": "Accident while delivering food for delivery service using personal vehicle",
  "claimAmount": 3500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Commercial use (food delivery) on personal motor policy. Policy exclusions for commercial use may apply. Requires review.",
  "clauseReferences": ["MOT-001", "MOT-EXCL-002"],
  "requiredDocuments": [
    "Police report",
    "Employment/contractor documentation",
    "Delivery app records",
    "Policy terms verification"
  ],
  "confidenceScore": 0.68
}
```

**Expected HTTP Status:** `200 OK`

---

## 3. NEGATIVE TEST CASES (NOT COVERED)

### 3.1 Intentional Damage

**Scenario:** Deliberate damage to vehicle

**Request:**
```json
{
  "policyNumber": "POL-2024-011",
  "claimDescription": "Intentionally crashed vehicle into wall due to mechanical problems and wanting replacement",
  "claimAmount": 5500,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Intentional damage explicitly excluded from all motor insurance coverage per exclusion clauses.",
  "clauseReferences": ["MOT-EXCL-001"],
  "requiredDocuments": [
    "Police report",
    "Statement of circumstances"
  ],
  "confidenceScore": 0.98
}
```

**Expected HTTP Status:** `200 OK`

---

### 3.2 DUI-Related Accident

**Scenario:** Accident while driving under influence

**Request:**
```json
{
  "policyNumber": "POL-2024-012",
  "claimDescription": "Single vehicle accident. Driver arrested for DUI at scene. Failed sobriety test.",
  "claimAmount": 4200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Accidents occurring while driver is under influence of alcohol or drugs are excluded from coverage.",
  "clauseReferences": ["MOT-EXCL-003"],
  "requiredDocuments": [
    "Police report",
    "DUI arrest record"
  ],
  "confidenceScore": 0.96
}
```

**Expected HTTP Status:** `200 OK`

---

### 3.3 Unlicensed Driver

**Scenario:** Driver without valid license at time of accident

**Request:**
```json
{
  "policyNumber": "POL-2024-013",
  "claimDescription": "Rear-end collision. Driver's license had been suspended 2 weeks prior to accident.",
  "claimAmount": 2800,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Policy requires valid driver's license at time of accident. Suspended or revoked license voids coverage.",
  "clauseReferences": ["MOT-EXCL-004", "MOT-REQ-001"],
  "requiredDocuments": [
    "Driver's license documentation",
    "DMV records",
    "Police report"
  ],
  "confidenceScore": 0.94
}
```

**Expected HTTP Status:** `200 OK`

---

### 3.4 Wear and Tear

**Scenario:** Mechanical failure from normal wear

**Request:**
```json
{
  "policyNumber": "POL-2024-014",
  "claimDescription": "Engine seized due to lack of oil changes and regular maintenance over past 3 years",
  "claimAmount": 3200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Normal wear and tear, maintenance issues, and mechanical breakdown are not covered. Motor insurance covers accidental damage, not mechanical failures.",
  "clauseReferences": ["MOT-EXCL-005"],
  "requiredDocuments": [
    "Maintenance records",
    "Mechanic's diagnosis"
  ],
  "confidenceScore": 0.97
}
```

**Expected HTTP Status:** `200 OK`

---

### 3.5 Racing or Competitive Event

**Scenario:** Damage during racing event

**Request:**
```json
{
  "policyNumber": "POL-2024-015",
  "claimDescription": "Crashed vehicle during amateur drag racing event at local track",
  "claimAmount": 6800,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Damage incurred during racing, speed contests, or competitive events is explicitly excluded from coverage.",
  "clauseReferences": ["MOT-EXCL-006"],
  "requiredDocuments": [
    "Event documentation",
    "Photos of damage"
  ],
  "confidenceScore": 0.99
}
```

**Expected HTTP Status:** `200 OK`

---

### 3.6 Uncovered Damage Type

**Scenario:** Cosmetic damage not covered

**Request:**
```json
{
  "policyNumber": "POL-2024-016",
  "claimDescription": "Paint fading and clear coat peeling due to sun exposure over years",
  "claimAmount": 1200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Not Covered",
  "explanation": "Cosmetic damage from environmental exposure and aging is not covered. Policy covers sudden and accidental damage only.",
  "clauseReferences": ["MOT-EXCL-005"],
  "requiredDocuments": [],
  "confidenceScore": 0.93
}
```

**Expected HTTP Status:** `200 OK`

---

## 4. EDGE CASES & BOUNDARY CONDITIONS

### 4.1 Exact Threshold Amount - $5000

**Scenario:** Claim at exact auto-approval threshold

**Request:**
```json
{
  "policyNumber": "POL-2024-017",
  "claimDescription": "Side-impact collision at intersection. Airbags deployed. Door and frame damage.",
  "claimAmount": 5000,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Collision coverage applies for side-impact damage with deployed airbags.",
  "clauseReferences": ["MOT-001", "MOT-004"],
  "requiredDocuments": [
    "Police accident report",
    "Repair estimate",
    "Photos of damage",
    "Airbag deployment documentation"
  ],
  "confidenceScore": 0.90
}
```

**Expected HTTP Status:** `200 OK`  
**Validation:** Amount = $5000 should NOT trigger manual review (threshold is >$5000)

---

### 4.2 One Dollar Over Threshold - $5001

**Scenario:** Claim just one dollar over auto-approval limit

**Request:**
```json
{
  "policyNumber": "POL-2024-018",
  "claimDescription": "Rear-end collision at stoplight. Moderate damage to trunk and rear bumper.",
  "claimAmount": 5001,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Amount $5001 exceeds auto-approval limit. Rear-end collision is covered under policy.",
  "clauseReferences": ["MOT-001"],
  "requiredDocuments": [
    "Police accident report",
    "Repair estimate",
    "Photos of damage"
  ],
  "confidenceScore": 0.91
}
```

**Expected HTTP Status:** `200 OK`  
**Validation:** Amount > $5000 should trigger manual review

---

### 4.3 Minimal Claim Amount - $0.01

**Scenario:** Extremely small claim

**Request:**
```json
{
  "policyNumber": "POL-2024-019",
  "claimDescription": "Tiny scratch from shopping cart in parking lot",
  "claimAmount": 0.01,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Minor parking lot damage covered, though amount is below typical deductible.",
  "clauseReferences": ["MOT-004"],
  "requiredDocuments": [
    "Photos of damage"
  ],
  "confidenceScore": 0.87
}
```

**Expected HTTP Status:** `200 OK`

---

### 4.4 Very Long Description - Token Limit Test

**Scenario:** Extremely detailed claim description

**Request:**
```json
{
  "policyNumber": "POL-2024-020",
  "claimDescription": "I was driving on Main Street at approximately 3:47 PM on Tuesday when suddenly a blue Honda Civic ran a red light at the intersection of Main and Oak. I had the green light and was proceeding through the intersection at about 25 mph when the other vehicle struck my car on the driver's side door. The impact caused my vehicle to spin approximately 180 degrees. There were multiple witnesses at the scene who can confirm the other driver ran the red light. The police arrived within 10 minutes and took statements from all parties involved. The other driver admitted fault at the scene. My airbags did not deploy but there is significant damage to the driver's door, front quarter panel, and the frame may be bent. The vehicle is not drivable and had to be towed from the scene. I was taken to the hospital as a precaution but released after examination with no serious injuries, just some bruising and soreness. The weather was clear and dry at the time of the accident.",
  "claimAmount": 4200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Other party at fault collision with witnesses. Comprehensive collision coverage applies for damage to vehicle.",
  "clauseReferences": ["MOT-001", "MOT-004"],
  "requiredDocuments": [
    "Police accident report",
    "Witness statements",
    "Medical records",
    "Photos of damage",
    "Tow truck receipt",
    "Other driver's insurance information"
  ],
  "confidenceScore": 0.94
}
```

**Expected HTTP Status:** `200 OK`

---

### 4.5 Empty Optional Field

**Scenario:** PolicyType not provided (should default to "Motor")

**Request:**
```json
{
  "policyNumber": "POL-2024-021",
  "claimDescription": "Hit pothole causing tire and rim damage",
  "claimAmount": 450
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Road hazard damage to tire and rim covered under comprehensive coverage.",
  "clauseReferences": ["MOT-002"],
  "requiredDocuments": [
    "Photos of damaged tire and rim",
    "Repair invoice",
    "Location of incident"
  ],
  "confidenceScore": 0.88
}
```

**Expected HTTP Status:** `200 OK`

---

### 4.6 Special Characters in Description

**Scenario:** Description with special characters and unicode

**Request:**
```json
{
  "policyNumber": "POL-2024-022",
  "claimDescription": "Véhicule endommagé @ parking lot - $pecial char$ & symbols: 50% damage, <front>, [bumper], {door}",
  "claimAmount": 1800,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Covered",
  "explanation": "Parking lot damage to front bumper and door covered under collision coverage.",
  "clauseReferences": ["MOT-004"],
  "requiredDocuments": [
    "Photos of damage",
    "Repair estimate"
  ],
  "confidenceScore": 0.85
}
```

**Expected HTTP Status:** `200 OK`

---

### 4.7 Maximum Realistic Claim Amount

**Scenario:** Very high value claim for luxury vehicle

**Request:**
```json
{
  "policyNumber": "POL-2024-023",
  "claimDescription": "Total loss of luxury vehicle in multi-car pile-up on freeway during foggy conditions",
  "claimAmount": 125000,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "status": "Manual Review",
  "explanation": "Amount $125000 exceeds auto-approval limit. Multi-vehicle accident in adverse weather conditions appears covered but requires manual review due to high value.",
  "clauseReferences": ["MOT-001", "MOT-009"],
  "requiredDocuments": [
    "Police accident report",
    "Vehicle valuation",
    "Photos of damage",
    "Weather report",
    "All other drivers' information",
    "Title and registration"
  ],
  "confidenceScore": 0.86
}
```

**Expected HTTP Status:** `200 OK`

---

## 5. VALIDATION ERROR TEST CASES

### 5.1 Missing Required Field - PolicyNumber

**Request:**
```json
{
  "claimDescription": "Fender bender in parking lot",
  "claimAmount": 1200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PolicyNumber": [
      "The PolicyNumber field is required."
    ]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

### 5.2 Missing Required Field - ClaimDescription

**Request:**
```json
{
  "policyNumber": "POL-2024-024",
  "claimAmount": 1200,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ClaimDescription": [
      "The ClaimDescription field is required."
    ]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

### 5.3 Invalid Claim Amount - Negative

**Request:**
```json
{
  "policyNumber": "POL-2024-025",
  "claimDescription": "Minor damage",
  "claimAmount": -100,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ClaimAmount": [
      "The field ClaimAmount must be between 0.01 and 1.7976931348623157E+308."
    ]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

### 5.4 Invalid Claim Amount - Zero

**Request:**
```json
{
  "policyNumber": "POL-2024-026",
  "claimDescription": "No damage claim",
  "claimAmount": 0,
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ClaimAmount": [
      "The field ClaimAmount must be between 0.01 and 1.7976931348623157E+308."
    ]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

### 5.5 Invalid JSON Format

**Request:**
```json
{
  "policyNumber": "POL-2024-027",
  "claimDescription": "Test claim",
  "claimAmount": "not a number",
  "policyType": "Motor"
}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "$.claimAmount": [
      "The JSON value could not be converted to System.Decimal."
    ]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

### 5.6 Empty Request Body

**Request:**
```json
{}
```

**Expected Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PolicyNumber": ["The PolicyNumber field is required."],
    "ClaimDescription": ["The ClaimDescription field is required."],
    "ClaimAmount": ["The ClaimAmount field is required."]
  }
}
```

**Expected HTTP Status:** `400 Bad Request`

---

## 6. BUSINESS RULE VALIDATION TESTS

### 6.1 Confidence Threshold - Exactly 0.85

**Scenario:** AI confidence exactly at threshold (should NOT trigger manual review)

**Mock Response Needed:** Configure to return exactly 0.85 confidence

**Expected Result:** Status = "Covered" (if claim is valid)

---

### 6.2 Confidence Threshold - 0.84

**Scenario:** AI confidence just below threshold (should trigger manual review)

**Mock Response Needed:** Configure to return 0.84 confidence

**Expected Result:** Status = "Manual Review" with explanation about confidence

---

### 6.3 Multiple Business Rules Triggered

**Scenario:** High amount + low confidence + potential exclusion

**Request:**
```json
{
  "policyNumber": "POL-2024-028",
  "claimDescription": "Unclear circumstances. Vehicle found damaged. Amount claimed for total replacement.",
  "claimAmount": 15000,
  "policyType": "Motor"
}
```

**Expected Result:** Status = "Manual Review" (multiple guardrails triggered)

---

## 7. PERFORMANCE & LOAD TESTS

### 7.1 Concurrent Requests

**Test:** Send 10 identical requests simultaneously

**Request:**
```json
{
  "policyNumber": "POL-LOAD-001",
  "claimDescription": "Standard collision test",
  "claimAmount": 2000,
  "policyType": "Motor"
}
```

**Expected:** All requests should complete successfully within 10 seconds

---

### 7.2 Large Batch Sequential

**Test:** Send 50 requests sequentially with varying scenarios

**Expected:** 
- No degradation in response time
- All responses consistent
- No memory leaks

---

## 8. INTEGRATION TESTS

### 8.1 AWS Bedrock Unavailable

**Scenario:** Bedrock service down or credentials invalid

**Expected:** HTTP 500 with detailed error message about Bedrock connectivity

---

### 8.2 DynamoDB Unavailable

**Scenario:** DynamoDB table not accessible

**Expected:** Claim should still process, but audit may fail (logged error, but request succeeds)

---

### 8.3 OpenSearch Unavailable

**Scenario:** OpenSearch endpoint unreachable

**Expected:** System should fall back to mock data and process claim

---

## 9. SECURITY TESTS

### 9.1 SQL Injection Attempt

**Request:**
```json
{
  "policyNumber": "POL'; DROP TABLE Claims; --",
  "claimDescription": "Test claim' OR '1'='1",
  "claimAmount": 1000,
  "policyType": "Motor"
}
```

**Expected:** Request processed safely, no SQL injection (system doesn't use SQL anyway)

---

### 9.2 XSS Attempt in Description

**Request:**
```json
{
  "policyNumber": "POL-2024-029",
  "claimDescription": "<script>alert('XSS')</script> Collision damage",
  "claimAmount": 1500,
  "policyType": "Motor"
}
```

**Expected:** Script tags handled safely in response

---

### 9.3 Very Large Payload

**Request:** JSON with claimDescription > 10,000 characters

**Expected:** Either processed successfully or rejected with appropriate error

---

## 10. AUDIT TRAIL VERIFICATION

### 10.1 Verify DynamoDB Record Created

**Test Steps:**
1. Submit any valid claim
2. Query DynamoDB table `ClaimsAuditTrail`
3. Verify record exists with all fields populated

**Expected Fields:**
- ClaimId (UUID)
- Timestamp (ISO 8601)
- PolicyNumber, ClaimAmount, ClaimDescription
- DecisionStatus, Explanation, ConfidenceScore
- ClauseReferences, RequiredDocuments
- RetrievedClauses

---

## Test Execution Guide

### Using Swagger UI

1. Navigate to `http://localhost:5184/swagger`
2. Expand `POST /api/claims/validate`
3. Click "Try it out"
4. Paste test case JSON
5. Click "Execute"
6. Verify response matches expected output

### Using PowerShell

```powershell
$body = @{
    policyNumber = "POL-2024-001"
    claimDescription = "Test claim description"
    claimAmount = 2500
    policyType = "Motor"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5184/api/claims/validate" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

### Using curl

```bash
curl -X POST http://localhost:5184/api/claims/validate \
  -H "Content-Type: application/json" \
  -d '{
    "policyNumber": "POL-2024-001",
    "claimDescription": "Front bumper damage from collision",
    "claimAmount": 2500,
    "policyType": "Motor"
  }'
```

### Automated Testing Script

```powershell
# Run all positive test cases
$testCases = @(
    @{name="Collision"; amount=2500; desc="Front bumper collision damage"},
    @{name="Theft"; amount=4800; desc="Vehicle stolen from parking lot"},
    @{name="Fire"; amount=4500; desc="Engine fire from electrical fault"}
)

foreach ($test in $testCases) {
    Write-Host "Testing: $($test.name)" -ForegroundColor Cyan
    $body = @{
        policyNumber = "POL-TEST-$(Get-Random)"
        claimDescription = $test.desc
        claimAmount = $test.amount
        policyType = "Motor"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "http://localhost:5184/api/claims/validate" `
        -Method Post -Body $body -ContentType "application/json"
    
    Write-Host "Status: $($response.status)" -ForegroundColor Green
    Write-Host "Confidence: $($response.confidenceScore)" -ForegroundColor Yellow
    Write-Host "---"
}
```

## Test Coverage Summary

| Category | Test Cases | Coverage |
|----------|-----------|----------|
| Positive (Covered) | 5 | Standard claims that should be approved |
| Manual Review | 5 | Guardrails and ambiguous scenarios |
| Negative (Not Covered) | 6 | Exclusions and denied claims |
| Edge Cases | 7 | Boundary conditions and limits |
| Validation Errors | 6 | Invalid input handling |
| Business Rules | 3 | Threshold and rule validation |
| Performance | 2 | Load and concurrency |
| Integration | 3 | AWS service failures |
| Security | 3 | Injection and XSS attempts |
| Audit | 1 | Compliance verification |
| **TOTAL** | **41** | **Comprehensive coverage** |

## Expected System Behavior

✅ **Covered Claims:** Clear approval with clause references  
⚠️ **Manual Review:** Triggered by amount > $5000, confidence < 0.85, or ambiguous scenarios  
❌ **Not Covered:** Clear denial with exclusion clause references  
🔴 **Validation Errors:** HTTP 400 with specific error messages  
🔧 **System Errors:** HTTP 500 with detailed error information  

---

**Document Version:** 1.0  
**Last Updated:** January 2026  
**Maintained By:** QA Team
