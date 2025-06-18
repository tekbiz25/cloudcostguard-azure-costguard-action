# Azure Cost Guard GitHub Action

A GitHub Action that automatically estimates Azure infrastructure costs for Pull Requests containing Bicep or ARM templates. This action integrates with the [Azure Cost Estimator (ACE)](https://github.com/TheCloudTheory/arm-estimator) to provide cost insights before infrastructure changes are deployed.

## Features

- 🔍 **Automatic Detection**: Scans PR changes for Bicep (`.bicep`) and ARM template (`.json`) files
- 💰 **Cost Estimation**: Provides monthly cost estimates for Azure resources
- 🔐 **Secure Authentication**: Uses Azure Service Principal for secure access
- 📊 **Detailed Reports**: Shows cost breakdown by service and resource
- ⚡ **Fast Execution**: Runs in Docker container for consistent performance
- 🌍 **Multi-tenant Support**: Works across different Azure subscriptions and tenants

## Quick Start

### 1. Add to Your Workflow

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
        
      - name: Azure Cost Guard
        uses: your-org/azure-cost-guard-action@v1
        env:
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
        with:
          location: 'eastus'
```

### 2. Set Up Azure Authentication

Follow the [Azure Setup Guide](#azure-setup-guide) below to configure authentication.

## Azure Setup Guide

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
| `diff-path` | Path to diff JSON | No | Auto-detected |
| `apply` | Apply remediation when true | No | `false` |

*Either input or environment variable required

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `AZURE_CLIENT_ID` | Service Principal Application ID | ✅ Yes |
| `AZURE_CLIENT_SECRET` | Service Principal Secret | ✅ Yes |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | ✅ Yes |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | ✅ Yes* |

*Can be provided via input instead

## Advanced Configuration

### Custom Location

```yaml
- name: Azure Cost Guard
  uses: your-org/azure-cost-guard-action@v1
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
  uses: your-org/azure-cost-guard-action@v1
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

jobs:
  cost-estimation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Azure Cost Guard
        uses: your-org/azure-cost-guard-action@v1
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

Files in these locations are automatically excluded:
- `.github/` directories
- `node_modules/` directories

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

- 📚 [Documentation](https://github.com/your-org/azure-cost-guard-action/wiki)
- 🐛 [Report Issues](https://github.com/your-org/azure-cost-guard-action/issues)
- 💬 [Discussions](https://github.com/your-org/azure-cost-guard-action/discussions)

## Related Projects

- [Azure Cost Estimator (ACE)](https://github.com/TheCloudTheory/arm-estimator) - The underlying cost estimation engine
- [Azure Bicep](https://github.com/Azure/bicep) - Infrastructure as Code for Azure
