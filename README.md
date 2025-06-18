# Azure Cost Guard GitHub Action

A GitHub Action that automatically estimates Azure infrastructure costs for Pull Requests containing Bicep, ARM templates, or Terraform files. This action integrates with the [Azure Cost Estimator (ACE)](https://github.com/TheCloudTheory/arm-estimator) to provide cost insights before infrastructure changes are deployed.

## Features

- 🔍 **Automatic Detection**: Scans PR changes for Bicep (`.bicep`), ARM template (`.json`), and Terraform (`.tf`) files
- 💰 **Cost Estimation**: Provides monthly cost estimates for Azure resources
- 🔐 **Secure Authentication**: Uses Azure Service Principal for secure access
- 📊 **Detailed Reports**: Shows cost breakdown by service and resource
- ⚡ **Fast Execution**: Runs in Docker container for consistent performance
- 🌍 **Multi-tenant Support**: Works across different Azure subscriptions and tenants
- 🛠️ **Multi-IaC Support**: Works with Bicep, ARM templates, and Terraform

## Quick Start

> 🚀 **Get started in 30 seconds!** MVP mode requires no Azure credentials - just add the workflow and go!

### 1. Add to Your Workflow (MVP Mode - Default)

Create `.github/workflows/cost-check.yml`:

```yaml
name: Azure Cost Check
on:
  pull_request:
    branches: [main, develop]

jobs:
  cost-estimation:
    runs-on: ubuntu-latest
    name: Estimate Azure Costs
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Azure Cost Guard (MVP Mode)
        uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
        with:
          location: 'eastus'
```

**That's it!** MVP mode provides fast cost estimates using Microsoft's pricing catalog without requiring any Azure credentials.

### 2. Optional: Enhanced Features

For drift detection and What-If validation, see the [Deep Scan Mode](#optional-deep-scan-mode) section below.

## Azure Setup Guide (Deep Scan Mode Only)

> ℹ️ **Note**: This section is only required for Deep Scan mode. MVP mode works without any Azure credentials.

### Step 1: Create Azure Service Principal

Run this command in Azure CLI to create a service principal:

```bash
az ad sp create-for-rbac \
  --name "github-cost-guard-YOUR-REPO-NAME" \
  --role "Reader" \
  --scopes "/subscriptions/YOUR-SUBSCRIPTION-ID" \
  --output json
```

This will output JSON like:
```json
{
  "appId": "12345678-1234-1234-1234-123456789012",
  "displayName": "github-cost-guard-YOUR-REPO-NAME",
  "password": "YOUR-CLIENT-SECRET",
  "tenant": "87654321-4321-4321-4321-210987654321"
}
```

### Step 2: Set GitHub Repository Secrets

Go to your repository → Settings → Secrets and variables → Actions → New repository secret.

Add these secrets:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZURE_CLIENT_ID` | `appId` from step 1 | Service Principal Application ID |
| `AZURE_CLIENT_SECRET` | `password` from step 1 | Service Principal Secret |
| `AZURE_TENANT_ID` | `tenant` from step 1 | Azure AD Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID | Azure Subscription ID |

### Step 3: Verify Permissions

The service principal needs these minimum permissions:
- **Reader** role on the subscription (for pricing data)
- Access to Azure Cost Management APIs (included with Reader role)

## Configuration Options

### Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `subscription-id` | Azure Subscription ID | No* | Uses `AZURE_SUBSCRIPTION_ID` env var |
| `location` | Azure region for cost estimation | No | `eastus` |
| `terraform-executable` | Path to Terraform executable | No | Searches in PATH |
| `deep-scan` | Enable What-If validation and drift detection | No | `false` |
| `diff-path` | Path to diff JSON | No | Auto-detected |
| `apply` | Apply remediation when true | No | `false` |

*Required for Deep Scan mode, optional for MVP mode

### Environment Variables

| Variable | Description | MVP Mode | Deep Scan Mode |
|----------|-------------|----------|----------------|
| `AZURE_CLIENT_ID` | Service Principal Application ID | ❌ Not needed | ✅ Required |
| `AZURE_CLIENT_SECRET` | Service Principal Secret | ❌ Not needed | ✅ Required |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | ❌ Not needed | ✅ Required |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | ❌ Optional* | ✅ Required |

*MVP mode uses fictitious subscription ID if not provided

### Optional: Deep Scan Mode

By default, Azure Cost Guard runs in **MVP mode** - providing fast cost estimates from Microsoft's pricing catalog without requiring Azure credentials. This is perfect for getting quick cost insights on PRs.

For enhanced validation including **drift detection** and **live What-If validation**, enable Deep Scan mode:

#### Step 1: Create Azure Service Principal with Enhanced Permissions

```bash
# Create service principal with Reader + Cost Management Reader roles
az ad sp create-for-rbac \
  --name "github-cost-guard-YOUR-REPO-NAME" \
  --role "Reader" \
  --scopes "/subscriptions/YOUR-SUBSCRIPTION-ID" \
  --output json

# Add Cost Management Reader role for enhanced pricing data
az role assignment create \
  --assignee YOUR-SERVICE-PRINCIPAL-APP-ID \
  --role "Cost Management Reader" \
  --scope "/subscriptions/YOUR-SUBSCRIPTION-ID"
```

#### Step 2: Add GitHub Repository Secrets

Set these additional secrets for Deep Scan mode:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZCG_CLIENT_ID` | Service Principal App ID | For Deep Scan authentication |
| `AZCG_TENANT_ID` | Azure AD Tenant ID | For Deep Scan authentication |
| `AZCG_CLIENT_SECRET` | Service Principal Secret | For Deep Scan authentication |
| `AZCG_SUBSCRIPTION` | Your subscription ID | Target subscription for validation |

#### Step 3: Enable Deep Scan in Workflow

```yaml
- name: Azure Cost Guard (Deep Scan)
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  env:
    AZURE_CLIENT_ID: ${{ secrets.AZCG_CLIENT_ID }}
    AZURE_CLIENT_SECRET: ${{ secrets.AZCG_CLIENT_SECRET }}
    AZURE_TENANT_ID: ${{ secrets.AZCG_TENANT_ID }}
    AZURE_SUBSCRIPTION_ID: ${{ secrets.AZCG_SUBSCRIPTION }}
  with:
    deep-scan: true  # Enable What-If validation
    location: 'eastus'
```

#### Deep Scan vs MVP Comparison

| Feature | MVP Mode (Default) | Deep Scan Mode |
|---------|-------------------|----------------|
| **Azure Credentials** | ❌ Not needed | ✅ Service Principal required |
| **API Calls** | ❌ Catalog only | ✅ What-If + Resource Graph |
| **Speed** | ⚡ Fast | 🐌 Slower (API calls) |
| **Accuracy** | 📊 Catalog pricing | 🎯 Live validation |
| **Drift Detection** | ❌ No | ✅ Yes |
| **Setup Complexity** | 🟢 One-click | 🟡 Requires SPN setup |

## Advanced Configuration

### Custom Location

```yaml
- name: Azure Cost Guard
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  env:
    AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
    AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
    AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
    AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  with:
    location: 'westeurope'  # Estimate costs for West Europe region
```

### Multiple Subscriptions

For organizations with multiple subscriptions, you can set up different secrets per environment:

```yaml
- name: Azure Cost Guard - Production
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  env:
    AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
    AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
    AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
    AZURE_SUBSCRIPTION_ID: ${{ secrets.PROD_SUBSCRIPTION_ID }}
  with:
    location: 'eastus'
```

### Conditional Execution

Only run on infrastructure changes:

```yaml
name: Azure Cost Check
on:
  pull_request:
    paths:
      - 'infrastructure/**'
      - '**/*.bicep'
      - '**/*.json'
      - '**/*.tf'

jobs:
  cost-estimation:
    runs-on: ubuntu-latest
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

## Output

The action produces:
- Console output with cost breakdown
- `cost.json` file with detailed results
- Summary of estimated monthly costs

Example output:
```
=== Cost Estimation Complete ===
Estimated cost for 5 resources
Total estimated monthly cost: €127.45

Top 5 most expensive resources:
  - Virtual Machine (Standard_D2s_v3): €65.40/month
  - Storage Account (Standard_LRS): €45.20/month
  - Application Gateway (Standard_v2): €12.30/month
  - Public IP (Standard): €3.65/month
  - Network Security Group: €0.90/month
```

## Supported File Types

- **Bicep files** (`.bicep`)
- **ARM templates** (`.json`)
- **Terraform files** (`.tf`)

Files in these locations are automatically excluded:
- `.github/` directories
- `node_modules/` directories
- `.terraform/` directories

## Troubleshooting

### Authentication Errors

```
Error: Missing required Azure authentication environment variables
```

**Solution**: Ensure all four Azure secrets are set in your repository settings.

### Subscription Access Errors

```
Error: The provided credentials do not have access to subscription
```

**Solution**: 
1. Verify the subscription ID is correct
2. Ensure the service principal has Reader role on the subscription
3. Check that the tenant ID matches your subscription's tenant

### No Files Found

```
No IaC files found in this PR. Skipping cost estimation.
```

This is normal behavior when PRs don't contain infrastructure files.

### Permission Denied

```
Error: Insufficient privileges to complete the operation
```

**Solution**: Grant the service principal additional permissions or contact your Azure administrator.

### Terraform Issues

```
azure-cost-estimator failed for main.tf
```

**Common Solutions**:
1. **Terraform not installed**: Ensure Terraform is installed in the environment
2. **Missing terraform init**: Run `terraform init` in directories with .tf files
3. **Provider configuration**: Ensure all required Terraform providers are properly configured
4. **Custom Terraform path**: Use the `terraform-executable` input if Terraform isn't in PATH
5. **State file issues**: Ensure Terraform state is properly initialized

Example with custom Terraform path:
```yaml
- name: Azure Cost Guard
  uses: tekbiz25/cloudcostguard-azure-costguard-action@v1
  with:
    terraform-executable: '/custom/path/to/terraform'
```

## Security Considerations

- **Principle of Least Privilege**: The service principal only has Reader access
- **Secure Storage**: All credentials are stored as encrypted GitHub secrets
- **Audit Trail**: All API calls are logged in Azure Activity Log
- **Rotation**: Regularly rotate service principal secrets
- **Isolation**: Each repository should use its own service principal

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with a sample repository
5. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Support

- 📚 [Documentation](https://github.com/tekbiz25/cloudcostguard-azure-costguard-action/wiki)
- 🐛 [Report Issues](https://github.com/tekbiz25/cloudcostguard-azure-costguard-action/issues)
- 💬 [Discussions](https://github.com/tekbiz25/cloudcostguard-azure-costguard-action/discussions)

## Related Projects

- [Azure Cost Estimator (ACE)](https://github.com/TheCloudTheory/arm-estimator) - The underlying cost estimation engine
- [Azure Bicep](https://github.com/Azure/bicep) - Infrastructure as Code for Azure