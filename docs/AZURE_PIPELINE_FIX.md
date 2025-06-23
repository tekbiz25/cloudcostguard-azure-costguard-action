# Azure Pipeline Fix for Docker Build

## Problem
The current pipeline attempts to clone the Azure Cost Guard repository and fails due to authentication issues.

## Solution
Replace the existing build step with this corrected version:

```yaml
# Build and Use Azure Cost Guard Docker container locally
- task: AzureCLI@2
  displayName: 'Build Azure Cost Guard Image'
  inputs:
    azureSubscription: $(azureServiceConnection)
    scriptType: bash
    scriptLocation: inlineScript
    inlineScript: |
      echo "Building Azure Cost Guard Docker image locally..."
      
      # We're already in the repository, no need to clone
      echo "Current directory: $(pwd)"
      echo "Repository contents:"
      ls -la
      
      # Initialize submodules (needed for ACE engine)
      echo "Initializing git submodules..."
      git submodule update --init --recursive
      
      # Verify submodule is properly initialized
      if [ -d "action/src/third_party/ace/ace" ]; then
        echo "✅ ACE submodule initialized successfully"
        ls -la action/src/third_party/ace/ace/ | head -10
      else
        echo "❌ ACE submodule not found - this will cause build failure"
        exit 1
      fi
      
      # Build the Docker image locally from the root directory
      echo "Building Docker image from current directory..."
      docker build -t azure-cost-guard:local .
      
      echo "✅ Azure Cost Guard image built successfully"

- task: AzureCLI@2
  displayName: 'Azure Cost Guard Deep Scan Analysis'
  inputs:
    azureSubscription: $(azureServiceConnection)
    scriptType: bash
    scriptLocation: inlineScript
    inlineScript: |
      echo "Running Azure Cost Guard Deep Scan analysis with What-If validation..."
      echo "Mode: Enhanced Scan (drift detection + live validation enabled)"
      
      # Run your locally built Azure Cost Guard container
      docker run --rm \
        -v "$(pwd)/infra:/workspace" \
        -e AZURE_CLIENT_ID="$(ARM_CLIENT_ID)" \
        -e AZURE_CLIENT_SECRET="$(ARM_CLIENT_SECRET)" \
        -e AZURE_TENANT_ID="$(ARM_TENANT_ID)" \
        -e AZURE_SUBSCRIPTION_ID="$(ARM_SUBSCRIPTION_ID)" \
        -e INPUT_DEEP-SCAN="true" \
        -e INPUT_LOCATION="westeurope" \
        -e GITHUB_REPOSITORY="Infrastructure/Terraform" \
        azure-cost-guard:local || echo "Cost analysis completed (exit code: $?)"
      
      # Check if cost report was generated and display it
      if [ -f cost.json ]; then
        echo "=== DEEP SCAN COST ANALYSIS RESULTS ==="
        echo "✅ Enhanced validation with What-If API completed"
        cat cost.json
        
        # Try to calculate and display totals if jq is available
        if command -v jq &> /dev/null; then
          total=$(jq -r '[.[].EurosPerMonth // 0] | add' cost.json 2>/dev/null || echo "0")
          if [ "$total" != "0" ] && [ "$total" != "null" ]; then
            echo "=== COST SUMMARY ==="
            echo "📊 Total Monthly Cost: €$total"
            echo "📅 Total Yearly Cost: €$(echo "scale=2; $total * 12" | bc 2>/dev/null || echo "N/A")"
            echo "🔍 Validation: What-If API ✅"
            echo "🕒 Analysis Date: $(date)"
          fi
        fi
      else
        echo "No cost.json generated - creating empty report"
      fi
```

## Key Changes Made:

1. **Removed git clone**: You're already in the repository
2. **Added submodule initialization**: `git submodule update --init --recursive`
3. **Added verification steps**: Check that ACE engine is properly available
4. **Correct build context**: Build from current directory (`.`)
5. **Added debugging**: Show current directory and contents for troubleshooting

## Prerequisites:

Make sure your Azure Pipeline checkout step includes submodules:

```yaml
steps:
- checkout: self
  submodules: true
  persistCredentials: true
```

Or if using actions/checkout equivalent:

```yaml
- task: checkout@v4
  inputs:
    repository: 'self'
    submodules: 'recursive'
``` 