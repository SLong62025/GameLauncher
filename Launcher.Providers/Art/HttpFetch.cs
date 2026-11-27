using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Providers.Art;

internal static class HttpFetch {
    public const int MaxBytes = 10 * 1024 * 1024;
    
    public static async Task<byte[]?> GetBytesAsync(
        HttpClient http, string url, CancellationToken ct, bool treatForbiddenAsMissing = false) {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return null;
        if (treatForbiddenAsMissing && response.StatusCode == HttpStatusCode.Forbidden) return null;

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxBytes) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0) {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxBytes) return null;
        }

        return buffer.ToArray();
    }
}
