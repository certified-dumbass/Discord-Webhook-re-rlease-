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
}
