using System.Diagnostics;
using System.Text.Json;

public class CostItem
{
    public string ResourceId { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal EurosPerMonth { get; set; }
}

public static class AceWrapper
{
    public static async Task<List<CostItem>> EstimateAsync(IEnumerable<string> files, string subscriptionId, string location = "eastus", string? terraformExecutable = null, bool deepScan = false)
    {
        // Only validate Azure authentication for deep scan mode
        if (deepScan)
        {
            ValidateAzureAuthentication();
        }
        else
        {
            Console.WriteLine("MVP Mode: Skipping Azure authentication validation");
        }
        
        var results = new List<CostItem>();
        
        foreach (var file in files)
        {
            Console.WriteLine($"Estimating costs for: {file}");
            
            try
            {
                // Build ACE arguments based on mode
                string arguments;
                if (deepScan)
                {
                    // Enhanced mode: Use standard command with What-If
                    arguments = $"sub \"{file}\" {subscriptionId} {location} --generate-json-output --stdout";
                }
                else
                {
                    // MVP mode: Use mocked responses to avoid Azure authentication completely
                    arguments = $"sub \"{file}\" {subscriptionId} {location} --generate-json-output --stdout --mocked-retail-api-response-path \"/app/mocked-retail-prices.json\" --what-if-file \"/app/mocked-whatif-response.json\"";
                }
                
                // Add Terraform executable option if specified and file is a Terraform file
                if (!string.IsNullOrEmpty(terraformExecutable) && file.EndsWith(".tf"))
                {
                    arguments += $" --tf-executable \"{terraformExecutable}\"";
                }
                
                Console.WriteLine($"ACE Command: azure-cost-estimator {arguments}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "azure-cost-estimator",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    Console.WriteLine($"Failed to start azure-cost-estimator process for {file}");
                    continue;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"azure-cost-estimator failed for {file}:");
                    Console.WriteLine($"Exit code: {process.ExitCode}");
                    Console.WriteLine($"Error: {error}");
                    
                    // For Terraform files, provide additional troubleshooting info
                    if (file.EndsWith(".tf"))
                    {
                        Console.WriteLine("Terraform troubleshooting:");
                        Console.WriteLine("- Ensure Terraform is installed and accessible");
                        Console.WriteLine("- Run 'terraform init' in the directory containing .tf files");
                        Console.WriteLine("- Make sure all required Terraform providers are configured");
                        if (string.IsNullOrEmpty(terraformExecutable))
                        {
                            Console.WriteLine("- Consider specifying terraform-executable input if Terraform is not in PATH");
                        }
                    }
                    
                    if (!deepScan)
                    {
                        Console.WriteLine("MVP Mode troubleshooting:");
                        Console.WriteLine("- This is normal if the file requires What-If validation");
                        Console.WriteLine("- Consider enabling deep-scan mode for enhanced validation");
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine($"No output from azure-cost-estimator for {file}");
                    continue;
                }

                var doc = JsonDocument.Parse(output);
                if (doc.RootElement.TryGetProperty("resources", out var resourcesProperty))
                {
                    var fileResults = resourcesProperty.EnumerateArray()
                        .Select(x => new CostItem
                        {
                            ResourceId = x.GetProperty("id").GetString() ?? "",
                            Service = x.GetProperty("serviceName").GetString() ?? "",
                            Sku = x.GetProperty("skuName").GetString() ?? "",
                            EurosPerMonth = x.GetProperty("monthlyCostEUR").GetDecimal()
                        });
                    
                    results.AddRange(fileResults);
                    Console.WriteLine($"Found {fileResults.Count()} resources in {file}");
                }
                else
                {
                    Console.WriteLine($"No resources found in output for {file}");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse JSON output for {file}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }
        
        return results;
    }

    private static void ValidateAzureAuthentication()
    {
        var requiredEnvVars = new[]
        {
            ("AZURE_CLIENT_ID", "Service Principal Application ID"),
            ("AZURE_CLIENT_SECRET", "Service Principal Secret"),
            ("AZURE_TENANT_ID", "Azure AD Tenant ID")
        };

        var missing = new List<string>();

        foreach (var (envVar, description) in requiredEnvVars)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
            {
                missing.Add($"{envVar} ({description})");
            }
        }

        if (missing.Any())
        {
            var errorMessage = "Missing required Azure authentication environment variables for Enhanced Scan mode:\n" +
                              string.Join("\n", missing.Select(m => $"  - {m}")) +
                              "\n\nFor Enhanced Scan mode, set these as GitHub Secrets in your repository." +
                              "\nOr use MVP mode (default) which only requires catalog pricing." +
                              "\nSee documentation: https://github.com/tekbiz25/cloudcostguard-azure-costguard-action#setup";
            
            throw new InvalidOperationException(errorMessage);
        }

        Console.WriteLine("✓ Azure authentication environment variables are configured for Enhanced Scan mode");
    }
} 