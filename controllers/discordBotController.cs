using System;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dreamstreaming.DiscordBot.Controllers;

[ApiController]
[Route("Dreamstreaming/DiscordBot")]
[Authorize(Policy = "RequiresElevation")]
public class DiscordBotController : ControllerBase
{
    [HttpGet("Libraries")]
    public async Task<ActionResult> GetLibraries(
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;

        if (plugin is null)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        "Dreamstreaming Discord Bot plugin instance is unavailable."
                });
        }

        var configuration =
            plugin.Configuration;

        if (string.IsNullOrWhiteSpace(
                configuration.JellyfinUrl))
        {
            return BadRequest(
                new
                {
                    Message =
                        "Jellyfin URL is not configured."
                });
        }

        if (string.IsNullOrWhiteSpace(
                configuration.JellyfinApiKey))
        {
            return BadRequest(
                new
                {
                    Message =
                        "Jellyfin API key is not configured."
                });
        }

        try
        {
            using var jellyfinService =
                new JellyfinService(
                    configuration);

            var libraries =
                await jellyfinService
                    .GetLibraries(
                        cancellationToken)
                    .ConfigureAwait(false);

            return Ok(
                new
                {
                    Libraries =
                        libraries
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new
                {
                    Message =
                        exception.Message
                });
        }
        catch (Exception exception)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        $"Failed to load Jellyfin libraries: {exception.Message}"
                });
        }
    }


    [HttpPost("TestDiscord")]
    public async Task<ActionResult> TestDiscord(
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;

        if (plugin is null)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        "Dreamstreaming Discord Bot plugin instance is unavailable."
                });
        }

        var configuration =
            plugin.Configuration;

        if (string.IsNullOrWhiteSpace(
                configuration.DiscordWebhook))
        {
            return BadRequest(
                new
                {
                    Message =
                        "Discord webhook is not configured."
                });
        }

        try
        {
            using var discordService =
                new DiscordWebhookService(
                    configuration.DiscordWebhook,
                    configuration);

            await discordService
                .SendTestMessage(
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(
                new
                {
                    Message =
                        "Discord test message sent successfully."
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new
                {
                    Message =
                        exception.Message
                });
        }
        catch (Exception exception)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        $"Discord test failed: {exception.Message}"
                });
        }
    }


    [HttpPost("RunScanNow")]
    public async Task<ActionResult> RunScanNow(
        CancellationToken cancellationToken)
    {
        try
        {
            var coordinator =
                new ScanCoordinator();

            var result =
                await coordinator
                    .RunScanAsync(
                        sendWhenEmpty: true,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (result.BaselineInitialized)
            {
                return Ok(
                    new
                    {
                        Message =
                            "Scan baseline created successfully."
                    });
            }

            return Ok(
                new
                {
                    Message =
                        $"Scan completed successfully. " +
                        $"{result.TotalNew} new item(s) found.",

                    TotalNew =
                        result.TotalNew
                });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(
                new
                {
                    Message =
                        exception.Message
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new
                {
                    Message =
                        exception.Message
                });
        }
        catch (Exception exception)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        $"Manual scan failed: {exception.Message}"
                });
        }
    }


    [HttpPost("ResetBaseline")]
    public ActionResult ResetBaseline()
    {
        try
        {
            var coordinator =
                new ScanCoordinator();

            coordinator.ResetBaseline();

            return Ok(
                new
                {
                    Message =
                        "Scan baseline reset successfully. " +
                        "The next scan will create a new baseline."
                });
        }
        catch (Exception exception)
        {
            return StatusCode(
                500,
                new
                {
                    Message =
                        $"Failed to reset scan baseline: {exception.Message}"
                });
        }
    }
}