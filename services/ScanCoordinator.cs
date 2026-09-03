using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Models;

namespace Dreamstreaming.DiscordBot.Services;

/// <summary>
/// Connects Jellyfin scanning, scan state and Discord delivery into one flow.
/// </summary>
public sealed class ScanCoordinator
{
    private static readonly SemaphoreSlim ScanLock = new(1, 1);

    public async Task<ScanResult> RunScanAsync(
        bool sendWhenEmpty,
        CancellationToken cancellationToken = default)
    {
        var plugin = Plugin.Instance ??
            throw new InvalidOperationException(
                "Dreamstreaming Discord Bot plugin instance is unavailable.");

        var configuration = plugin.Configuration;

        ValidateConfiguration(
            configuration.JellyfinUrl,
            configuration.JellyfinApiKey,
            configuration.DiscordWebhook);

        await ScanLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            Directory.CreateDirectory(
                plugin.StateDirectory);

            string lastScanFile =
                Path.Combine(
                    plugin.StateDirectory,
                    "lastscan.json");

            using var jellyfinService =
                new JellyfinService(
                    configuration);

            var scanner =
                new ScannerService(
                    jellyfinService,
                    configuration,
                    lastScanFile);

            ScanResult result =
                await scanner
                    .ScanAsync(cancellationToken)
                    .ConfigureAwait(false);

            bool shouldSend =
                result.BaselineInitialized ||
                result.TotalNew > 0 ||
                sendWhenEmpty;

            if (shouldSend)
            {
                using var discordService =
                    new DiscordWebhookService(
                        configuration.DiscordWebhook,
                        configuration);

                await discordService
                    .SendScanResult(
                        result,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        finally
        {
            ScanLock.Release();
        }
    }

    public void ResetBaseline()
    {
        var plugin = Plugin.Instance ??
            throw new InvalidOperationException(
                "Dreamstreaming Discord Bot plugin instance is unavailable.");

        string lastScanFile =
            Path.Combine(
                plugin.StateDirectory,
                "lastscan.json");

        if (File.Exists(lastScanFile))
        {
            File.Delete(lastScanFile);
        }
    }

    private static void ValidateConfiguration(
        string jellyfinUrl,
        string jellyfinApiKey,
        string discordWebhook)
    {
        if (string.IsNullOrWhiteSpace(jellyfinUrl))
        {
            throw new InvalidOperationException(
                "Jellyfin URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jellyfinApiKey))
        {
            throw new InvalidOperationException(
                "Jellyfin API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(discordWebhook))
        {
            throw new InvalidOperationException(
                "Discord webhook is not configured.");
        }
    }
}