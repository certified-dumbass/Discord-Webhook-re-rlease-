using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Configuration;
using Dreamstreaming.DiscordBot.Models;

namespace Dreamstreaming.DiscordBot.Services;

public sealed class JellyfinService : IDisposable
{
    private readonly string _jellyfinUrl;
    private readonly HttpClient _client;

    public JellyfinService(PluginConfiguration configuration)
        : this(configuration.JellyfinUrl, configuration.JellyfinApiKey)
    {
    }

    public JellyfinService(string jellyfinUrl, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(jellyfinUrl))
        {
            throw new ArgumentException(
                "Jellyfin URL cannot be empty.",
                nameof(jellyfinUrl));
        }

        if (!Uri.TryCreate(jellyfinUrl.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ArgumentException(
                "Jellyfin URL is invalid.",
                nameof(jellyfinUrl));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "Jellyfin URL must use HTTP or HTTPS.",
                nameof(jellyfinUrl));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "Jellyfin API key cannot be empty.",
                nameof(apiKey));
        }

        _jellyfinUrl = jellyfinUrl.Trim().TrimEnd('/');
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _client.DefaultRequestHeaders.Add(
            "X-Emby-Token",
            apiKey.Trim());
    }

    public async Task<List<Movie>> GetMovies(
        CancellationToken cancellationToken = default)
    {
        var movies = new List<Movie>();

        string url =
            $"{_jellyfinUrl}/Items?Recursive=true&IncludeItemTypes=Movie&Fields=DateCreated,ProductionYear";

        using var response =
            await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("Items", out var items))
        {
            return movies;
        }

        foreach (var item in items.EnumerateArray())
        {
            var dateAdded = TryGetDateAdded(item);

            movies.Add(new Movie
            {
                Id = GetString(item, "Id"),
                Name = GetString(item, "Name"),
                DateAdded = dateAdded,
                Year = TryGetInt(item, "ProductionYear")
            });
        }

        return movies;
    }

    public async Task<List<Series>> GetSeries(
        CancellationToken cancellationToken = default)
    {
        var series = new List<Series>();

        string url =
            $"{_jellyfinUrl}/Items?Recursive=true&IncludeItemTypes=Series&Fields=DateCreated,ProductionYear";

        using var response =
            await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("Items", out var items))
        {
            return series;
        }

        foreach (var item in items.EnumerateArray())
        {
            var dateAdded = TryGetDateAdded(item);

            series.Add(new Series
            {
                Id = GetString(item, "Id"),
                Name = GetString(item, "Name"),
                DateAdded = dateAdded,
                Year = TryGetInt(item, "ProductionYear")
            });
        }

        return series;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int? TryGetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result)
            ? result
            : null;
    }

    private static DateTime TryGetDateAdded(JsonElement element)
    {
        if (!element.TryGetProperty("DateCreated", out var value))
        {
            return DateTime.MinValue;
        }

        if (!value.TryGetDateTime(out var date))
        {
            return DateTime.MinValue;
        }

        return date.ToUniversalTime();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
