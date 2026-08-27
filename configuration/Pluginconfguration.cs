using MediaBrowser.Model.Plugins;

namespace Dreamstreaming.DiscordBot.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string JellyfinUrl { get; set; } = string.Empty;

    public string JellyfinApiKey { get; set; } = string.Empty;

    public string DiscordWebhook { get; set; } = string.Empty;


    // ============================================================
    // Scan schedule
    // ============================================================

    // Scan interval in hours.
    // 1 = every hour
    // 2 = every 2 hours
    // 4 = every 4 hours
    // 6 = every 6 hours
    // 12 = every 12 hours
    // 24 = every day
    // 168 = every week
    public int ScanIntervalHours { get; set; } = 168;

    // Day used for weekly scans.
    // 0 = Sunday
    // 1 = Monday
    // 2 = Tuesday
    // 3 = Wednesday
    // 4 = Thursday
    // 5 = Friday
    // 6 = Saturday
    public int ScanDay { get; set; } = 0;

    // Time at which the scan should run.
    // Format: HH:mm
    public string ScanTime { get; set; } = "20:00";


    // ============================================================
    // Content scanning
    // ============================================================

    // Movies
    public bool ScanMovies { get; set; } = true;


    // Series
    public bool ScanSeries { get; set; } = true;

    // Scan seasons belonging to series
    public bool ScanSeriesSeasons { get; set; } = true;

    // Scan episodes belonging to series
    public bool ScanSeriesEpisodes { get; set; } = true;


    // Anime
    public bool ScanAnime { get; set; } = true;

    // Scan seasons belonging to anime series
    public bool ScanAnimeSeasons { get; set; } = true;

    // Scan episodes belonging to anime series
    public bool ScanAnimeEpisodes { get; set; } = true;


    // Anime movies
    public bool ScanAnimeMovies { get; set; } = true;


    // Collections
    public bool ScanCollections { get; set; } = true;
}