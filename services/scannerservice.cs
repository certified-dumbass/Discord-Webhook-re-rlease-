using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Configuration;
using Dreamstreaming.DiscordBot.Models;

namespace Dreamstreaming.DiscordBot.Services;

public sealed class ScannerService
{
    private readonly JellyfinService _jellyfinService;
    private readonly PluginConfiguration _configuration;
    private readonly string _lastScanFile;

    public ScannerService(
        JellyfinService jellyfinService,
        PluginConfiguration configuration,
        string lastScanFile)
    {
        _jellyfinService = jellyfinService;
        _configuration = configuration;
        _lastScanFile = lastScanFile;
    }

    public async Task<ScanResult> ScanAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime scanStartedUtc = DateTime.UtcNow;
        DateTime? lastScanUtc = LoadLastScanUtc();

        if (lastScanUtc is null)
        {
            SaveLastScanUtc(scanStartedUtc);

            return new ScanResult
            {
                ScanDate = scanStartedUtc,
                BaselineInitialized = true
            };
        }

        var result = new ScanResult
        {
            ScanDate = scanStartedUtc
        };

        List<JellyfinLibrary> jellyfinLibraries =
            await _jellyfinService
                .GetLibraries(cancellationToken)
                .ConfigureAwait(false);

        var librariesById =
            jellyfinLibraries.ToDictionary(
                x => x.Id,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        DiscordLibraryConfiguration[] enabledLibraries =
            (_configuration.Libraries ?? Array.Empty<DiscordLibraryConfiguration>())
                .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.LibraryId))
                .ToArray();

        if (enabledLibraries.Length > 0)
        {
            await ScanDynamicLibraries(
                    enabledLibraries,
                    librariesById,
                    result,
                    lastScanUtc.Value,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await ScanLegacy(
                    librariesById,
                    result,
                    lastScanUtc.Value,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        SaveLastScanUtc(scanStartedUtc);

        return result;
    }

    private async Task ScanDynamicLibraries(
        IEnumerable<DiscordLibraryConfiguration> configuredLibraries,
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        ScanResult result,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        foreach (DiscordLibraryConfiguration configuredLibrary in configuredLibraries)
        {
            if (!librariesById.TryGetValue(
                    configuredLibrary.LibraryId,
                    out JellyfinLibrary? jellyfinLibrary))
            {
                continue;
            }

            string collectionType =
                string.IsNullOrWhiteSpace(jellyfinLibrary.CollectionType)
                    ? configuredLibrary.CollectionType
                    : jellyfinLibrary.CollectionType;

            var libraryResult = new LibraryScanResult
            {
                LibraryId = jellyfinLibrary.Id,
                LibraryName = jellyfinLibrary.Name,
                CollectionType = collectionType
            };

            if (collectionType.Equals(
                    "movies",
                    StringComparison.OrdinalIgnoreCase))
            {
                List<Movie> movies =
                    await _jellyfinService
                        .GetMovies(
                            jellyfinLibrary.Id,
                            jellyfinLibrary.Name,
                            cancellationToken)
                        .ConfigureAwait(false);

                AddNewMovies(
                    movies,
                    libraryResult.NewMovies,
                    isAnime: false,
                    lastScanUtc,
                    scanStartedUtc);

                RemoveDuplicateMovies(libraryResult.NewMovies);
            }
            else if (collectionType.Equals(
                         "tvshows",
                         StringComparison.OrdinalIgnoreCase))
            {
                List<Series> series =
                    await _jellyfinService
                        .GetSeries(
                            jellyfinLibrary.Id,
                            jellyfinLibrary.Name,
                            cancellationToken)
                        .ConfigureAwait(false);

                AddNewSeries(
                    series,
                    libraryResult.NewSeries,
                    isAnime: false,
                    lastScanUtc,
                    scanStartedUtc);

                RemoveDuplicateSeries(libraryResult.NewSeries);

                if (configuredLibrary.ScanSeasons)
                {
                    List<SeasonScanItem> seasons =
                        await _jellyfinService
                            .GetSeasons(
                                jellyfinLibrary.Id,
                                jellyfinLibrary.Name,
                                cancellationToken)
                            .ConfigureAwait(false);

                    AddNewSeasons(
                        seasons,
                        libraryResult.NewSeasons,
                        lastScanUtc,
                        scanStartedUtc);

                    RemoveDuplicateSeasons(libraryResult.NewSeasons);
                }

                if (configuredLibrary.ScanEpisodes)
                {
                    List<EpisodeScanItem> episodes =
                        await _jellyfinService
                            .GetEpisodes(
                                jellyfinLibrary.Id,
                                jellyfinLibrary.Name,
                                cancellationToken)
                            .ConfigureAwait(false);

                    AddNewEpisodes(
                        episodes,
                        libraryResult.NewEpisodes,
                        lastScanUtc,
                        scanStartedUtc);

                    RemoveDuplicateEpisodes(libraryResult.NewEpisodes);
                }
            }
            else if (collectionType.Equals(
                         "boxsets",
                         StringComparison.OrdinalIgnoreCase))
            {
                List<CollectionItem> collections =
                    await _jellyfinService
                        .GetCollections(
                            jellyfinLibrary.Id,
                            cancellationToken)
                        .ConfigureAwait(false);

                AddNewCollections(
                    collections,
                    libraryResult.NewCollections,
                    lastScanUtc,
                    scanStartedUtc);

                RemoveDuplicateCollections(libraryResult.NewCollections);
            }
            else
            {
                continue;
            }

            result.Libraries.Add(libraryResult);
        }
    }

    private async Task ScanLegacy(
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        ScanResult result,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        if (_configuration.ScanMovies)
        {
            await ScanMovieCategory(
                    _configuration.MovieLibraryIds,
                    librariesById,
                    result.NewMovies,
                    isAnime: false,
                    lastScanUtc,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (_configuration.ScanSeries)
        {
            await ScanSeriesCategory(
                    _configuration.SeriesLibraryIds,
                    librariesById,
                    result.NewSeries,
                    isAnime: false,
                    lastScanUtc,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (_configuration.ScanAnime)
        {
            await ScanSeriesCategory(
                    _configuration.AnimeLibraryIds,
                    librariesById,
                    result.NewAnime,
                    isAnime: true,
                    lastScanUtc,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (_configuration.ScanAnimeMovies)
        {
            await ScanMovieCategory(
                    _configuration.AnimeMovieLibraryIds,
                    librariesById,
                    result.NewAnimeMovies,
                    isAnime: true,
                    lastScanUtc,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (_configuration.ScanCollections)
        {
            await ScanCollectionsCategory(
                    _configuration.CollectionLibraryIds,
                    result.NewCollections,
                    lastScanUtc,
                    scanStartedUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ScanMovieCategory(
        string[]? libraryIds,
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        List<Movie> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids = NormalizeLibraryIds(libraryIds);

        if (ids.Length == 0)
        {
            if (isAnime)
            {
                return;
            }

            List<Movie> movies =
                await _jellyfinService
                    .GetMovies(cancellationToken)
                    .ConfigureAwait(false);

            AddNewMovies(
                movies,
                destination,
                isAnime,
                lastScanUtc,
                scanStartedUtc);

            return;
        }

        foreach (string libraryId in ids)
        {
            librariesById.TryGetValue(
                libraryId,
                out JellyfinLibrary? library);

            string libraryName =
                library?.Name ?? string.Empty;

            List<Movie> movies =
                await _jellyfinService
                    .GetMovies(
                        libraryId,
                        libraryName,
                        cancellationToken)
                    .ConfigureAwait(false);

            AddNewMovies(
                movies,
                destination,
                isAnime,
                lastScanUtc,
                scanStartedUtc);
        }

        RemoveDuplicateMovies(destination);
    }

    private async Task ScanSeriesCategory(
        string[]? libraryIds,
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        List<Series> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids = NormalizeLibraryIds(libraryIds);

        if (ids.Length == 0)
        {
            if (isAnime)
            {
                return;
            }

            List<Series> series =
                await _jellyfinService
                    .GetSeries(cancellationToken)
                    .ConfigureAwait(false);

            AddNewSeries(
                series,
                destination,
                isAnime,
                lastScanUtc,
                scanStartedUtc);

            return;
        }

        foreach (string libraryId in ids)
        {
            librariesById.TryGetValue(
                libraryId,
                out JellyfinLibrary? library);

            string libraryName =
                library?.Name ?? string.Empty;

            List<Series> series =
                await _jellyfinService
                    .GetSeries(
                        libraryId,
                        libraryName,
                        cancellationToken)
                    .ConfigureAwait(false);

            AddNewSeries(
                series,
                destination,
                isAnime,
                lastScanUtc,
                scanStartedUtc);
        }

        RemoveDuplicateSeries(destination);
    }

    private async Task ScanCollectionsCategory(
        string[]? libraryIds,
        List<CollectionItem> destination,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids = NormalizeLibraryIds(libraryIds);

        if (ids.Length == 0)
        {
            List<CollectionItem> collections =
                await _jellyfinService
                    .GetCollections(cancellationToken)
                    .ConfigureAwait(false);

            AddNewCollections(
                collections,
                destination,
                lastScanUtc,
                scanStartedUtc);

            return;
        }

        foreach (string libraryId in ids)
        {
            List<CollectionItem> collections =
                await _jellyfinService
                    .GetCollections(
                        libraryId,
                        cancellationToken)
                    .ConfigureAwait(false);

            AddNewCollections(
                collections,
                destination,
                lastScanUtc,
                scanStartedUtc);
        }

        RemoveDuplicateCollections(destination);
    }

    private static void AddNewMovies(
        IEnumerable<Movie> movies,
        List<Movie> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        foreach (Movie movie in movies)
        {
            if (!IsNewItem(
                    movie.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                continue;
            }

            movie.IsAnime = isAnime;
            destination.Add(movie);
        }
    }

    private static void AddNewSeries(
        IEnumerable<Series> series,
        List<Series> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        foreach (Series item in series)
        {
            if (!IsNewItem(
                    item.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                continue;
            }

            item.IsAnime = isAnime;
            destination.Add(item);
        }
    }

    private static void AddNewSeasons(
        IEnumerable<SeasonScanItem> seasons,
        List<SeasonScanItem> destination,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        foreach (SeasonScanItem season in seasons)
        {
            if (IsNewItem(
                    season.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                destination.Add(season);
            }
        }
    }

    private static void AddNewEpisodes(
        IEnumerable<EpisodeScanItem> episodes,
        List<EpisodeScanItem> destination,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        foreach (EpisodeScanItem episode in episodes)
        {
            if (IsNewItem(
                    episode.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                destination.Add(episode);
            }
        }
    }

    private static void AddNewCollections(
        IEnumerable<CollectionItem> collections,
        List<CollectionItem> destination,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        foreach (CollectionItem collection in collections)
        {
            if (IsNewItem(
                    collection.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                destination.Add(collection);
            }
        }
    }

    private static void RemoveDuplicateMovies(
        List<Movie> movies)
    {
        ReplaceWithUniqueById(
            movies,
            x => x.Id);
    }

    private static void RemoveDuplicateSeries(
        List<Series> series)
    {
        ReplaceWithUniqueById(
            series,
            x => x.Id);
    }

    private static void RemoveDuplicateSeasons(
        List<SeasonScanItem> seasons)
    {
        ReplaceWithUniqueById(
            seasons,
            x => x.Id);
    }

    private static void RemoveDuplicateEpisodes(
        List<EpisodeScanItem> episodes)
    {
        ReplaceWithUniqueById(
            episodes,
            x => x.Id);
    }

    private static void RemoveDuplicateCollections(
        List<CollectionItem> collections)
    {
        ReplaceWithUniqueById(
            collections,
            x => x.Id);
    }

    private static void ReplaceWithUniqueById<T>(
        List<T> items,
        Func<T, string> idSelector)
    {
        List<T> unique =
            items
                .GroupBy(
                    idSelector,
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

        items.Clear();
        items.AddRange(unique);
    }

    private static string[] NormalizeLibraryIds(
        string[]? libraryIds)
    {
        if (libraryIds is null ||
            libraryIds.Length == 0)
        {
            return Array.Empty<string>();
        }

        return libraryIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsNewItem(
        DateTime dateAdded,
        DateTime lastScanUtc,
        DateTime scanStartedUtc)
    {
        return
            dateAdded > lastScanUtc &&
            dateAdded <= scanStartedUtc;
    }

    public void ResetBaseline()
    {
        if (File.Exists(_lastScanFile))
        {
            File.Delete(_lastScanFile);
        }
    }

    private DateTime? LoadLastScanUtc()
    {
        if (!File.Exists(_lastScanFile))
        {
            return null;
        }

        try
        {
            string json =
                File.ReadAllText(_lastScanFile);

            using JsonDocument document =
                JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(
                    "LastScanUtc",
                    out JsonElement lastScanElement) &&
                lastScanElement.TryGetDateTime(
                    out DateTime date))
            {
                return date.ToUniversalTime();
            }

            if (document.RootElement.TryGetProperty(
                    "LastScan",
                    out lastScanElement) &&
                lastScanElement.TryGetDateTime(
                    out date))
            {
                return date.ToUniversalTime();
            }
        }
        catch
        {
        }

        return null;
    }

    private void SaveLastScanUtc(
        DateTime value)
    {
        string? directory =
            Path.GetDirectoryName(_lastScanFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data =
            new
            {
                LastScanUtc =
                    value.ToUniversalTime()
            };

        string json =
            JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            _lastScanFile,
            json);
    }
}
