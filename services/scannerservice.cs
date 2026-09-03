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

        // First run establishes a baseline.
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

        var libraries =
            await _jellyfinService
                .GetLibraries(cancellationToken)
                .ConfigureAwait(false);

        var librariesById =
            libraries.ToDictionary(
                x => x.Id,
                x => x,
                StringComparer.OrdinalIgnoreCase);


        // ============================================================
        // Movies
        // ============================================================

        if (_configuration.ScanMovies)
        {
            await ScanMovieCategory(
                _configuration.MovieLibraryIds,
                librariesById,
                result.NewMovies,
                isAnime: false,
                lastScanUtc.Value,
                scanStartedUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }


        // ============================================================
        // Series
        // ============================================================

        if (_configuration.ScanSeries)
        {
            await ScanSeriesCategory(
                _configuration.SeriesLibraryIds,
                librariesById,
                result.NewSeries,
                isAnime: false,
                lastScanUtc.Value,
                scanStartedUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }


        // ============================================================
        // Anime
        // ============================================================

        if (_configuration.ScanAnime)
        {
            await ScanSeriesCategory(
                _configuration.AnimeLibraryIds,
                librariesById,
                result.NewAnime,
                isAnime: true,
                lastScanUtc.Value,
                scanStartedUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }


        // ============================================================
        // Anime Movies
        // ============================================================

        if (_configuration.ScanAnimeMovies)
        {
            await ScanMovieCategory(
                _configuration.AnimeMovieLibraryIds,
                librariesById,
                result.NewAnimeMovies,
                isAnime: true,
                lastScanUtc.Value,
                scanStartedUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }


        // ============================================================
        // Collections
        // ============================================================

        if (_configuration.ScanCollections)
        {
            await ScanCollectionsCategory(
                _configuration.CollectionLibraryIds,
                result.NewCollections,
                lastScanUtc.Value,
                scanStartedUtc,
                cancellationToken)
                .ConfigureAwait(false);
        }


        // Save the scan START time rather than the finish time.
        // This prevents missing media added while the scan is running.
        SaveLastScanUtc(scanStartedUtc);

        return result;
    }


    // ============================================================
    // Movie category scanning
    // ============================================================

    private async Task ScanMovieCategory(
        string[]? libraryIds,
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        List<Movie> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids =
            NormalizeLibraryIds(libraryIds);

        /*
         * Backward compatibility:
         * no mapping = scan all movies server-wide.
         *
         * We only use this fallback for regular Movies.
         * Anime Movies should never fall back to all movies,
         * otherwise everything would be duplicated.
         */
        if (ids.Length == 0)
        {
            if (isAnime)
            {
                return;
            }

            var movies =
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
                library?.Name ??
                string.Empty;

            var movies =
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


    // ============================================================
    // Series category scanning
    // ============================================================

    private async Task ScanSeriesCategory(
        string[]? libraryIds,
        IReadOnlyDictionary<string, JellyfinLibrary> librariesById,
        List<Series> destination,
        bool isAnime,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids =
            NormalizeLibraryIds(libraryIds);

        /*
         * Backward compatibility:
         * no mapping = scan all series server-wide.
         *
         * Anime does NOT use that fallback because that would duplicate
         * every normal series into the Anime category.
         */
        if (ids.Length == 0)
        {
            if (isAnime)
            {
                return;
            }

            var series =
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
                library?.Name ??
                string.Empty;

            var series =
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


    // ============================================================
    // Collection scanning
    // ============================================================

    private async Task ScanCollectionsCategory(
        string[]? libraryIds,
        List<CollectionItem> destination,
        DateTime lastScanUtc,
        DateTime scanStartedUtc,
        CancellationToken cancellationToken)
    {
        string[] ids =
            NormalizeLibraryIds(libraryIds);

        if (ids.Length == 0)
        {
            var collections =
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
            var collections =
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


    // ============================================================
    // Add/filter helpers
    // ============================================================

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

            movie.IsAnime =
                isAnime;

            destination.Add(
                movie);
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

            item.IsAnime =
                isAnime;

            destination.Add(
                item);
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
            if (!IsNewItem(
                    collection.DateAdded,
                    lastScanUtc,
                    scanStartedUtc))
            {
                continue;
            }

            destination.Add(
                collection);
        }
    }


    // ============================================================
    // Duplicate protection
    // ============================================================

    private static void RemoveDuplicateMovies(
        List<Movie> movies)
    {
        var unique =
            movies
                .GroupBy(
                    x => x.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    x => x.First())
                .ToList();

        movies.Clear();
        movies.AddRange(unique);
    }


    private static void RemoveDuplicateSeries(
        List<Series> series)
    {
        var unique =
            series
                .GroupBy(
                    x => x.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    x => x.First())
                .ToList();

        series.Clear();
        series.AddRange(unique);
    }


    private static void RemoveDuplicateCollections(
        List<CollectionItem> collections)
    {
        var unique =
            collections
                .GroupBy(
                    x => x.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    x => x.First())
                .ToList();

        collections.Clear();
        collections.AddRange(unique);
    }


    // ============================================================
    // General helpers
    // ============================================================

    private static string[] NormalizeLibraryIds(
        string[]? libraryIds)
    {
        if (libraryIds is null ||
            libraryIds.Length == 0)
        {
            return Array.Empty<string>();
        }

        return libraryIds
            .Where(
                x => !string.IsNullOrWhiteSpace(x))
            .Select(
                x => x.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
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
                File.ReadAllText(
                    _lastScanFile);

            using JsonDocument document =
                JsonDocument.Parse(
                    json);

            if (document.RootElement.TryGetProperty(
                    "LastScanUtc",
                    out JsonElement lastScanElement) &&
                lastScanElement.TryGetDateTime(
                    out var date))
            {
                return date.ToUniversalTime();
            }

            // Backward compatibility.
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
            // Invalid state file.
            // The next scan establishes a clean baseline.
        }

        return null;
    }


    private void SaveLastScanUtc(
        DateTime value)
    {
        string? directory =
            Path.GetDirectoryName(
                _lastScanFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(
                directory);
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