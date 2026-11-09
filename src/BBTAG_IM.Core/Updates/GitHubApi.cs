using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BBTAG_IM.Core.Updates;

public sealed class GitHubApi(HttpClient http) : IGitHubApi
{
    public const string ClientName = "BBTAG-Improvement-Mod";

    private const string ApiHost = "https://api.github.com/repos/";
    private const string JsonMediaType = "application/vnd.github+json";
    private const string ShaMediaType = "application/vnd.github.sha";
    private const string ApiVersionHeader = "X-GitHub-Api-Version";
    private const string ApiVersion = "2022-11-28";
    private const string RemainingChecksHeader = "X-RateLimit-Remaining";
    private const string NoChecksLeft = "0";
    private const string BlobType = "blob";
    private const string RegularFileModePrefix = "100";
    private const int BufferLength = 1 << 16;

    public async Task<GitHubRelease?> LatestReleaseAsync(GitHubRepository repository, CancellationToken cancellation)
    {
        using var response = await SendAsync(Api(repository, "releases/latest"), JsonMediaType, cancellation);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        var release = await ReadAsync(response, repository, GitHubJson.Default.ReleaseResponse, cancellation);
        return new GitHubRelease(release.Tag ?? "", new Uri(release.Page ?? repository.Url), [.. (release.Assets ?? []).Select(ToAsset)]);
    }

    public async Task<string> HeadCommitAsync(GitHubRepository repository, CancellationToken cancellation)
    {
        using var response = await SendAsync(Api(repository, "commits/HEAD"), ShaMediaType, cancellation);
        EnsureFound(response, repository);
        return (await response.Content.ReadAsStringAsync(cancellation)).Trim();
    }

    public async Task<IReadOnlyList<GitTreeFile>> TreeAsync(GitHubRepository repository, string commit, CancellationToken cancellation)
    {
        using var response = await SendAsync(Api(repository, $"git/trees/{commit}?recursive=1"), JsonMediaType, cancellation);
        var tree = await ReadAsync(response, repository, GitHubJson.Default.TreeResponse, cancellation);

        if (tree.Truncated)
            throw new GitHubException($"{repository.Url} has more files than GitHub lists at once, so it cannot be followed. Ask its author for a release with a .zip.");

        return [.. (tree.Entries ?? []).Where(IsRegularFile).Select(entry => new GitTreeFile(entry.Path!, entry.Sha!, entry.Size ?? 0))];
    }

    public async Task DownloadAsync(Uri address, Stream target, IProgress<long> progress, CancellationToken cancellation)
    {
        using var response = await SendAsync(address, null, cancellation);

        if (!response.IsSuccessStatusCode)
            throw new GitHubException($"{address} could not be downloaded (GitHub answered {(int)response.StatusCode}).");

        try
        {
            await using var source = await response.Content.ReadAsStreamAsync(cancellation);
            await CopyAsync(source, target, progress, cancellation);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw new GitHubException($"The download of {address} stopped: {exception.Message}", exception);
        }
    }

    private static async Task CopyAsync(Stream source, Stream target, IProgress<long> progress, CancellationToken cancellation)
    {
        var buffer = new byte[BufferLength];
        long copied = 0;
        int read;

        while ((read = await source.ReadAsync(buffer, cancellation)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellation);
            copied += read;
            progress.Report(copied);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri address, string? mediaType, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(ClientName, null));

        if (mediaType is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            request.Headers.Add(ApiVersionHeader, ApiVersion);
        }

        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
        }
        catch (HttpRequestException exception)
        {
            throw new GitHubException($"GitHub could not be reached: {exception.Message}", exception);
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, GitHubRepository repository, JsonTypeInfo<T> typeInfo, CancellationToken cancellation)
    {
        EnsureFound(response, repository);

        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(cancellation);
            return await JsonSerializer.DeserializeAsync(body, typeInfo, cancellation) ?? throw Unreadable(repository);
        }
        catch (JsonException exception)
        {
            throw Unreadable(repository, exception);
        }
    }

    private static void EnsureFound(HttpResponseMessage response, GitHubRepository repository)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (IsRateLimited(response))
            throw new GitHubException("GitHub allows 60 checks an hour and they are used up. Try again later.");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new GitHubException($"{repository.Url} was not found. Check the link, and that the repository is public.");

        throw new GitHubException($"GitHub answered {(int)response.StatusCode} for {repository.Url}.");
    }

    private static bool IsRateLimited(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests
        && response.Headers.TryGetValues(RemainingChecksHeader, out var remaining)
        && remaining.FirstOrDefault() == NoChecksLeft;

    private static GitHubException Unreadable(GitHubRepository repository, Exception? exception = null) =>
        new($"GitHub sent something unexpected for {repository.Url}.", exception);

    private static bool IsRegularFile(TreeEntry entry) =>
        entry.Type == BlobType && entry.Path is not null && entry.Sha is not null && entry.Mode?.StartsWith(RegularFileModePrefix, StringComparison.Ordinal) == true;

    private static GitHubAsset ToAsset(ReleaseAsset asset) =>
        new(asset.Id, asset.Name ?? "", asset.Size, new Uri(asset.DownloadUrl ?? ""), asset.UpdatedAt);

    private static Uri Api(GitHubRepository repository, string path) => new($"{ApiHost}{repository}/{path}");
}
