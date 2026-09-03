namespace Dreamstreaming.DiscordBot.Models;

public class Movie
{
    public string Name { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string Id { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }

    public string PosterUrl { get; set; } = string.Empty;


    // ============================================================
    // Jellyfin library information
    // ============================================================

    // ID of the Jellyfin library this movie belongs to.
    public string LibraryId { get; set; } = string.Empty;

    // Display name of the Jellyfin library.
    //
    // This is informational only. We should not rely on a library
    // having a specific name such as "Anime Movies".
    public string LibraryName { get; set; } = string.Empty;


    // ============================================================
    // Classification
    // ============================================================

    // True when this movie has been classified as an anime movie.
    //
    // Classification will be handled by the scanning/service layer
    // rather than guessing based on the movie title.
    public bool IsAnime { get; set; }
}