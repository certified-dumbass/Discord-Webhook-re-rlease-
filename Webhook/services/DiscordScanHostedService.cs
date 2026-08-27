using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dreamstreaming.DiscordBot.Services;

/// <summary>
/// Lightweight scheduler for automatic scans.
/// It checks once per minute and runs only when the configured schedule is due.
/// </summary>
public sealed class DiscordScanHostedService : BackgroundService
{
    private readonly ILogger<DiscordScanHostedService> _logger;
    private DateTime _retryAfterUtc = DateTime.MinValue;

    public DiscordScanHostedService(
        ILogger<DiscordScanHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Give Jellyfin and the plugin instance time to finish starting.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken)
            .ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckScheduleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Dreamstreaming Discord Bot scheduler check failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task CheckScheduleAsync(
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;

        if (plugin is null)
        {
            return;
        }

        var configuration = plugin.Configuration;

        if (string.IsNullOrWhiteSpace(configuration.DiscordWebhook) ||
            string.IsNullOrWhiteSpace(configuration.JellyfinUrl) ||
            string.IsNullOrWhiteSpace(configuration.JellyfinApiKey))
        {
            return;
        }

        if (DateTime.UtcNow < _retryAfterUtc)
        {
            return;
        }

        Directory.CreateDirectory(plugin.StateDirectory);

        string stateFile =
            Path.Combine(plugin.StateDirectory, "scheduler-state.json");

        DateTime? lastRunLocal = LoadLastRunLocal(stateFile);
        DateTime nowLocal = DateTime.Now;

        if (lastRunLocal is null)
        {
            // Establish an anchor. This prevents a newly installed plugin from
            // unexpectedly firing a full scheduled run immediately at startup.
            SaveLastRunLocal(stateFile, nowLocal);
            return;
        }

        if (!IsDue(configuration.ScanIntervalHours, configuration.ScanDay, configuration.ScanTime, lastRunLocal.Value, nowLocal))
        {
            return;
        }

        _logger.LogInformation(
            "Dreamstreaming Discord Bot scheduled scan starting.");

        try
        {
            var coordinator = new ScanCoordinator();
            var result =
                await coordinator.RunScanAsync(
                    sendWhenEmpty: false,
                    cancellationToken)
                    .ConfigureAwait(false);

            SaveLastRunLocal(stateFile, nowLocal);
            _retryAfterUtc = DateTime.MinValue;

            _logger.LogInformation(
                "Dreamstreaming Discord Bot scheduled scan completed. New items: {Count}.",
                result.TotalNew);
        }
        catch (Exception ex)
        {
            _retryAfterUtc = DateTime.UtcNow.AddMinutes(10);

            _logger.LogError(
                ex,
                "Dreamstreaming Discord Bot scheduled scan failed. Retrying after 10 minutes.");
        }
    }

    private static bool IsDue(
        int intervalHours,
        int scanDay,
        string scanTime,
        DateTime lastRunLocal,
        DateTime nowLocal)
    {
        intervalHours = Math.Max(1, intervalHours);

        if (intervalHours >= 168)
        {
            if (!TimeSpan.TryParse(scanTime, out var configuredTime))
            {
                configuredTime = TimeSpan.FromHours(20);
            }

            int normalizedDay = Math.Clamp(scanDay, 0, 6);

            return (int)nowLocal.DayOfWeek == normalizedDay &&
                   nowLocal.TimeOfDay >= configuredTime &&
                   lastRunLocal.Date < nowLocal.Date;
        }

        return nowLocal >= lastRunLocal.AddHours(intervalHours);
    }

    private static DateTime? LoadLastRunLocal(string stateFile)
    {
        if (!File.Exists(stateFile))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(stateFile);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(
                    "LastRunLocal",
                    out var element) &&
                element.TryGetDateTime(out var value))
            {
                return value;
            }
        }
        catch
        {
            // Recreate state below.
        }

        return null;
    }

    private static void SaveLastRunLocal(
        string stateFile,
        DateTime value)
    {
        string? directory = Path.GetDirectoryName(stateFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            new { LastRunLocal = value },
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(stateFile, json);
    }
}
