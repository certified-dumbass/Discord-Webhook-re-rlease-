namespace Dreamstreaming.DiscordBot.Models;

public class CollectionItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }

    public int? Year { get; set; }
}