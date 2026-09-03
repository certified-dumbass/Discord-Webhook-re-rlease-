using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dreamstreaming.DiscordBot.Configuration;
using Dreamstreaming.DiscordBot.Models;

namespace Dreamstreaming.DiscordBot.Services;

public sealed class DiscordWebhookService : IDisposable
{
    private const int SafeDiscordMessageLength = 1900;

    private readonly string _webhookUrl;
    private readonly PluginConfiguration _configuration;
    private readonly HttpClient _client;


    public DiscordWebhookService(
        string webhookUrl,
        PluginConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException(
                "Discord webhook URL cannot be empty.",
                nameof(webhookUrl));
        }

        if (!Uri.TryCreate(
                webhookUrl.Trim(),
                UriKind.Absolute,
                out var uri))
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

        if (!uri.Host.EndsWith(
                "discord.com",
                StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith(
                "discordapp.com",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The webhook URL does not appear to be a Discord webhook.",
                nameof(webhookUrl));
        }

        _webhookUrl =
            webhookUrl.Trim();

        _configuration =
            configuration;

        _client =
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(30)
            };
    }


    // ============================================================
    // Public methods
    // ============================================================

    public async Task SendScanResult(
        ScanResult result,
        CancellationToken cancellationToken = default)
    {
        foreach (string message in CreateMessages(result))
        {
            await SendMessage(
                    message,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }


    public Task SendTestMessage(
        CancellationToken cancellationToken = default)
    {
        return SendMessage(
            "💜 **Dreamstreaming Discord Bot**\n\n" +
            "✅ Test successful!\n" +
            "Your Discord webhook is configured correctly.\n\n" +
            "Jellyfin updates will be posted to this channel.",
            cancellationToken);
    }


    // ============================================================
    // Discord delivery
    // ============================================================

    private async Task SendMessage(
        string message,
        CancellationToken cancellationToken)
    {
        using var response =
            await _client.PostAsJsonAsync(
                    _webhookUrl,
                    new
                    {
                        content = message
                    },
                    cancellationToken)
                .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string responseBody =
                await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            throw new HttpRequestException(
                $"Discord returned {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. {responseBody}".Trim());
        }
    }


    // ============================================================
    // Message creation
    // ============================================================

    private IEnumerable<string> CreateMessages(
        ScanResult result)
    {
        if (result.BaselineInitialized)
        {
            yield return
                "💜 **Dreamstreaming Discord Bot**\n\n" +
                "✅ Scan baseline created successfully.\n" +
                "Newly added content will be announced from now on.";

            yield break;
        }


        int totalCount =
            GetTotalCount(result);


        if (totalCount == 0)
        {
            string emptyMessage =
                string.IsNullOrWhiteSpace(
                    _configuration.EmptyScanTemplate)

                    ? "🔍 Scan complete — nothing new this time."

                    : _configuration.EmptyScanTemplate;


            yield return emptyMessage;

            yield break;
        }


        string renderedMessage =
            RenderUpdateMessage(result);


        foreach (string chunk in
                 SplitMessage(renderedMessage))
        {
            yield return chunk;
        }
    }


    // ============================================================
    // Main renderer
    // ============================================================

    private string RenderUpdateMessage(
        ScanResult result)
    {
        string template =
            GetSelectedTemplate();


        string schedule =
            GetScheduleText();


        var sections =
            BuildSections(result);


        string orderedCategories =
            BuildOrderedCategoryBlock(
                sections);


        int totalCount =
            GetTotalCount(result);


        /*
         * The category placeholders are replaced as ONE ordered block.
         *
         * This allows the configured category order to actually control
         * the position of Movies, Series, Anime, Anime Movies and
         * Collections.
         */
        template =
            ReplaceCategoryPlaceholdersWithOrderedBlock(
                template,
                orderedCategories);


        var replacements =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["{schedule}"] =
                    _configuration.ShowScanFrequency
                        ? schedule
                        : string.Empty,

                ["{date}"] =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd"),

                ["{count}"] =
                    _configuration.ShowTotalCount
                        ? totalCount.ToString()
                        : string.Empty
            };


        string output =
            template;


        foreach (var replacement in replacements)
        {
            output =
                output.Replace(
                    replacement.Key,
                    replacement.Value,
                    StringComparison.OrdinalIgnoreCase);
        }


        return CleanMessage(output);
    }


    // ============================================================
    // Build category sections
    // ============================================================

    private Dictionary<string, string> BuildSections(
        ScanResult result)
    {
        return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Movies"] =
                _configuration.ScanMovies
                    ? BuildMoviesSection(
                        result.NewMovies)
                    : string.Empty,

            ["Series"] =
                _configuration.ScanSeries
                    ? BuildSeriesSection(
                        result.NewSeries)
                    : string.Empty,

            ["Anime"] =
                _configuration.ScanAnime
                    ? BuildAnimeSection(
                        result.NewAnime)
                    : string.Empty,

            ["AnimeMovies"] =
                _configuration.ScanAnimeMovies
                    ? BuildAnimeMoviesSection(
                        result.NewAnimeMovies)
                    : string.Empty,

            ["Collections"] =
                _configuration.ScanCollections
                    ? BuildCollectionsSection(
                        result.NewCollections)
                    : string.Empty
        };
    }


    // ============================================================
    // Movies
    // ============================================================

    private string BuildMoviesSection(
        IReadOnlyCollection<Movie> movies)
    {
        if (movies.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : "🎬 **Movies**\nNo new movies found.";
        }


        var builder =
            new StringBuilder();


        builder.AppendLine(
            "🎬 **Movies**");


        foreach (Movie movie in movies)
        {
            builder.Append(
                "🍿 ");

            builder.Append(
                movie.Name);


            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(movie.Year));
            }


            builder.AppendLine();
        }


        return builder
            .ToString()
            .TrimEnd();
    }


    // ============================================================
    // Series
    // ============================================================

    private string BuildSeriesSection(
        IReadOnlyCollection<Series> series)
    {
        if (series.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : "📺 **Series**\nNo new series found.";
        }


        var builder =
            new StringBuilder();


        builder.AppendLine(
            "📺 **Series**");


        foreach (Series serie in series)
        {
            builder.Append(
                "📺 ");

            builder.Append(
                serie.Name);


            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(serie.Year));
            }


            builder.AppendLine();
        }


        return builder
            .ToString()
            .TrimEnd();
    }


    // ============================================================
    // Anime
    // ============================================================

    private string BuildAnimeSection(
        IReadOnlyCollection<Series> anime)
    {
        if (anime.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : "🌸 **Anime**\nNo new anime found.";
        }


        var builder =
            new StringBuilder();


        builder.AppendLine(
            "🌸 **Anime**");


        foreach (Series serie in anime)
        {
            builder.Append(
                "🌸 ");

            builder.Append(
                serie.Name);


            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(serie.Year));
            }


            builder.AppendLine();
        }


        return builder
            .ToString()
            .TrimEnd();
    }


    // ============================================================
    // Anime Movies
    // ============================================================

    private string BuildAnimeMoviesSection(
        IReadOnlyCollection<Movie> movies)
    {
        if (movies.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : "🎌 **Anime Movies**\nNo new anime movies found.";
        }


        var builder =
            new StringBuilder();


        builder.AppendLine(
            "🎌 **Anime Movies**");


        foreach (Movie movie in movies)
        {
            builder.Append(
                "🎞️ ");

            builder.Append(
                movie.Name);


            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(movie.Year));
            }


            builder.AppendLine();
        }


        return builder
            .ToString()
            .TrimEnd();
    }


    // ============================================================
    // Collections
    // ============================================================

    private string BuildCollectionsSection(
        IReadOnlyCollection<CollectionItem> collections)
    {
        if (collections.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : "📚 **Collections**\nNo new collections found.";
        }


        var builder =
            new StringBuilder();


        builder.AppendLine(
            "📚 **Collections**");


        foreach (CollectionItem collection in collections)
        {
            builder.Append(
                "📦 ");

            builder.Append(
                collection.Name);


            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(collection.Year));
            }


            builder.AppendLine();
        }


        return builder
            .ToString()
            .TrimEnd();
    }


    // ============================================================
    // Category ordering
    // ============================================================

    private string BuildOrderedCategoryBlock(
        IReadOnlyDictionary<string, string> sections)
    {
        string[] order =
            GetEffectiveCategoryOrder();


        var builder =
            new StringBuilder();


        foreach (string category in order)
        {
            if (!sections.TryGetValue(
                    category,
                    out string? section))
            {
                continue;
            }


            if (string.IsNullOrWhiteSpace(section))
            {
                continue;
            }


            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }


            builder.Append(section);
        }


        return builder.ToString();
    }


    private string ReplaceCategoryPlaceholdersWithOrderedBlock(
        string template,
        string orderedCategories)
    {
        string[] placeholders =
        [
            "{movies}",
            "{series}",
            "{anime}",
            "{anime_movies}",
            "{collections}"
        ];


        int firstPosition =
            -1;


        string? firstPlaceholder =
            null;


        foreach (string placeholder in placeholders)
        {
            int position =
                template.IndexOf(
                    placeholder,
                    StringComparison.OrdinalIgnoreCase);


            if (position < 0)
            {
                continue;
            }


            if (firstPosition == -1 ||
                position < firstPosition)
            {
                firstPosition =
                    position;

                firstPlaceholder =
                    placeholder;
            }
        }


        /*
         * Custom templates do not have to contain category placeholders.
         * If none are present, append the ordered categories.
         */
        if (firstPlaceholder is null)
        {
            if (string.IsNullOrWhiteSpace(
                    orderedCategories))
            {
                return template;
            }

            return
                template.TrimEnd() +
                "\n\n" +
                orderedCategories;
        }


        string output =
            template;


        bool inserted =
            false;


        /*
         * Replace whichever category placeholder appears FIRST
         * with the complete ordered category block.
         *
         * Every other category placeholder is removed.
         */
        foreach (string placeholder in placeholders)
        {
            int position =
                output.IndexOf(
                    placeholder,
                    StringComparison.OrdinalIgnoreCase);


            if (position < 0)
            {
                continue;
            }


            if (placeholder.Equals(
                    firstPlaceholder,
                    StringComparison.OrdinalIgnoreCase) &&
                !inserted)
            {
                output =
                    output.Replace(
                        placeholder,
                        orderedCategories,
                        StringComparison.OrdinalIgnoreCase);

                inserted =
                    true;
            }
            else
            {
                output =
                    output.Replace(
                        placeholder,
                        string.Empty,
                        StringComparison.OrdinalIgnoreCase);
            }
        }


        return output;
    }


    private string[] GetEffectiveCategoryOrder()
    {
        /*
         * MostWatched is the next stage.
         *
         * Until Jellyfin usage statistics are connected,
         * MostWatched safely falls back to the configured
         * manual order.
         */

        string[] configuredOrder =
            _configuration.ManualCategoryOrder
            ?? Array.Empty<string>();


        string[] validCategories =
        [
            "Anime",
            "Movies",
            "Series",
            "AnimeMovies",
            "Collections"
        ];


        var cleanedOrder =
            configuredOrder
                .Where(
                    category =>
                        validCategories.Contains(
                            category,
                            StringComparer.OrdinalIgnoreCase))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();


        /*
         * Add missing categories automatically.
         * This protects older configuration files.
         */
        foreach (string category in validCategories)
        {
            if (!cleanedOrder.Contains(
                    category,
                    StringComparer.OrdinalIgnoreCase))
            {
                cleanedOrder.Add(
                    category);
            }
        }


        return cleanedOrder.ToArray();
    }


    // ============================================================
    // Count
    // ============================================================

    private int GetTotalCount(
        ScanResult result)
    {
        int total =
            0;


        if (_configuration.ScanMovies)
        {
            total +=
                result.NewMovies.Count;
        }


        if (_configuration.ScanSeries)
        {
            total +=
                result.NewSeries.Count;
        }


        if (_configuration.ScanAnime)
        {
            total +=
                result.NewAnime.Count;
        }


        if (_configuration.ScanAnimeMovies)
        {
            total +=
                result.NewAnimeMovies.Count;
        }


        if (_configuration.ScanCollections)
        {
            total +=
                result.NewCollections.Count;
        }


        return total;
    }


    // ============================================================
    // Templates
    // ============================================================

    private string GetSelectedTemplate()
    {
        string style =
            _configuration.MessageStyle?.Trim()
            ?? "Default";


        if (style.Equals(
                "Custom",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(
                    _configuration.MessageTemplate))
            {
                return _configuration.MessageTemplate;
            }


            return GetDefaultTemplate();
        }


        if (style.Equals(
                "Casual",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Hey everyone! 👋\n\n" +
                "Here is your {schedule} Dreamstreaming update.\n\n" +
                "{movies}\n" +
                "{series}\n" +
                "{anime}\n" +
                "{anime_movies}\n" +
                "{collections}\n\n" +
                "{count} new additions just dropped. Have fun watching! 💜";
        }


        if (style.Equals(
                "Compact",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "💜 **Dreamstreaming Update**\n\n" +
                "{movies}\n" +
                "{series}\n" +
                "{anime}\n" +
                "{anime_movies}\n" +
                "{collections}";
        }


        return GetDefaultTemplate();
    }


    private static string GetDefaultTemplate()
    {
        return
            "💜 **Dreamstreaming Library Update**\n\n" +
            "Here is your {schedule} update on newly added content.\n\n" +
            "{movies}\n" +
            "{series}\n" +
            "{anime}\n" +
            "{anime_movies}\n" +
            "{collections}\n\n" +
            "✅ {count} new additions.\n\n" +
            "🌙 Enjoy watching on Dreamstreaming!";
    }


    // ============================================================
    // Schedule text
    // ============================================================

    private string GetScheduleText()
    {
        return _configuration.ScanIntervalHours switch
        {
            1 =>
                "hourly",

            2 =>
                "2-hour",

            4 =>
                "4-hour",

            6 =>
                "6-hour",

            12 =>
                "12-hour",

            24 =>
                "daily",

            168 =>
                "weekly",

            _ =>
                $"every {_configuration.ScanIntervalHours} hours"
        };
    }


    // ============================================================
    // Message cleanup
    // ============================================================

    private static string CleanMessage(
        string message)
    {
        string[] lines =
            message
                .Replace(
                    "\r\n",
                    "\n")
                .Split('\n');


        var cleaned =
            new List<string>();


        bool previousWasEmpty =
            false;


        foreach (string rawLine in lines)
        {
            string line =
                rawLine.TrimEnd();


            bool isEmpty =
                string.IsNullOrWhiteSpace(
                    line);


            if (isEmpty &&
                previousWasEmpty)
            {
                continue;
            }


            cleaned.Add(
                line);


            previousWasEmpty =
                isEmpty;
        }


        return string.Join(
                '\n',
                cleaned)
            .Trim();
    }


    // ============================================================
    // Discord message splitting
    // ============================================================

    private static IEnumerable<string> SplitMessage(
        string message)
    {
        if (message.Length <=
            SafeDiscordMessageLength)
        {
            yield return message;

            yield break;
        }


        string[] lines =
            message.Split('\n');


        var builder =
            new StringBuilder();


        foreach (string line in lines)
        {
            int extraLength =
                line.Length +
                (builder.Length > 0
                    ? 1
                    : 0);


            if (builder.Length > 0 &&
                builder.Length + extraLength >
                SafeDiscordMessageLength)
            {
                yield return
                    builder.ToString();

                builder.Clear();
            }


            if (builder.Length > 0)
            {
                builder.Append('\n');
            }


            builder.Append(
                line);
        }


        if (builder.Length > 0)
        {
            yield return
                builder.ToString();
        }
    }


    // ============================================================
    // Formatting
    // ============================================================

    private static string FormatYear(
        int? year)
    {
        return year.HasValue
            ? $" ({year.Value})"
            : string.Empty;
    }


    public void Dispose()
    {
        _client.Dispose();
    }
}