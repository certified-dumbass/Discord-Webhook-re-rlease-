using System;
using System.IO;
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

        // The first run establishes a baseline instead of announcing the entire library.
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

        if (_configuration.ScanMovies)
        {
            var movies =
                await _jellyfinService.GetMovies(cancellationToken)
                    .ConfigureAwait(false);

            foreach (var movie in movies)
            {
                if (movie.DateAdded > lastScanUtc.Value &&
                    movie.DateAdded <= scanStartedUtc)
                {
                    result.NewMovies.Add(movie);
                }
            }
        }

        if (_configuration.ScanSeries)
        {
            var series =
                await _jellyfinService.GetSeries(cancellationToken)
                    .ConfigureAwait(false);

            foreach (var serie in series)
            {
                if (serie.DateAdded > lastScanUtc.Value &&
                    serie.DateAdded <= scanStartedUtc)
                {
                    result.NewSeries.Add(serie);
                }
            }
        }

        // Store the start time, not the finish time. This avoids missing media
        // that is added while a scan is running.
        SaveLastScanUtc(scanStartedUtc);

        return result;
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
            string json = File.ReadAllText(_lastScanFile);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(
                    "LastScanUtc",
                    out JsonElement lastScanElement) &&
                lastScanElement.TryGetDateTime(out var date))
            {
                return date.ToUniversalTime();
            }

            // Backward compatibility with the old LastScan property.
            if (document.RootElement.TryGetProperty(
                    "LastScan",
                    out lastScanElement) &&
                lastScanElement.TryGetDateTime(out date))
            {
                return date.ToUniversalTime();
            }
        }
        catch
        {
            // Invalid state file: establish a clean baseline on the next scan.
        }

        return null;
    }

    private void SaveLastScanUtc(DateTime value)
    {
        string? directory = Path.GetDirectoryName(_lastScanFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new
        {
            LastScanUtc = value.ToUniversalTime()
        };

        string json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_lastScanFile, json);
    }
}
