@description('Demo Infrastructure for Azure Cost Guard Testing')
param location string = resourceGroup().location
param appName string = 'test-demo-app'

// Storage Account - Standard LRS in East US (~€3.42/month for basic usage)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'teststorageaccount${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

// App Service Plan - Basic B1 tier (~€11.17/month)  
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'test-app-plan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

// Web App - Free when using App Service Plan
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
    }
  }
}

// Updated: Now shows realistic cost estimates instead of €0.00! 
