using System;
using MediaBrowser.Model.Plugins;

namespace Dreamstreaming.DiscordBot.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string JellyfinUrl { get; set; } = string.Empty;
    public string JellyfinApiKey { get; set; } = string.Empty;
    public string DiscordWebhook { get; set; } = string.Empty;

    // Scan schedule
    public int ScanIntervalHours { get; set; } = 168;
    public int ScanDay { get; set; } = 0;
    public string ScanTime { get; set; } = "20:00";

    // Dynamic libraries
    public DiscordLibraryConfiguration[] Libraries { get; set; } =
        Array.Empty<DiscordLibraryConfiguration>();

    // Legacy content scanning
    public bool ScanMovies { get; set; } = true;
    public bool ScanSeries { get; set; } = true;
    public bool ScanSeriesSeasons { get; set; } = true;
    public bool ScanSeriesEpisodes { get; set; } = true;
    public bool ScanAnime { get; set; } = true;
    public bool ScanAnimeSeasons { get; set; } = true;
    public bool ScanAnimeEpisodes { get; set; } = true;
    public bool ScanAnimeMovies { get; set; } = true;
    public bool ScanCollections { get; set; } = true;

    // Legacy library mapping
    public string[] MovieLibraryIds { get; set; } =
        Array.Empty<string>();

    public string[] SeriesLibraryIds { get; set; } =
        Array.Empty<string>();

    public string[] AnimeLibraryIds { get; set; } =
        Array.Empty<string>();

    public string[] AnimeMovieLibraryIds { get; set; } =
        Array.Empty<string>();

    public string[] CollectionLibraryIds { get; set; } =
        Array.Empty<string>();

    // Discord message style
    public string MessageStyle { get; set; } = "Default";

    public string MessageTemplate { get; set; } =
        "Hey everyone! 👋\n\n" +
        "Here is your {schedule} update on what got added.\n\n" +
        "{libraries}\n\n" +
        "That is {count} new additions. Enjoy watching! 💜";

    public string EmptyScanTemplate { get; set; } =
        "🔍 Scan complete — nothing new this time.";

    public bool HideEmptyCategories { get; set; } = true;
    public bool ShowYears { get; set; } = true;
    public bool ShowTotalCount { get; set; } = true;
    public bool ShowScanFrequency { get; set; } = true;

    // Discord category ordering
    public string CategoryOrderMode { get; set; } = "Manual";

    public string[] ManualCategoryOrder { get; set; } =
        Array.Empty<string>();

    public int PopularityWindowDays { get; set; } = 7;
}

public class DiscordLibraryConfiguration
{
    public string LibraryId { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = false;

    public string DisplayName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;

    public string CollectionType { get; set; } = string.Empty;
    public string ScanType { get; set; } = "Auto";

    public bool ScanSeasons { get; set; } = true;
    public bool ScanEpisodes { get; set; } = true;
    public bool ShowEpisodeNames { get; set; } = true;
}