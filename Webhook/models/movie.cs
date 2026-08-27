namespace Dreamstreaming.DiscordBot.Models;

public class Movie
{
    public string Name { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string Id { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }

    public string PosterUrl { get; set; } = string.Empty;
}
