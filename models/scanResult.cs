namespace Dreamstreaming.DiscordBot.Models;

public class ScanResult
{
    public DateTime ScanDate { get; set; }

    public bool BaselineInitialized { get; set; }


    // ============================================================
    // Movies
    // ============================================================

    public List<Movie> NewMovies { get; set; } = new();


    // ============================================================
    // Series
    // ============================================================

    public List<Series> NewSeries { get; set; } = new();


    // ============================================================
    // Anime
    // ============================================================

    public List<Series> NewAnime { get; set; } = new();


    // ============================================================
    // Anime movies
    // ============================================================

    public List<Movie> NewAnimeMovies { get; set; } = new();


    // ============================================================
    // Collections
    // ============================================================

    public List<CollectionItem> NewCollections { get; set; } = new();


    // ============================================================
    // Totals
    // ============================================================

    public int TotalNew =>
        NewMovies.Count +
        NewSeries.Count +
        NewAnime.Count +
        NewAnimeMovies.Count +
        NewCollections.Count;
}