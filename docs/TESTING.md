# AzureCostGuard Testing Guide

## GitHub Actions Workflow

The repository is configured with a GitHub Actions workflow (`.github/workflows/test.yml`) that automatically runs cost estimation on every pull request.

### How It Works

1. **Automatic Trigger**: The workflow runs on every PR automatically
2. **File Detection**: It scans the PR for changed Infrastructure as Code files:
   - Bicep files (`.bicep`)
   - ARM templates (`.json`) 
   - Terraform files (`.tf`)
3. **Cost Estimation**: Uses Azure Cost Estimator (ACE) to calculate monthly costs
4. **MVP Mode**: Runs in catalog pricing mode (no Azure credentials needed)

### Test Infrastructure

The `test-bicep/test.bicep` file contains sample Azure resources for testing:
- Storage Account (Standard_LRS)
- App Service Plan (Basic B1)
- Web App

### Creating a Test PR

To test the cost estimation:

1. Create a new branch
2. Modify `test-bicep/test.bicep` (change SKUs, add resources, etc.)
3. Create a pull request
4. The GitHub Action will automatically run and provide cost estimates

### Expected Output

The action will:
- Detect the changed bicep file
- Calculate costs for each resource
- Output total monthly cost in Euros
- Show the top 5 most expensive resources

### Troubleshooting

If the action fails:
- Check the GitHub Actions logs for detailed error messages
- Ensure the bicep file syntax is valid
- Verify that submodules are properly checked out 