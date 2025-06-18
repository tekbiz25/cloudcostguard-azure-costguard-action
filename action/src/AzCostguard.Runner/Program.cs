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
    
    // Filter extensions and save result
    var changed = files.Select(f => f.FileName)
        .Where(f => f.EndsWith(".bicep"))
        .ToList();
    
    Console.WriteLine($"Found {changed.Count} IaC files in PR.");
    
    Console.WriteLine($"\nFiltered .bicep files ({changed.Count}):");
    foreach (var bicepFile in changed)
    {
        Console.WriteLine($"- {bicepFile}");
    }
    
    // Estimate costs using ACE
    if (changed.Count > 0)
    {
        Console.WriteLine("\nEstimating costs using ACE...");
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var cost = await AceWrapper.EstimateAsync(changed);
        await File.WriteAllTextAsync("cost.json", JsonSerializer.Serialize(cost, opts));
        Console.WriteLine($"Estimated cost for {cost.Count} resources");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}