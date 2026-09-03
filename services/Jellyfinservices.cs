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
    private const int LatestItemLimit = 250;

    private readonly string _jellyfinUrl;
    private readonly HttpClient _client;


    public JellyfinService(
        PluginConfiguration configuration)
        : this(
            configuration.JellyfinUrl,
            configuration.JellyfinApiKey)
    {
    }


    public JellyfinService(
        string jellyfinUrl,
        string apiKey)
    {
        if (string.IsNullOrWhiteSpace(jellyfinUrl))
        {
            throw new ArgumentException(
                "Jellyfin URL cannot be empty.",
                nameof(jellyfinUrl));
        }

        if (!Uri.TryCreate(
                jellyfinUrl.Trim(),
                UriKind.Absolute,
                out var uri))
        {
            throw new ArgumentException(
                "Jellyfin URL is invalid.",
                nameof(jellyfinUrl));
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
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


        _jellyfinUrl =
            jellyfinUrl
                .Trim()
                .TrimEnd('/');


        _client =
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(60)
            };


        _client.DefaultRequestHeaders.Add(
            "X-Emby-Token",
            apiKey.Trim());
    }


    // ============================================================
    // Libraries
    // ============================================================

    public async Task<List<JellyfinLibrary>> GetLibraries(
        CancellationToken cancellationToken = default)
    {
        string url =
            $"{_jellyfinUrl}/Library/VirtualFolders";


        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);


        response.EnsureSuccessStatusCode();


        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);


        using JsonDocument document =
            JsonDocument.Parse(json);


        var libraries =
            new List<JellyfinLibrary>();


        if (document.RootElement.ValueKind !=
            JsonValueKind.Array)
        {
            return libraries;
        }


        foreach (JsonElement item
                 in document.RootElement.EnumerateArray())
        {
            string id =
                GetString(
                    item,
                    "ItemId");


            string name =
                GetString(
                    item,
                    "Name");


            string collectionType =
                GetString(
                    item,
                    "CollectionType");


            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }


            libraries.Add(
                new JellyfinLibrary
                {
                    Id = id,
                    Name = name,
                    CollectionType = collectionType
                });
        }


        return libraries;
    }


    // ============================================================
    // Movies
    // ============================================================

    public Task<List<Movie>> GetMovies(
        CancellationToken cancellationToken = default)
    {
        return GetMovies(
            parentId: null,
            libraryName: string.Empty,
            cancellationToken);
    }


    public async Task<List<Movie>> GetMovies(
        string? parentId,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        string url =
            BuildLatestItemsUrl(
                "Movie",
                parentId);


        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);


        response.EnsureSuccessStatusCode();


        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);


        using JsonDocument document =
            JsonDocument.Parse(json);


        var movies =
            new List<Movie>();
        if (document.RootElement.ValueKind !=
            JsonValueKind.Array)
        {
            return movies;
        }


        foreach (JsonElement item
                 in document.RootElement.EnumerateArray())
        {
            DateTime dateAdded =
                TryGetDateAdded(item);


            movies.Add(
                new Movie
                {
                    Id =
                        GetString(
                            item,
                            "Id"),

                    Name =
                        GetString(
                            item,
                            "Name"),

                    DateAdded =
                        dateAdded,

                    Year =
                        TryGetInt(
                            item,
                            "ProductionYear"),

                    LibraryId =
                        parentId ??
                        string.Empty,

                    LibraryName =
                        libraryName
                });
        }


        return movies;
    }


    // ============================================================
    // Series
    // ============================================================

    public Task<List<Series>> GetSeries(
        CancellationToken cancellationToken = default)
    {
        return GetSeries(
            parentId: null,
            libraryName: string.Empty,
            cancellationToken);
    }


    public async Task<List<Series>> GetSeries(
        string? parentId,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        string url =
            BuildLatestItemsUrl(
                "Series",
                parentId);


        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);


        response.EnsureSuccessStatusCode();


        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);


        using JsonDocument document =
            JsonDocument.Parse(json);


        var series =
            new List<Series>();


        if (document.RootElement.ValueKind !=
            JsonValueKind.Array)
        {
            return series;
        }


        foreach (JsonElement item
                 in document.RootElement.EnumerateArray())
        {
            DateTime dateAdded =
                TryGetDateAdded(item);


            series.Add(
                new Series
                {
                    Id =
                        GetString(
                            item,
                            "Id"),

                    Name =
                        GetString(
                            item,
                            "Name"),

                    DateAdded =
                        dateAdded,

                    Year =
                        TryGetInt(
                            item,
                            "ProductionYear"),

                    LibraryId =
                        parentId ??
                        string.Empty,

                    LibraryName =
                        libraryName
                });
        }


        return series;
    }


    // ============================================================
    // Seasons
    // ============================================================

    public async Task<List<SeasonScanItem>> GetSeasons(
        string parentId,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        string url =
            BuildItemsUrl(
                "Season",
                parentId,
                "DateCreated,ProductionYear,SeriesId,SeriesName,IndexNumber");

        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        using JsonDocument document =
            JsonDocument.Parse(json);

        var seasons =
            new List<SeasonScanItem>();

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return seasons;
        }

        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            seasons.Add(
                new SeasonScanItem
                {
                    Id = GetString(item, "Id"),
                    SeriesId = GetString(item, "SeriesId"),
                    SeriesName = GetString(item, "SeriesName"),
                    Name = GetString(item, "Name"),
                    SeasonNumber = TryGetInt(item, "IndexNumber"),
                    DateAdded = TryGetDateAdded(item)
                });
        }

        return seasons;
    }


    // ============================================================
    // Episodes
    // ============================================================

    public async Task<List<EpisodeScanItem>> GetEpisodes(
        string parentId,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        string url =
            BuildItemsUrl(
                "Episode",
                parentId,
                "DateCreated,ProductionYear,SeriesId,SeriesName,SeasonId,SeasonName,ParentIndexNumber,IndexNumber");

        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        using JsonDocument document =
            JsonDocument.Parse(json);

        var episodes =
            new List<EpisodeScanItem>();

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return episodes;
        }

        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            episodes.Add(
                new EpisodeScanItem
                {
                    Id = GetString(item, "Id"),
                    SeriesId = GetString(item, "SeriesId"),
                    SeriesName = GetString(item, "SeriesName"),
                    SeasonId = GetString(item, "SeasonId"),
                    SeasonName = GetString(item, "SeasonName"),
                    SeasonNumber = TryGetInt(item, "ParentIndexNumber"),
                    EpisodeNumber = TryGetInt(item, "IndexNumber"),
                    Name = GetString(item, "Name"),
                    DateAdded = TryGetDateAdded(item)
                });
        }

        return episodes;
    }


    // ============================================================
    // Collections
    // ============================================================

    public Task<List<CollectionItem>> GetCollections(
        CancellationToken cancellationToken = default)
    {
        return GetCollections(
            parentId: null,
            cancellationToken);
    }


    public async Task<List<CollectionItem>> GetCollections(
        string? parentId,
        CancellationToken cancellationToken = default)
    {

        string url =
            BuildItemsUrl(
                "BoxSet",
                parentId);


        using var response =
            await _client
                .GetAsync(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);


        response.EnsureSuccessStatusCode();


        string json =
            await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);


        using JsonDocument document =
            JsonDocument.Parse(json);


        var collections =
            new List<CollectionItem>();


        if (!document.RootElement.TryGetProperty(
                "Items",
                out JsonElement items))
        {
            return collections;
        }


        foreach (JsonElement item
                 in items.EnumerateArray())
        {
            collections.Add(
                new CollectionItem
                {
                    Id =
                        GetString(
                            item,
                            "Id"),

                    Name =
                        GetString(
                            item,
                            "Name"),

                    DateAdded =
                        TryGetDateAdded(
                            item),

                    Year =
                        TryGetInt(
                            item,
                            "ProductionYear")
                });
        }


        return collections;
    }


    // ============================================================
    // Latest items URL
    // ============================================================

    private string BuildLatestItemsUrl(
        string itemType,
        string? parentId,
        string fields = "DateCreated,ProductionYear")
    {
        string url =
            $"{_jellyfinUrl}/Items/Latest" +
            $"?IncludeItemTypes={Uri.EscapeDataString(itemType)}" +
            $"&Limit={LatestItemLimit}" +
            $"&Fields={Uri.EscapeDataString(fields)}";


        if (!string.IsNullOrWhiteSpace(parentId))
        {
            url +=
                $"&ParentId={Uri.EscapeDataString(parentId)}";
        }


        return url;
    }


    // ============================================================
    // Normal Items URL
    // ============================================================

    private string BuildItemsUrl(
        string itemType,
        string? parentId,
        string fields = "DateCreated,ProductionYear")
    {
        string url =
            $"{_jellyfinUrl}/Items" +
            $"?Recursive=true" +
            $"&IncludeItemTypes={Uri.EscapeDataString(itemType)}" +
            $"&Fields={Uri.EscapeDataString(fields)}";


        if (!string.IsNullOrWhiteSpace(parentId))
        {
            url +=
                $"&ParentId={Uri.EscapeDataString(parentId)}";
        }


        return url;
    }


    // ============================================================
    // JSON helpers
    // ============================================================

    private static string GetString(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(
                name,
                out JsonElement value))
        {
            return string.Empty;
        }


        if (value.ValueKind !=
            JsonValueKind.String)
        {
            return string.Empty;
        }


        return value.GetString() ??
               string.Empty;
    }


    private static int? TryGetInt(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(
                name,
                out JsonElement value))
        {
            return null;
        }


        if (value.ValueKind ==
                JsonValueKind.Number &&
            value.TryGetInt32(
                out int result))
        {
            return result;
        }


        return null;
    }


    private static DateTime TryGetDateAdded(
        JsonElement element)
    {

        if (!element.TryGetProperty(
                "DateCreated",
                out JsonElement value))
        {
            return DateTime.MinValue;
        }


        if (value.ValueKind !=
            JsonValueKind.String)
        {
            return DateTime.MinValue;
        }


        if (!value.TryGetDateTime(
                out DateTime date))
        {
            return DateTime.MinValue;
        }


        return date.ToUniversalTime();
    }


    // ============================================================
    // Dispose
    // ============================================================

    public void Dispose()
    {
        _client.Dispose();
    }
}