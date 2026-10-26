using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KznStore.Store;

public sealed record GitHubCommunity(int Stars, DateTimeOffset RetrievedAt);
public static class GitHubCommunityClient
{
    public static bool ValidRepository(string? repository) => repository is not null && repository.Length <= 180
        && Regex.IsMatch(repository, @"\A[A-Za-z0-9_-]+/[A-Za-z0-9_.-]+\z");
    public static async Task<GitHubCommunity?> FetchAsync(string repository, CancellationToken cancellation, HttpMessageHandler? handler = null)
    {
        if (!ValidRepository(repository)) return null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var client = new HttpClient(handler ?? new HttpClientHandler());
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KznStore/0.10.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        try
        {
            using var response = await client.GetAsync($"https://api.github.com/repos/{repository}", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var content = new MemoryStream(); var buffer = new byte[8192]; int read;
            while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
            { if (content.Length + read > 256 * 1024) return null; content.Write(buffer, 0, read); }
            using var json = JsonDocument.Parse(content.ToArray());
            return json.RootElement.TryGetProperty("stargazers_count", out var stars) && stars.TryGetInt32(out var count) && count >= 0
                ? new(count, DateTimeOffset.Now) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException) { return null; }
    }
}
