# Quick script to create blob-metadata container using REST API

$config = Get-Content src/ClaimsRagBot.Api/appsettings.json -Raw | ConvertFrom-Json
$endpoint = $config.Azure.CosmosDB.Endpoint
$key = $config.Azure.CosmosDB.Key
$databaseId = $config.Azure.CosmosDB.DatabaseId

Write-Host "Creating blob-metadata container in Cosmos DB..." -ForegroundColor Cyan
Write-Host "  Endpoint: $endpoint" -ForegroundColor Gray
Write-Host "  Database: $databaseId" -ForegroundColor Gray

# Container definition
$containerBody = @{
    id = "blob-metadata"
    partitionKey = @{
        paths = @("/DocumentId")
        kind = "Hash"
    }
} | ConvertTo-Json

$uri = "$endpoint/dbs/$databaseId/colls"
$date = Get-Date -Format "r"
$verb = "POST"
$resourceType = "colls"
$resourceLink = "dbs/$databaseId"

# Generate auth token
function Get-CosmosAuthToken {
    param($verb, $resourceType, $resourceLink, $date, $key)
    
    $keyBytes = [System.Convert]::FromBase64String($key)
    $text = @($verb.ToLowerInvariant() + "`n" + $resourceType.ToLowerInvariant() + "`n" + $resourceLink + "`n" + $date.ToLowerInvariant() + "`n" + "" + "`n")
    $body = [Text.Encoding]::UTF8.GetBytes($text)
    $hmacsha = New-Object System.Security.Cryptography.HMACSHA256
    $hmacsha.Key = $keyBytes
    $hash = $hmacsha.ComputeHash($body)
    $signature = [Convert]::ToBase64String($hash)
    
    [System.Web.HttpUtility]::UrlEncode("type=master&ver=1.0&sig=$signature")
}

Add-Type -AssemblyName System.Web

$authToken = Get-CosmosAuthToken -verb $verb -resourceType $resourceType -resourceLink $resourceLink -date $date -key $key

$headers = @{
    "Authorization" = $authToken
    "x-ms-date" = $date
    "x-ms-version" = "2018-12-31"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri $uri -Method $verb -Headers $headers -Body $containerBody -ErrorAction Stop
    Write-Host "✅ Container 'blob-metadata' created successfully!" -ForegroundColor Green
    Write-Host "   Partition key: /DocumentId" -ForegroundColor Gray
}
catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "✅ Container 'blob-metadata' already exists" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Error creating container: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        
        # Try to read error details
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "   Details: $responseBody" -ForegroundColor Red
    }
}
