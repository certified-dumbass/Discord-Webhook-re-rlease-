namespace Dreamstreaming.DiscordBot.Models;

public class ScanResult
{
    public DateTime ScanDate { get; set; }

    public bool BaselineInitialized { get; set; }

    public List<Movie> NewMovies { get; set; } = new();

    public List<Series> NewSeries { get; set; } = new();

    public int TotalNew => NewMovies.Count + NewSeries.Count;
}
