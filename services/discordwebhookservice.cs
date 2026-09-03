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

        _webhookUrl = webhookUrl.Trim();
        _configuration = configuration;

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task SendScanResult(
        ScanResult result,
        CancellationToken cancellationToken = default)
    {
        bool mentionEveryone =
            !result.BaselineInitialized &&
            GetTotalCount(result) > 0;

        bool firstMessage = true;

        foreach (string message in CreateMessages(result))
        {
            await SendMessage(
                    message,
                    mentionEveryone && firstMessage,
                    cancellationToken)
                .ConfigureAwait(false);

            firstMessage = false;
        }
    }

    public Task SendTestMessage(
        CancellationToken cancellationToken = default)
    {
        return SendMessage(
            $"💜 **{GetNotificationTitle()}**\n\n" +
            "✅ Test successful!\n" +
            "Your Discord webhook is configured correctly.\n\n" +
            "Jellyfin updates will be posted to this channel.",
            false,
            cancellationToken);
    }

    private async Task SendMessage(
        string message,
        bool mentionEveryone,
        CancellationToken cancellationToken)
    {
        string content =
            mentionEveryone
                ? "@everyone\n\n" + message
                : message;

        using var response =
            await _client.PostAsJsonAsync(
                    _webhookUrl,
                    new
                    {
                        content,
                        allowed_mentions = new
                        {
                            parse = mentionEveryone
                                ? new[] { "everyone" }
                                : Array.Empty<string>()
                        }
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

    private IEnumerable<string> CreateMessages(
        ScanResult result)
    {
        if (result.BaselineInitialized)
        {
            yield return
                $"💜 **{GetNotificationTitle()}**\n\n" +
                "✅ Scan baseline created successfully.\n" +
                "Newly added content will be announced from now on.";

            yield break;
        }

        int totalCount = GetTotalCount(result);

        if (totalCount == 0)
        {
            yield return
                string.IsNullOrWhiteSpace(_configuration.EmptyScanTemplate)
                    ? "🔍 Scan complete — nothing new this time."
                    : _configuration.EmptyScanTemplate;

            yield break;
        }

        string renderedMessage =
            RenderUpdateMessage(result);

        foreach (string chunk in SplitMessage(renderedMessage))
        {
            yield return chunk;
        }
    }

    private string RenderUpdateMessage(
        ScanResult result)
    {
        string template = GetSelectedTemplate();
        string schedule = GetScheduleText();
        int totalCount = GetTotalCount(result);

        if (result.Libraries.Count > 0)
        {
            string librariesBlock =
                BuildDynamicLibrariesBlock(result);

            template =
                ReplaceDynamicLibraryPlaceholder(
                    template,
                    librariesBlock);
        }
        else
        {
            Dictionary<string, string> sections =
                BuildLegacySections(result);

            string orderedCategories =
                BuildLegacyOrderedCategoryBlock(sections);

            template =
                ReplaceLegacyCategoryPlaceholders(
                    template,
                    orderedCategories);
        }

        var replacements =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["{schedule}"] =
                    _configuration.ShowScanFrequency
                        ? schedule
                        : string.Empty,

                ["{date}"] =
                    DateTime.Now.ToString("yyyy-MM-dd"),

                ["{count}"] =
                    _configuration.ShowTotalCount
                        ? totalCount.ToString()
                        : string.Empty,

                ["{title}"] =
                    GetNotificationTitle(),

                ["{name}"] =
                    GetNotificationName()
            };

        foreach (KeyValuePair<string, string> replacement in replacements)
        {
            template =
                template.Replace(
                    replacement.Key,
                    replacement.Value,
                    StringComparison.OrdinalIgnoreCase);
        }

        return CleanMessage(template);
    }

    private string BuildDynamicLibrariesBlock(
        ScanResult result)
    {
        List<LibraryScanResult> orderedLibraries =
            GetOrderedDynamicLibraries(result);

        var sections =
            new List<string>();

        foreach (LibraryScanResult library in orderedLibraries)
        {
            string section =
                BuildDynamicLibrarySection(library);

            if (!string.IsNullOrWhiteSpace(section))
            {
                sections.Add(section);
            }
        }

        return string.Join(
            "\n\n",
            sections);
    }

    private List<LibraryScanResult> GetOrderedDynamicLibraries(
        ScanResult result)
    {
        List<LibraryScanResult> libraries =
            result.Libraries.ToList();

        string[] configuredOrder =
            _configuration.ManualCategoryOrder ??
            Array.Empty<string>();

        if (configuredOrder.Length == 0)
        {
            return libraries;
        }

        var orderLookup =
            configuredOrder
                .Select((id, index) => new { id, index })
                .Where(x => !string.IsNullOrWhiteSpace(x.id))
                .GroupBy(
                    x => x.id,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().index,
                    StringComparer.OrdinalIgnoreCase);

        return libraries
            .Select((library, originalIndex) =>
                new
                {
                    library,
                    originalIndex,
                    order =
                        orderLookup.TryGetValue(
                            library.LibraryId,
                            out int index)
                            ? index
                            : int.MaxValue
                })
            .OrderBy(x => x.order)
            .ThenBy(x => x.originalIndex)
            .Select(x => x.library)
            .ToList();
    }

    private string BuildDynamicLibrarySection(
        LibraryScanResult library)
    {
        DiscordLibraryConfiguration? config =
            FindLibraryConfiguration(
                library.LibraryId);

        string displayName =
            !string.IsNullOrWhiteSpace(config?.DisplayName)
                ? config.DisplayName.Trim()
                : library.LibraryName;

        string emoji =
            !string.IsNullOrWhiteSpace(config?.Emoji)
                ? config.Emoji.Trim()
                : GetDefaultLibraryEmoji(
                    library.CollectionType);

        if (library.TotalNew == 0)
        {
            if (_configuration.HideEmptyCategories)
            {
                return string.Empty;
            }

            return
                $"{emoji} **{displayName}**\n" +
                "No new items found.";
        }

        var builder =
            new StringBuilder();

        builder.Append(emoji);
        builder.Append(" **");
        builder.Append(displayName);
        builder.AppendLine("**");
        builder.AppendLine();

        if (library.CollectionType.Equals(
                "movies",
                StringComparison.OrdinalIgnoreCase))
        {
            AppendDynamicMovies(
                builder,
                library.NewMovies);
        }
        else if (library.CollectionType.Equals(
                     "tvshows",
                     StringComparison.OrdinalIgnoreCase))
        {
            AppendDynamicTvShows(
                builder,
                library,
                config);
        }
        else if (library.CollectionType.Equals(
                     "boxsets",
                     StringComparison.OrdinalIgnoreCase))
        {
            AppendDynamicCollections(
                builder,
                library.NewCollections);
        }

        return builder
            .ToString()
            .TrimEnd();
    }

    private void AppendDynamicMovies(
        StringBuilder builder,
        IEnumerable<Movie> movies)
    {
        foreach (Movie movie in movies)
        {
            builder.Append("🍿 ");
            builder.Append(movie.Name);

            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(movie.Year));
            }

            builder.AppendLine();
        }
    }

    private void AppendDynamicCollections(
        StringBuilder builder,
        IEnumerable<CollectionItem> collections)
    {
        foreach (CollectionItem collection in collections)
        {
            builder.Append("📦 ");
            builder.Append(collection.Name);

            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(collection.Year));
            }

            builder.AppendLine();
        }
    }

    private void AppendDynamicTvShows(
        StringBuilder builder,
        LibraryScanResult library,
        DiscordLibraryConfiguration? config)
    {
        bool showEpisodeNames =
            config?.ShowEpisodeNames ?? true;

        var newSeriesById =
            library.NewSeries
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(
                    x => x.Id,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

        var seasonsBySeries =
            library.NewSeasons
                .GroupBy(
                    GetSeasonSeriesKey,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderBy(y => y.SeasonNumber ?? int.MaxValue)
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);

        var episodesBySeries =
            library.NewEpisodes
                .GroupBy(
                    GetEpisodeSeriesKey,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderBy(y => y.SeasonNumber ?? int.MaxValue)
                        .ThenBy(y => y.EpisodeNumber ?? int.MaxValue)
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);

        var seriesKeys =
            new List<string>();

        foreach (Series series in library.NewSeries)
        {
            AddUnique(
                seriesKeys,
                !string.IsNullOrWhiteSpace(series.Id)
                    ? series.Id
                    : series.Name);
        }

        foreach (SeasonScanItem season in library.NewSeasons)
        {
            AddUnique(
                seriesKeys,
                GetSeasonSeriesKey(season));
        }

        foreach (EpisodeScanItem episode in library.NewEpisodes)
        {
            AddUnique(
                seriesKeys,
                GetEpisodeSeriesKey(episode));
        }

        foreach (string seriesKey in seriesKeys)
        {
            Series? newSeries =
                newSeriesById.TryGetValue(
                    seriesKey,
                    out Series? foundSeries)
                    ? foundSeries
                    : null;

            List<SeasonScanItem> newSeasons =
                seasonsBySeries.TryGetValue(
                    seriesKey,
                    out List<SeasonScanItem>? foundSeasons)
                    ? foundSeasons
                    : new List<SeasonScanItem>();

            List<EpisodeScanItem> newEpisodes =
                episodesBySeries.TryGetValue(
                    seriesKey,
                    out List<EpisodeScanItem>? foundEpisodes)
                    ? foundEpisodes
                    : new List<EpisodeScanItem>();

            string seriesName =
                GetSeriesDisplayName(
                    newSeries,
                    newSeasons,
                    newEpisodes,
                    seriesKey);

            builder.Append("📺 **");
            builder.Append(seriesName);
            builder.Append("**");

            if (_configuration.ShowYears &&
                newSeries is not null)
            {
                builder.Append(
                    FormatYear(newSeries.Year));
            }

            builder.AppendLine();

            var seasonNumbers =
                new List<int?>();

            foreach (SeasonScanItem season in newSeasons)
            {
                AddUniqueSeasonNumber(
                    seasonNumbers,
                    season.SeasonNumber);
            }

            foreach (EpisodeScanItem episode in newEpisodes)
            {
                AddUniqueSeasonNumber(
                    seasonNumbers,
                    episode.SeasonNumber);
            }

            foreach (int? seasonNumber in seasonNumbers
                         .OrderBy(x => x ?? int.MaxValue))
            {
                SeasonScanItem? season =
                    newSeasons.FirstOrDefault(
                        x => x.SeasonNumber == seasonNumber);

                List<EpisodeScanItem> seasonEpisodes =
                    newEpisodes
                        .Where(x => x.SeasonNumber == seasonNumber)
                        .OrderBy(x => x.EpisodeNumber ?? int.MaxValue)
                        .ToList();

                string seasonLabel =
                    GetSeasonLabel(
                        seasonNumber,
                        season?.Name);

                builder.Append("└─ **");
                builder.Append(seasonLabel);
                builder.AppendLine("**");

                for (int i = 0; i < seasonEpisodes.Count; i++)
                {
                    EpisodeScanItem episode =
                        seasonEpisodes[i];

                    bool lastEpisode =
                        i == seasonEpisodes.Count - 1;

                    builder.Append(
                        lastEpisode
                            ? "   └─ "
                            : "   ├─ ");

                    builder.Append(
                        FormatEpisodeCode(
                            episode.SeasonNumber,
                            episode.EpisodeNumber));

                    if (showEpisodeNames &&
                        !string.IsNullOrWhiteSpace(episode.Name))
                    {
                        builder.Append(" — ");
                        builder.Append(episode.Name);
                    }

                    builder.AppendLine();
                }
            }

            builder.AppendLine();
        }
    }

    private static string GetSeasonSeriesKey(
        SeasonScanItem season)
    {
        if (!string.IsNullOrWhiteSpace(season.SeriesId))
        {
            return season.SeriesId;
        }

        return season.SeriesName;
    }

    private static string GetEpisodeSeriesKey(
        EpisodeScanItem episode)
    {
        if (!string.IsNullOrWhiteSpace(episode.SeriesId))
        {
            return episode.SeriesId;
        }

        return episode.SeriesName;
    }

    private static string GetSeriesDisplayName(
        Series? series,
        IReadOnlyCollection<SeasonScanItem> seasons,
        IReadOnlyCollection<EpisodeScanItem> episodes,
        string fallback)
    {
        if (series is not null &&
            !string.IsNullOrWhiteSpace(series.Name))
        {
            return series.Name;
        }

        string? seasonSeriesName =
            seasons
                .Select(x => x.SeriesName)
                .FirstOrDefault(
                    x => !string.IsNullOrWhiteSpace(x));

        if (!string.IsNullOrWhiteSpace(seasonSeriesName))
        {
            return seasonSeriesName;
        }

        string? episodeSeriesName =
            episodes
                .Select(x => x.SeriesName)
                .FirstOrDefault(
                    x => !string.IsNullOrWhiteSpace(x));

        if (!string.IsNullOrWhiteSpace(episodeSeriesName))
        {
            return episodeSeriesName;
        }

        return fallback;
    }

    private static string GetSeasonLabel(
        int? seasonNumber,
        string? seasonName)
    {
        if (seasonNumber.HasValue)
        {
            return $"Season {seasonNumber.Value}";
        }

        if (!string.IsNullOrWhiteSpace(seasonName))
        {
            return seasonName;
        }

        return "Season";
    }

    private static string FormatEpisodeCode(
        int? seasonNumber,
        int? episodeNumber)
    {
        string season =
            seasonNumber.HasValue
                ? seasonNumber.Value.ToString("00")
                : "??";

        string episode =
            episodeNumber.HasValue
                ? episodeNumber.Value.ToString("00")
                : "??";

        return $"S{season}E{episode}";
    }

    private static void AddUnique(
        List<string> values,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!values.Contains(
                value,
                StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }

    private static void AddUniqueSeasonNumber(
        List<int?> values,
        int? value)
    {
        if (!values.Contains(value))
        {
            values.Add(value);
        }
    }

    private DiscordLibraryConfiguration? FindLibraryConfiguration(
        string libraryId)
    {
        return (_configuration.Libraries ??
                Array.Empty<DiscordLibraryConfiguration>())
            .FirstOrDefault(
                x => x.LibraryId.Equals(
                    libraryId,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string GetDefaultLibraryEmoji(
        string collectionType)
    {
        if (collectionType.Equals(
                "movies",
                StringComparison.OrdinalIgnoreCase))
        {
            return "🎬";
        }

        if (collectionType.Equals(
                "tvshows",
                StringComparison.OrdinalIgnoreCase))
        {
            return "📺";
        }

        if (collectionType.Equals(
                "boxsets",
                StringComparison.OrdinalIgnoreCase))
        {
            return "📦";
        }

        return "💜";
    }

    private string ReplaceDynamicLibraryPlaceholder(
        string template,
        string librariesBlock)
    {
        if (template.Contains(
                "{libraries}",
                StringComparison.OrdinalIgnoreCase))
        {
            return template.Replace(
                "{libraries}",
                librariesBlock,
                StringComparison.OrdinalIgnoreCase);
        }

        string withoutLegacyPlaceholders =
            RemoveLegacyPlaceholders(template);

        if (string.IsNullOrWhiteSpace(librariesBlock))
        {
            return withoutLegacyPlaceholders;
        }

        return
            withoutLegacyPlaceholders.TrimEnd() +
            "\n\n" +
            librariesBlock;
    }

    private static string RemoveLegacyPlaceholders(
        string template)
    {
        string output = template;

        foreach (string placeholder in GetLegacyPlaceholders())
        {
            output =
                output.Replace(
                    placeholder,
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase);
        }

        return output;
    }

    private Dictionary<string, string> BuildLegacySections(
        ScanResult result)
    {
        return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Movies"] =
                _configuration.ScanMovies
                    ? BuildLegacyMoviesSection(result.NewMovies)
                    : string.Empty,

            ["Series"] =
                _configuration.ScanSeries
                    ? BuildLegacySeriesSection(
                        result.NewSeries,
                        "📺",
                        "Series")
                    : string.Empty,

            ["Anime"] =
                _configuration.ScanAnime
                    ? BuildLegacySeriesSection(
                        result.NewAnime,
                        "🌸",
                        "Anime")
                    : string.Empty,

            ["AnimeMovies"] =
                _configuration.ScanAnimeMovies
                    ? BuildLegacyMoviesSection(
                        result.NewAnimeMovies,
                        "🎌",
                        "Anime Movies",
                        "🎞️")
                    : string.Empty,

            ["Collections"] =
                _configuration.ScanCollections
                    ? BuildLegacyCollectionsSection(
                        result.NewCollections)
                    : string.Empty
        };
    }

    private string BuildLegacyMoviesSection(
        IReadOnlyCollection<Movie> movies,
        string headingEmoji = "🎬",
        string heading = "Movies",
        string itemEmoji = "🍿")
    {
        if (movies.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : $"{headingEmoji} **{heading}**\nNo new items found.";
        }

        var builder =
            new StringBuilder();

        builder.Append(headingEmoji);
        builder.Append(" **");
        builder.Append(heading);
        builder.AppendLine("**");

        foreach (Movie movie in movies)
        {
            builder.Append(itemEmoji);
            builder.Append(' ');
            builder.Append(movie.Name);

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

    private string BuildLegacySeriesSection(
        IReadOnlyCollection<Series> series,
        string headingEmoji,
        string heading)
    {
        if (series.Count == 0)
        {
            return _configuration.HideEmptyCategories
                ? string.Empty
                : $"{headingEmoji} **{heading}**\nNo new items found.";
        }

        var builder =
            new StringBuilder();

        builder.Append(headingEmoji);
        builder.Append(" **");
        builder.Append(heading);
        builder.AppendLine("**");

        foreach (Series item in series)
        {
            builder.Append(headingEmoji);
            builder.Append(' ');
            builder.Append(item.Name);

            if (_configuration.ShowYears)
            {
                builder.Append(
                    FormatYear(item.Year));
            }

            builder.AppendLine();
        }

        return builder
            .ToString()
            .TrimEnd();
    }

    private string BuildLegacyCollectionsSection(
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

        builder.AppendLine("📚 **Collections**");

        foreach (CollectionItem collection in collections)
        {
            builder.Append("📦 ");
            builder.Append(collection.Name);

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

    private string BuildLegacyOrderedCategoryBlock(
        IReadOnlyDictionary<string, string> sections)
    {
        string[] order =
            GetLegacyCategoryOrder();

        var builder =
            new StringBuilder();

        foreach (string category in order)
        {
            if (!sections.TryGetValue(
                    category,
                    out string? section) ||
                string.IsNullOrWhiteSpace(section))
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

    private string ReplaceLegacyCategoryPlaceholders(
        string template,
        string orderedCategories)
    {
        string[] placeholders =
            GetLegacyPlaceholders();

        int firstPosition = -1;
        string? firstPlaceholder = null;

        foreach (string placeholder in placeholders)
        {
            int position =
                template.IndexOf(
                    placeholder,
                    StringComparison.OrdinalIgnoreCase);

            if (position >= 0 &&
                (firstPosition == -1 ||
                 position < firstPosition))
            {
                firstPosition = position;
                firstPlaceholder = placeholder;
            }
        }

        if (firstPlaceholder is null)
        {
            if (template.Contains(
                    "{libraries}",
                    StringComparison.OrdinalIgnoreCase))
            {
                return template.Replace(
                    "{libraries}",
                    orderedCategories,
                    StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(orderedCategories))
            {
                return template;
            }

            return
                template.TrimEnd() +
                "\n\n" +
                orderedCategories;
        }

        string output = template;
        bool inserted = false;

        foreach (string placeholder in placeholders)
        {
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

                inserted = true;
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

        output =
            output.Replace(
                "{libraries}",
                string.Empty,
                StringComparison.OrdinalIgnoreCase);

        return output;
    }

    private string[] GetLegacyCategoryOrder()
    {
        string[] configuredOrder =
            _configuration.ManualCategoryOrder ??
            Array.Empty<string>();

        string[] validCategories =
        [
            "Anime",
            "Movies",
            "Series",
            "AnimeMovies",
            "Collections"
        ];

        List<string> cleanedOrder =
            configuredOrder
                .Where(
                    category =>
                        validCategories.Contains(
                            category,
                            StringComparer.OrdinalIgnoreCase))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (string category in validCategories)
        {
            if (!cleanedOrder.Contains(
                    category,
                    StringComparer.OrdinalIgnoreCase))
            {
                cleanedOrder.Add(category);
            }
        }

        return cleanedOrder.ToArray();
    }

    private static string[] GetLegacyPlaceholders()
    {
        return
        [
            "{movies}",
            "{series}",
            "{anime}",
            "{anime_movies}",
            "{collections}"
        ];
    }

    private int GetTotalCount(
        ScanResult result)
    {
        if (result.Libraries.Count > 0)
        {
            return result.Libraries.Sum(
                x => x.TotalNew);
        }

        int total = 0;

        if (_configuration.ScanMovies)
        {
            total += result.NewMovies.Count;
        }

        if (_configuration.ScanSeries)
        {
            total += result.NewSeries.Count;
        }

        if (_configuration.ScanAnime)
        {
            total += result.NewAnime.Count;
        }

        if (_configuration.ScanAnimeMovies)
        {
            total += result.NewAnimeMovies.Count;
        }

        if (_configuration.ScanCollections)
        {
            total += result.NewCollections.Count;
        }

        return total;
    }

    private string GetSelectedTemplate()
    {
        string style =
            _configuration.MessageStyle?.Trim() ??
            "Default";

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
                $"Here is your {{schedule}} {GetNotificationTitle().ToLowerInvariant()}.\n\n" +
                "{libraries}\n\n" +
                "{count} new additions just dropped. Have fun watching! 💜";
        }

        if (style.Equals(
                "Compact",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                $"💜 **{GetNotificationTitle()}**\n\n" +
                "{libraries}";
        }

        return GetDefaultTemplate();
    }

    private string GetDefaultTemplate()
    {
        string closingLine =
            string.IsNullOrWhiteSpace(GetNotificationName())
                ? "🌙 Enjoy watching!"
                : $"🌙 Enjoy watching on {GetNotificationName()}!";

        return
            $"💜 **{GetNotificationTitle()}**\n\n" +
            "Here is your {schedule} update on newly added content.\n\n" +
            "{libraries}\n\n" +
            "✅ {count} new additions.\n\n" +
            closingLine;
    }

    private string GetNotificationTitle()
    {
        string type =
            _configuration.NotificationType?.Trim() ??
            "Library";

        if (type.Equals(
                "Custom",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(
                    _configuration.CustomNotificationTitle))
            {
                return _configuration.CustomNotificationTitle.Trim();
            }

            return "Update";
        }

        string updateType =
            type.Equals(
                "Server",
                StringComparison.OrdinalIgnoreCase)
                ? "Server Update"
                : type.Equals(
                    "Website",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Website Update"
                    : "Library Update";

        string name =
            GetNotificationName();

        return string.IsNullOrWhiteSpace(name)
            ? updateType
            : $"{name} {updateType}";
    }

    private string GetNotificationName()
    {
        return _configuration.NotificationName?.Trim() ??
               string.Empty;
    }

    private string GetScheduleText()
    {
        return _configuration.ScanIntervalHours switch
        {
            1 => "hourly",
            2 => "2-hour",
            4 => "4-hour",
            6 => "6-hour",
            12 => "12-hour",
            24 => "daily",
            168 => "weekly",
            _ => $"every {_configuration.ScanIntervalHours} hours"
        };
    }

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

        bool previousWasEmpty = false;

        foreach (string rawLine in lines)
        {
            string line =
                rawLine.TrimEnd();

            bool isEmpty =
                string.IsNullOrWhiteSpace(line);

            if (isEmpty &&
                previousWasEmpty)
            {
                continue;
            }

            cleaned.Add(line);
            previousWasEmpty = isEmpty;
        }

        return string.Join(
                '\n',
                cleaned)
            .Trim();
    }

    private static IEnumerable<string> SplitMessage(
        string message)
    {
        if (message.Length <= SafeDiscordMessageLength)
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