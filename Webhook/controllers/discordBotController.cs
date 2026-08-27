using System;
using System.Net.Http;
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
    [HttpPost("TestDiscord")]
    public async Task<ActionResult> TestDiscord(
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;

        if (plugin is null)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Dreamstreaming Discord Bot plugin instance is not available."
            });
        }

        var configuration = plugin.Configuration;

        if (string.IsNullOrWhiteSpace(configuration.DiscordWebhook))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Discord webhook is not configured. Save the plugin settings first."
            });
        }

        try
        {
            using var discordService =
                new DiscordWebhookService(configuration.DiscordWebhook);

            await discordService.SendTestMessage(cancellationToken)
                .ConfigureAwait(false);

            return Ok(new
            {
                Success = true,
                Message = "Discord webhook test succesvol verzonden."
            });
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Discord weigerde de webhook request: {ex.Message}"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Ongeldige Discord webhook: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Onverwachte fout tijdens Discord test: {ex.Message}"
            });
        }
    }

    [HttpPost("RunScanNow")]
    public async Task<ActionResult> RunScanNow(
        CancellationToken cancellationToken)
    {
        try
        {
            var coordinator = new ScanCoordinator();

            var result =
                await coordinator.RunScanAsync(
                    sendWhenEmpty: true,
                    cancellationToken)
                    .ConfigureAwait(false);

            return Ok(new
            {
                Success = true,
                result.BaselineInitialized,
                NewMovies = result.NewMovies.Count,
                NewSeries = result.NewSeries.Count,
                TotalNew = result.TotalNew,
                Message = result.BaselineInitialized
                    ? "Scan-baseline aangemaakt. Nieuwe toevoegingen worden vanaf nu gemeld."
                    : $"Scan voltooid. {result.TotalNew} nieuwe item(s) gevonden."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"HTTP fout tijdens scan: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Onverwachte fout tijdens scan: {ex.Message}"
            });
        }
    }

    [HttpPost("ResetBaseline")]
    public ActionResult ResetBaseline()
    {
        try
        {
            var coordinator = new ScanCoordinator();
            coordinator.ResetBaseline();

            return Ok(new
            {
                Success = true,
                Message = "Scan-baseline verwijderd. De volgende scan maakt een nieuwe baseline aan."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Baseline kon niet worden verwijderd: {ex.Message}"
            });
        }
    }
}
