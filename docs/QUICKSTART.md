# Quick Start Guide

Get started with Azure Cost Guard in your repository in 5 minutes!

## 🚀 Quick Setup

### 1. Create Your Workflow

Create `.github/workflows/azure-cost-check.yml` in your repository:

```yaml
name: Azure Cost Check
on:
  pull_request:
    branches: [main]

jobs:
  cost-estimation:
    runs-on: ubuntu-latest
    name: Estimate Azure Costs
    steps:
      - uses: actions/checkout@v4
      
      - name: Azure Cost Guard
        uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
        env:
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### 2. Create Azure Service Principal

Run this in Azure CLI (replace YOUR-SUBSCRIPTION-ID):

```bash
az ad sp create-for-rbac \
  --name "github-cost-guard-$(date +%s)" \
  --role "Reader" \
  --scopes "/subscriptions/YOUR-SUBSCRIPTION-ID" \
  --output json
```

### 3. Add GitHub Secrets

Copy the output and add these secrets to your repository:

- `AZURE_CLIENT_ID` → `appId` value
- `AZURE_CLIENT_SECRET` → `password` value  
- `AZURE_TENANT_ID` → `tenant` value
- `AZURE_SUBSCRIPTION_ID` → Your subscription ID

**Where to add secrets:** Repository → Settings → Secrets and variables → Actions → New repository secret

### 4. Test It!

Create a PR with a `.bicep`, `.json`, or `.tf` file and watch the cost estimation run! 🎉

## 📝 Example Infrastructure Files

### Bicep Example (`test.bicep`)
```bicep
param location string = resourceGroup().location
param storageAccountName string = 'mystorageaccount${uniqueString(resourceGroup().id)}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
```

### Terraform Example (`main.tf`)
```hcl
resource "azurerm_resource_group" "example" {
  name     = "example-resources"
  location = "East US"
}

resource "azurerm_storage_account" "example" {
  name                     = "examplestorageaccount"
  resource_group_name      = azurerm_resource_group.example.name
  location                 = azurerm_resource_group.example.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}
```

### ARM Template Example (`template.json`)
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "resources": [
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-04-01",
      "name": "mystorageaccount",
      "location": "[resourceGroup().location]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2"
    }
  ]
}
```

## 🔧 Advanced Options

### Custom Azure Region
```yaml
- name: Azure Cost Guard
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  env:
    # ... secrets ...
  with:
    location: 'westeurope'  # Cost estimates for West Europe
```

### Custom Terraform Path
```yaml
- name: Azure Cost Guard
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  env:
    # ... secrets ...
  with:
    terraform-executable: '/usr/local/bin/terraform'
```

### Only Run on Infrastructure Changes
```yaml
name: Azure Cost Check
on:
  pull_request:
    paths:
      - '**/*.bicep'
      - '**/*.json' 
      - '**/*.tf'
      - 'infrastructure/**'
```

## 🆘 Need Help?

- 📖 [Full Documentation](../README.md)
- 🔧 [Azure Setup Guide](AZURE_SETUP.md)
- 🐛 [Report Issues](https://github.com/tekbiz25/cloudcostguard-azure-costguard-action/issues)

That's it! You now have automated Azure cost estimation on every Pull Request! 🎉 