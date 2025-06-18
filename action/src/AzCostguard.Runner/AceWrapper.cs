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
    public static async Task<List<CostItem>> EstimateAsync(IEnumerable<string> files)
    {
        var results = new List<CostItem>();
        foreach (var f in files)
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "ace",
                Arguments = $"estimate --file {f} --format json",
                RedirectStandardOutput = true
            });
            var output = await p.StandardOutput.ReadToEndAsync();
            var doc = JsonDocument.Parse(output);
            results.AddRange(doc.RootElement.GetProperty("resources")
                .EnumerateArray()
                .Select(x => new CostItem
                {
                    ResourceId = x.GetProperty("id").GetString()!,
                    Service = x.GetProperty("serviceName").GetString()!,
                    Sku = x.GetProperty("skuName").GetString()!,
                    EurosPerMonth = x.GetProperty("monthlyCostEUR").GetDecimal()
                }));
            p.WaitForExit();
        }
        return results;
    }
} 