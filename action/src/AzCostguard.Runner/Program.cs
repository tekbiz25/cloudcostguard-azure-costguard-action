using System;
using System.IO;
using System.Text.Json;
using Octokit;

Console.WriteLine("AzureCostGuard runner started.");
Console.WriteLine($"Args: {string.Join(' ', args)}");

// Parse GitHub environment variables
var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
var eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

// Parse Action inputs
var deepScan = Environment.GetEnvironmentVariable("INPUT_DEEP-SCAN")?.ToLowerInvariant() == "true";
var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") 
                    ?? Environment.GetEnvironmentVariable("INPUT_SUBSCRIPTION-ID");
var location = Environment.GetEnvironmentVariable("INPUT_LOCATION") ?? "eastus";
var terraformExecutable = Environment.GetEnvironmentVariable("INPUT_TERRAFORM-EXECUTABLE");

Console.WriteLine($"Mode: {(deepScan ? "Enhanced Scan (What-If enabled)" : "MVP Diff-only (catalog pricing)")}");

// Handle two-mode system
if (!deepScan)
{
    // MVP Mode: Use fictitious subscription ID to skip What-If calls
    subscriptionId = Environment.GetEnvironmentVariable("AZCG_SUBSCRIPTION") 
                    ?? "00000000-0000-0000-0000-000000000000";
    location = Environment.GetEnvironmentVariable("AZCG_LOCATION") ?? location;
    Console.WriteLine("MVP Mode: Using catalog pricing without Azure What-If validation");
}
else
{
    Console.WriteLine("Enhanced Scan Mode: Will perform What-If validation and drift detection");
}

if (string.IsNullOrEmpty(repo))
{
    Console.WriteLine("Warning: GITHUB_REPOSITORY environment variable is not set.");
    return;
}

Console.WriteLine($"Repository: {repo}");

// Split owner/repo from GITHUB_REPOSITORY
var repoParts = repo.Split('/');
if (repoParts.Length != 2)
{
    Console.WriteLine($"Error: Invalid GITHUB_REPOSITORY format. Expected 'owner/repo', got '{repo}'");
    return;
}

var owner = repoParts[0];
var repoName = repoParts[1];
Console.WriteLine($"Owner: {owner}, Repository: {repoName}");

if (string.IsNullOrEmpty(githubToken))
{
    Console.WriteLine("Warning: GITHUB_TOKEN environment variable is not set.");
    return;
}

if (string.IsNullOrEmpty(eventPath))
{
    Console.WriteLine("Warning: GITHUB_EVENT_PATH environment variable is not set.");
    return;
}

if (!File.Exists(eventPath))
{
    Console.WriteLine($"Warning: GitHub event file does not exist at path: {eventPath}");
    return;
}

// Validate subscription ID (always required, even in MVP mode)
if (string.IsNullOrEmpty(subscriptionId))
{
    Console.WriteLine("Error: Azure Subscription ID is required. Set AZURE_SUBSCRIPTION_ID environment variable or use the subscription-id input.");
    return;
}

Console.WriteLine($"Azure Subscription ID: {subscriptionId}");
Console.WriteLine($"Azure Location: {location}");

try
{
    var eventJson = File.ReadAllText(eventPath);
    var prNumber = JsonDocument.Parse(eventJson)
        .RootElement.GetProperty("pull_request").GetProperty("number").GetInt32();
    
    Console.WriteLine($"Pull Request Number: {prNumber}");

    // Fetch changed files via GitHub API
    var client = new GitHubClient(new ProductHeaderValue("AzureCostGuard"));
    client.Credentials = new Credentials(githubToken);
    
    Console.WriteLine("Fetching changed files from GitHub API...");
    var files = await client.PullRequest.Files(owner, repoName, prNumber);
    
    Console.WriteLine($"Found {files.Count} changed files:");
    foreach (var file in files)
    {
        Console.WriteLine($"- {file.FileName} ({file.Status})");
    }
    
    // Filter for Infrastructure as Code files with improved filtering
    var changed = files.Select(f => f.FileName)
        .Where(f => f.EndsWith(".bicep") || f.EndsWith(".tf") || IsArmTemplate(f))
        .Where(f => !f.Contains("/.github/") && !f.Contains("/node_modules/") && !f.Contains("/.terraform/"))
        .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.Contains("/Debug/") && !f.Contains("/Release/"))
        .Where(f => !f.StartsWith("mocked-") && !f.Contains(".sourcelink.json") && !f.Contains(".pdb"))
        .ToList();
    
    Console.WriteLine($"Found {changed.Count} IaC files in PR.");
    
    Console.WriteLine($"\nFiltered IaC files ({changed.Count}):");
    foreach (var iacFile in changed)
    {
        var fileType = iacFile.EndsWith(".bicep") ? "Bicep" : 
                      iacFile.EndsWith(".tf") ? "Terraform" : "ARM/JSON";
        Console.WriteLine($"- {iacFile} ({fileType})");
    }
    
    if (!changed.Any())
    {
        Console.WriteLine("No IaC files found in this PR. Skipping cost estimation.");
        var emptyCost = new List<CostItem>();
        var emptyOpts = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync("cost.json", JsonSerializer.Serialize(emptyCost, emptyOpts));
        return;
    }

    // Estimate costs using ACE
    Console.WriteLine("\n=== Starting Azure Cost Estimation ===");
    var cost = await AceWrapper.EstimateAsync(changed, subscriptionId, location, terraformExecutable, deepScan);
    
    var opts = new JsonSerializerOptions { WriteIndented = true };
    await File.WriteAllTextAsync("cost.json", JsonSerializer.Serialize(cost, opts));
    
    Console.WriteLine($"\n=== Cost Estimation Complete ===");
    Console.WriteLine($"Estimated cost for {cost.Count} resources");
    
    if (cost.Any())
    {
        var totalCost = cost.Sum(c => c.EurosPerMonth);
        Console.WriteLine($"Total estimated monthly cost: €{totalCost:F2}");
        
        Console.WriteLine("\nTop 5 most expensive resources:");
        foreach (var item in cost.OrderByDescending(c => c.EurosPerMonth).Take(5))
        {
            Console.WriteLine($"  - {item.Service} ({item.Sku}): €{item.EurosPerMonth:F2}/month");
        }
    }
    else
    {
        Console.WriteLine("No resources found for cost estimation.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.StackTrace != null)
    {
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
    Environment.Exit(1);
}

// Helper function to identify ARM template JSON files
static bool IsArmTemplate(string filename)
{
    if (!filename.EndsWith(".json")) return false;
    
    // Include common ARM template patterns
    var armPatterns = new[] { "template", "azuredeploy", "mainTemplate", "nested", "linked" };
    var lowerFilename = Path.GetFileNameWithoutExtension(filename).ToLower();
    
    return armPatterns.Any(pattern => lowerFilename.Contains(pattern)) ||
           filename.Contains("/templates/") ||
           filename.Contains("/arm/") ||
           filename.Contains("/bicep/") ||
           filename.Contains("/infrastructure/");
}