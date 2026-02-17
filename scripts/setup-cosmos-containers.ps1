# Azure Cosmos DB Setup Script
# Creates the blob-metadata container for storing document ID to blob name mappings

param(
    [string]$CosmosEndpoint = "https://YOUR_COSMOS_NAME.documents.azure.com:443/",
    [string]$CosmosKey = "YOUR_PRIMARY_KEY",
    [string]$DatabaseName = "ClaimsRagBot"
)

Write-Host "🔧 Azure Cosmos DB Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if Az.CosmosDB module is installed
if (-not (Get-Module -ListAvailable -Name Az.CosmosDB)) {
    Write-Host "Installing Az.CosmosDB module..." -ForegroundColor Yellow
    Install-Module -Name Az.CosmosDB -Force -AllowClobber -Scope CurrentUser
}

Import-Module Az.CosmosDB

# Parse account name from endpoint
$accountName = ($CosmosEndpoint -split '\.')[0].Replace('https://', '')

Write-Host "`n📋 Configuration:" -ForegroundColor Green
Write-Host "  Account: $accountName"
Write-Host "  Database: $DatabaseName"

# Connect to Azure (if not already connected)
$context = Get-AzContext -ErrorAction SilentlyContinue
if (-not $context) {
    Write-Host "`n🔐 Connecting to Azure..." -ForegroundColor Yellow
    Connect-AzAccount
}

# Get resource group
Write-Host "`n🔍 Finding Cosmos DB account..." -ForegroundColor Yellow
$cosmosAccount = Get-AzCosmosDBAccount | Where-Object { $_.Name -eq $accountName }

if (-not $cosmosAccount) {
    Write-Host "❌ ERROR: Cosmos DB account '$accountName' not found" -ForegroundColor Red
    Write-Host "   Available accounts:" -ForegroundColor Yellow
    Get-AzCosmosDBAccount | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Yellow }
    exit 1
}

$resourceGroup = $cosmosAccount.ResourceGroupName
Write-Host "✅ Found account in resource group: $resourceGroup" -ForegroundColor Green

# Create database if it doesn't exist
Write-Host "`n📦 Creating database '$DatabaseName'..." -ForegroundColor Yellow
try {
    $database = Get-AzCosmosDBSqlDatabase -ResourceGroupName $resourceGroup -AccountName $accountName -Name $DatabaseName -ErrorAction SilentlyContinue
    if ($database) {
        Write-Host "✅ Database already exists" -ForegroundColor Green
    } else {
        New-AzCosmosDBSqlDatabase -ResourceGroupName $resourceGroup -AccountName $accountName -Name $DatabaseName
        Write-Host "✅ Database created" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Error creating database: $_" -ForegroundColor Red
    exit 1
}

# Create blob-metadata container
Write-Host "`n📦 Creating 'blob-metadata' container..." -ForegroundColor Yellow
try {
    $container = Get-AzCosmosDBSqlContainer -ResourceGroupName $resourceGroup -AccountName $accountName -DatabaseName $DatabaseName -Name "blob-metadata" -ErrorAction SilentlyContinue
    if ($container) {
        Write-Host "✅ Container 'blob-metadata' already exists" -ForegroundColor Green
    } else {
        # Create partition key path for DocumentId
        $partitionKey = New-AzCosmosDBSqlContainerPartitionKey -Path "/DocumentId"
        
        # Create container with serverless (no throughput needed)
        New-AzCosmosDBSqlContainer `
            -ResourceGroupName $resourceGroup `
            -AccountName $accountName `
            -DatabaseName $DatabaseName `
            -Name "blob-metadata" `
            -PartitionKeyPath "/DocumentId"
        
        Write-Host "✅ Container 'blob-metadata' created with partition key '/DocumentId'" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Error creating container: $_" -ForegroundColor Red
    exit 1
}

# Create AuditTrail container (if not exists)
Write-Host "`n📦 Creating 'AuditTrail' container..." -ForegroundColor Yellow
try {
    $container = Get-AzCosmosDBSqlContainer -ResourceGroupName $resourceGroup -AccountName $accountName -DatabaseName $DatabaseName -Name "AuditTrail" -ErrorAction SilentlyContinue
    if ($container) {
        Write-Host "✅ Container 'AuditTrail' already exists" -ForegroundColor Green
    } else {
        New-AzCosmosDBSqlContainer `
            -ResourceGroupName $resourceGroup `
            -AccountName $accountName `
            -DatabaseName $DatabaseName `
            -Name "AuditTrail" `
            -PartitionKeyPath "/PolicyNumber"
        
        Write-Host "✅ Container 'AuditTrail' created with partition key '/PolicyNumber'" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Error creating container: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Setup complete!" -ForegroundColor Green
Write-Host "`n📝 Update your appsettings.json:" -ForegroundColor Cyan
Write-Host @"
  "Azure": {
    "CosmosDB": {
      "Endpoint": "$CosmosEndpoint",
      "Key": "YOUR_PRIMARY_KEY",
      "DatabaseName": "$DatabaseName",
      "ContainerId": "AuditTrail",
      "BlobMetadataContainer": "blob-metadata"
    }
  }
"@ -ForegroundColor Yellow

Write-Host "`n🔑 Get your Cosmos DB key:" -ForegroundColor Cyan
Write-Host "  az cosmosdb keys list --name $accountName --resource-group $resourceGroup --query primaryMasterKey -o tsv" -ForegroundColor Yellow
