using System.Text.Json;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Xml.Linq;
using System.Net.Http;

namespace KznStore.Store;

public static class StoreRepository
{
    public const string OwnerAndName = "kuzondev/KznStore";
    public const string CatalogUrl = "https://raw.githubusercontent.com/kuzondev/KznStore/main/catalog/store.json";
}

public sealed class StoreProduct
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string PackageName { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string GitHubRepository { get; set; } = "";
    public string Repository => string.IsNullOrEmpty(GitHubRepository) && Id == "kzn-notch" && PackageName == "Notchify.Desktop"
        ? "kuzondev/KznNotch" : GitHubRepository;
    public Dictionary<string, string> Description { get; set; } = [];
    public string Describe(string language) => Description.GetValueOrDefault(language)
        ?? Description.GetValueOrDefault("en") ?? Description.Values.FirstOrDefault() ?? "";
}

public sealed class StoreCatalog
{
    public int SchemaVersion { get; set; } = 1;
    public List<StoreProduct> Products { get; set; } = [];
    public static StoreCatalog Read(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Catalog too large (max. 1 MB).");
        return Parse(File.ReadAllText(path));
    }
    public static StoreCatalog Parse(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new InvalidDataException("Catalog too large (max. 1 MB).");
        var catalog = JsonSerializer.Deserialize<StoreCatalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Empty catalog.");
        if (catalog.SchemaVersion != 1 || catalog.Products is null || catalog.Products.Count > 200) throw new InvalidDataException("Unsupported catalog schema.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in catalog.Products)
        {
            if (string.IsNullOrWhiteSpace(p.Id) || p.Id.Length > 80 || !ids.Add(p.Id)
                || string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 120
                || string.IsNullOrWhiteSpace(p.PackageName) || string.IsNullOrWhiteSpace(p.Publisher)
                || !System.Version.TryParse(p.Version, out _) || p.Description is null)
                throw new InvalidDataException("Invalid or duplicate product.");
            if (!string.IsNullOrEmpty(p.GitHubRepository) && !GitHubCommunityClient.ValidRepository(p.GitHubRepository)) throw new InvalidDataException("Invalid GitHub repository.");
            if (!string.IsNullOrEmpty(p.DownloadUrl) && (!Uri.TryCreate(p.DownloadUrl, UriKind.Absolute, out var uri)
                || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || p.Sha256.Length != 64 || !p.Sha256.All(Uri.IsHexDigit)))
                throw new InvalidDataException("Downloads require HTTPS and SHA-256.");
        }
        return catalog;
    }
    public static StoreCatalog Default => new() { Products = [new()
    {
        Id = "kzn-notch", Name = "Kzn Notch", Version = "0.10.7.0",
        PackageName = "Notchify.Desktop", Publisher = "CN=Notchify Development",
        GitHubRepository = "kuzondev/KznNotch",
        Description = new() { ["de"] = "Musik, Fokus, Aufgaben und deine eigenen .kzn-Erweiterungen – direkt am Bildschirmrand.",
            ["en"] = "Music, focus, tasks and your own .kzn extensions, right at the edge of your screen." }
    }] };

    public static void ValidatePackage(string path, StoreProduct product)
    {
        using var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry("AppxManifest.xml") ?? throw new InvalidDataException("MSIX manifest missing.");
        if (entry.Length > 1024 * 1024) throw new InvalidDataException("MSIX manifest too large.");
        using var stream = entry.Open();
        using var reader = System.Xml.XmlReader.Create(stream, new() { DtdProcessing = System.Xml.DtdProcessing.Prohibit, MaxCharactersInDocument = 1024 * 1024 });
        var identity = XDocument.Load(reader).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "Identity");
        if ((string?)identity?.Attribute("Name") != product.PackageName || (string?)identity?.Attribute("Publisher") != product.Publisher
            || !System.Version.TryParse((string?)identity?.Attribute("Version"), out var version)
            || version != System.Version.Parse(product.Version)) throw new InvalidDataException("Package identity, publisher or version does not match catalog.");
    }

    public static async Task<string> DownloadAsync(StoreProduct product, string directory, IProgress<double> progress, CancellationToken cancellation, HttpMessageHandler? handler = null)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".msix");
        using var client = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromMinutes(15) };
        try
        {
            using var response = await client.GetAsync(product.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != "https") throw new InvalidDataException("HTTPS required.");
            const long maximum = 1024L * 1024 * 1024;
            var length = response.Content.Headers.ContentLength;
            if (length > maximum) throw new InvalidDataException("Installer too large (max. 1 GB).");
            await using var input = await response.Content.ReadAsStreamAsync(cancellation);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long received = 0; int count;
                while ((count = await input.ReadAsync(buffer, cancellation)) > 0)
                {
                    received += count; if (received > maximum) throw new InvalidDataException("Installer too large.");
                    hash.AppendData(buffer, 0, count); await output.WriteAsync(buffer.AsMemory(0, count), cancellation);
                    if (length > 0) progress.Report(received * 100d / length.Value);
                }
            }
            if (!Convert.ToHexString(hash.GetHashAndReset()).Equals(product.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 mismatch.");
            ValidatePackage(path, product); return path;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
}
