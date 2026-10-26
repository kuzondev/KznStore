using System.Net.Http.Headers;
using System.Text;
using KznStore.Store;

namespace KznStore;

public static class StoreCatalogClient
{
    public static async Task<StoreCatalog> FetchAsync(CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        using var request = new HttpRequestMessage(HttpMethod.Get, StoreRepository.CatalogUrl + "?t=" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60));
        request.Headers.UserAgent.ParseAdd("KznStore/1.0");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri is not { Scheme: "https", Host: "raw.githubusercontent.com" })
            throw new InvalidDataException("Unexpected catalog source.");
        if (response.Content.Headers.ContentLength > 1024 * 1024) throw new InvalidDataException("Catalog too large.");
        await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var content = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
        {
            if (content.Length + read > 1024 * 1024) throw new InvalidDataException("Catalog too large.");
            await content.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
        }
        return StoreCatalog.Parse(Encoding.UTF8.GetString(content.ToArray()));
    }
}
