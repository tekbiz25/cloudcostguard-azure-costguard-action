# Azure Setup Guide for Cost Guard Action

This guide walks you through setting up Azure authentication for the Azure Cost Guard GitHub Action.

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed locally
- GitHub repository admin access
- Permission to create Service Principals in Azure AD

## Step-by-Step Setup

### 1. Install Azure CLI (if not already installed)

**Windows:**
```powershell
winget install Microsoft.AzureCLI
```

**macOS:**
```bash
brew install azure-cli
```

**Linux (Ubuntu/Debian):**
```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### 2. Login to Azure

```bash
az login
```

This will open a browser window for authentication. After successful login, you'll see your subscriptions listed.

### 3. Set Your Subscription (if you have multiple)

```bash
# List all subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR-SUBSCRIPTION-ID"
```

### 4. Get Your Subscription ID

```bash
az account show --query id --output tsv
```

Save this value - you'll need it for GitHub secrets.

### 5. Create Service Principal

Replace `YOUR-REPO-NAME` and `YOUR-SUBSCRIPTION-ID` with your actual values:

```bash
az ad sp create-for-rbac \
  --name "github-cost-guard-YOUR-REPO-NAME" \
  --role "Reader" \
  --scopes "/subscriptions/YOUR-SUBSCRIPTION-ID" \
  --output json
```

**Important**: Save the entire JSON output immediately! You cannot retrieve the password later.

Example output:
```json
{
  "appId": "12345678-1234-1234-1234-123456789012",
  "displayName": "github-cost-guard-YOUR-REPO-NAME",
  "password": "abcd1234-5678-90ef-ghij-klmnopqrstuv",
  "tenant": "87654321-4321-4321-4321-210987654321"
}
```

### 6. Verify Service Principal

Test that the service principal works:

```bash
# Login as service principal
az login --service-principal \
  --username "APPID-FROM-STEP-5" \
  --password "PASSWORD-FROM-STEP-5" \
  --tenant "TENANT-FROM-STEP-5"

# Test access to subscription
az account show

# Test ability to read resources (required for cost estimation)
az resource list --query "length(@)"
```

If these commands succeed, your service principal is configured correctly.

### 7. Add GitHub Repository Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each of these secrets:

| Secret Name | Value from Step 5 | Example |
|-------------|-------------------|---------|
| `AZURE_CLIENT_ID` | `appId` | `12345678-1234-1234-1234-123456789012` |
| `AZURE_CLIENT_SECRET` | `password` | `abcd1234-5678-90ef-ghij-klmnopqrstuv` |
| `AZURE_TENANT_ID` | `tenant` | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID | `aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee` |

**Security Note**: GitHub secrets are encrypted and cannot be viewed after creation.

### 8. Test the Setup

Create a test workflow in `.github/workflows/test-cost-guard.yml`:

```yaml
name: Test Azure Cost Guard
on:
  workflow_dispatch:  # Manual trigger for testing

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Test Azure Cost Guard
        uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
        env:
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
        with:
          location: 'eastus'
```

Run this workflow manually to verify everything works.

## Advanced Configuration

### Multiple Environments

For different environments (dev/staging/prod), create separate service principals:

```bash
# Development environment
az ad sp create-for-rbac \
  --name "github-cost-guard-YOUR-REPO-dev" \
  --role "Reader" \
  --scopes "/subscriptions/DEV-SUBSCRIPTION-ID" \
  --output json

# Production environment  
az ad sp create-for-rbac \
  --name "github-cost-guard-YOUR-REPO-prod" \
  --role "Reader" \
  --scopes "/subscriptions/PROD-SUBSCRIPTION-ID" \
  --output json
```

Then use environment-specific secrets in GitHub:
- `DEV_AZURE_CLIENT_ID`, `DEV_AZURE_CLIENT_SECRET`, etc.
- `PROD_AZURE_CLIENT_ID`, `PROD_AZURE_CLIENT_SECRET`, etc.

### Cross-Tenant Scenarios

For organizations with multiple Azure AD tenants, create service principals in each tenant and use appropriate tenant-specific secrets.

### Resource Group Scoped Access

To limit access to specific resource groups:

```bash
az ad sp create-for-rbac \
  --name "github-cost-guard-rg-specific" \
  --role "Reader" \
  --scopes "/subscriptions/YOUR-SUBSCRIPTION-ID/resourceGroups/YOUR-RG-NAME" \
  --output json
```

## Security Best Practices

### 1. Use Minimal Permissions

The service principal only needs **Reader** role. Never grant higher permissions unless absolutely necessary.

### 2. Regular Rotation

Rotate service principal credentials every 90 days:

```bash
# Reset service principal password
az ad sp credential reset --id "YOUR-APP-ID" --output json
```

Update GitHub secrets with the new password.

### 3. Monitor Usage

- Review Azure Activity Log for service principal actions
- Set up alerts for unusual activity
- Use Azure Policy to enforce compliance

### 4. Separate Service Principals

Use different service principals for:
- Different repositories
- Different environments
- Different teams/projects

## Troubleshooting

### Common Errors

#### Error: "Insufficient privileges to complete the operation"

**Cause**: You don't have permission to create service principals in Azure AD.

**Solution**: 
- Contact your Azure AD administrator
- Request "Application Developer" role in Azure AD
- Use an existing service principal if available

#### Error: "The provided credentials do not have access to subscription"

**Cause**: Service principal doesn't have correct subscription access.

**Solution**:
```bash
# Verify role assignment
az role assignment list --assignee "YOUR-APP-ID" --all

# Add Reader role if missing
az role assignment create \
  --assignee "YOUR-APP-ID" \
  --role "Reader" \
  --scope "/subscriptions/YOUR-SUBSCRIPTION-ID"
```

#### Error: "AADSTS700016: Application with identifier was not found"

**Cause**: Wrong App ID or service principal was deleted.

**Solution**:
- Verify the `AZURE_CLIENT_ID` secret value
- Recreate the service principal if it was deleted

#### Error: "AADSTS7000215: Invalid client secret is provided"

**Cause**: Wrong client secret or it has expired.

**Solution**:
- Verify the `AZURE_CLIENT_SECRET` secret value
- Reset the service principal password and update secrets

### Testing Commands

```bash
# Test service principal login
az login --service-principal \
  --username "$AZURE_CLIENT_ID" \
  --password "$AZURE_CLIENT_SECRET" \
  --tenant "$AZURE_TENANT_ID"

# Test subscription access
az account show --subscription "$AZURE_SUBSCRIPTION_ID"

# Test resource read permissions
az resource list --subscription "$AZURE_SUBSCRIPTION_ID" --query "length(@)"

# Test cost management API access
az consumption usage list --subscription "$AZURE_SUBSCRIPTION_ID" --top 1
```

### Getting Help

1. **Azure CLI Issues**: Run `az --help` or visit [Azure CLI docs](https://docs.microsoft.com/en-us/cli/azure/)
2. **Azure AD Issues**: Check [Azure AD documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
3. **GitHub Secrets**: See [GitHub secrets documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
4. **Action Issues**: Create an issue in this repository

## Cleanup

To remove the service principal when no longer needed:

```bash
# List service principals
az ad sp list --display-name "github-cost-guard" --query "[].{Name:displayName,AppId:appId}"

# Delete service principal
az ad sp delete --id "YOUR-APP-ID"
```

## Next Steps

After completing this setup:

1. ✅ Test the action with a sample PR
2. ✅ Configure your main workflow
3. ✅ Set up cost thresholds and alerts
4. ✅ Train your team on interpreting cost reports

Return to the [main README](../README.md) for usage examples and advanced configuration. 