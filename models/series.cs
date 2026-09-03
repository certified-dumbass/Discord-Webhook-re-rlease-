namespace Dreamstreaming.DiscordBot.Models;

public class Series
{
    public string Name { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }

    public string PosterUrl { get; set; } = string.Empty;

    public int? Year { get; set; }

    public int? Seasons { get; set; }

    public int? Episodes { get; set; }


    // ============================================================
    // Jellyfin library information
    // ============================================================

    // ID of the Jellyfin library this series belongs to.
    public string LibraryId { get; set; } = string.Empty;

    // Display name of the Jellyfin library.
    //
    // This is informational only. The plugin should not assume that
    // a specific library name automatically means Anime or Series.
    public string LibraryName { get; set; } = string.Empty;


    // ============================================================
    // Classification
    // ============================================================

    // True when this series has been classified as anime.
    //
    // This will later be determined by the selected library mapping
    // rather than guessing based on the title.
    public bool IsAnime { get; set; }
}