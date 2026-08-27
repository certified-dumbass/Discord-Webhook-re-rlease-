using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Models;

namespace Dreamstreaming.DiscordBot.Services;

public sealed class DiscordWebhookService : IDisposable
{
    private const int SafeDiscordMessageLength = 1900;

    private readonly string _webhookUrl;
    private readonly HttpClient _client;

    public DiscordWebhookService(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException(
                "Discord webhook URL cannot be empty.",
                nameof(webhookUrl));
        }

        if (!Uri.TryCreate(webhookUrl.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ArgumentException(
                "Discord webhook URL is invalid.",
                nameof(webhookUrl));
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "Discord webhook URL must use HTTPS.",
                nameof(webhookUrl));
        }

        if (!uri.Host.EndsWith("discord.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith("discordapp.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The webhook URL does not appear to be a Discord webhook.",
                nameof(webhookUrl));
        }

        _webhookUrl = webhookUrl.Trim();
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task SendScanResult(
        ScanResult result,
        CancellationToken cancellationToken = default)
    {
        foreach (string message in CreateMessages(result))
        {
            await SendMessage(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SendTestMessage(
        CancellationToken cancellationToken = default)
    {
        return SendMessage(
            "💜 **Dreamstreaming Discord Bot**\n\n" +
            "✅ Testbericht succesvol verzonden!",
            cancellationToken);
    }

    private async Task SendMessage(
        string message,
        CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync(
            _webhookUrl,
            new { content = message },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string responseBody =
                await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            throw new HttpRequestException(
                $"Discord returned {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}".Trim());
        }
    }

    private static IEnumerable<string> CreateMessages(ScanResult result)
    {
        if (result.BaselineInitialized)
        {
            yield return
                "💜 **Dreamstreaming Discord Bot**\n\n" +
                "✅ Scan-baseline aangemaakt. Nieuwe toevoegingen vanaf nu worden gemeld.";
            yield break;
        }

        var lines = new List<string>
        {
            "💜 **Dreamstreaming Weekly Update**",
            string.Empty,
            "🎬 **Movies**"
        };

        if (result.NewMovies.Count == 0)
        {
            lines.Add("Geen nieuwe films");
        }
        else
        {
            foreach (var movie in result.NewMovies)
            {
                lines.Add($"🍿 {movie.Name}{FormatYear(movie.Year)}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("📺 **Series**");

        if (result.NewSeries.Count == 0)
        {
            lines.Add("Geen nieuwe series");
        }
        else
        {
            foreach (var serie in result.NewSeries)
            {
                lines.Add($"📺 {serie.Name}{FormatYear(serie.Year)}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("🌙 Veel kijkplezier op Dreamstreaming!");

        var builder = new StringBuilder();

        foreach (string line in lines)
        {
            int extraLength = line.Length + (builder.Length > 0 ? 1 : 0);

            if (builder.Length > 0 &&
                builder.Length + extraLength > SafeDiscordMessageLength)
            {
                yield return builder.ToString();
                builder.Clear();
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(line);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static string FormatYear(int? year)
    {
        return year.HasValue ? $" ({year.Value})" : string.Empty;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
