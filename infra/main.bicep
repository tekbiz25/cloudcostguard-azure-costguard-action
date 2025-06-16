@description('The location for all resources')
param location string = resourceGroup().location

@description('The name of the storage account')
param storageAccountName string = 'stor${uniqueString(resourceGroup().id)}'

@description('The SKU of the storage account')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
  'Premium_LRS'
])
param storageAccountSku string = 'Standard_LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageAccountSku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name 