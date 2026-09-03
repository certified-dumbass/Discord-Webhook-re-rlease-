using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreamstreaming.DiscordBot.Models;

public class ScanResult
{
    public DateTime ScanDate { get; set; }

    public bool BaselineInitialized { get; set; }

    // Dynamic libraries - v1.2.0
    public List<LibraryScanResult> Libraries { get; set; } = new();

    // Legacy results
    public List<Movie> NewMovies { get; set; } = new();
    public List<Series> NewSeries { get; set; } = new();
    public List<Series> NewAnime { get; set; } = new();
    public List<Movie> NewAnimeMovies { get; set; } = new();
    public List<CollectionItem> NewCollections { get; set; } = new();

    public int TotalNew
    {
        get
        {
            if (Libraries.Count > 0)
            {
                return Libraries.Sum(library => library.TotalNew);
            }

            return
                NewMovies.Count +
                NewSeries.Count +
                NewAnime.Count +
                NewAnimeMovies.Count +
                NewCollections.Count;
        }
    }
}

public class LibraryScanResult
{
    public string LibraryId { get; set; } = string.Empty;

    public string LibraryName { get; set; } = string.Empty;

    public string CollectionType { get; set; } = string.Empty;

    public List<Movie> NewMovies { get; set; } = new();

    public List<Series> NewSeries { get; set; } = new();

    public List<SeasonScanItem> NewSeasons { get; set; } = new();

    public List<EpisodeScanItem> NewEpisodes { get; set; } = new();

    public List<CollectionItem> NewCollections { get; set; } = new();

    public int TotalNew =>
        NewMovies.Count +
        NewSeries.Count +
        NewSeasons.Count +
        NewEpisodes.Count +
        NewCollections.Count;
}

public class SeasonScanItem
{
    public string Id { get; set; } = string.Empty;

    public string SeriesId { get; set; } = string.Empty;

    public string SeriesName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? SeasonNumber { get; set; }

    public DateTime DateAdded { get; set; }
}

public class EpisodeScanItem
{
    public string Id { get; set; } = string.Empty;

    public string SeriesId { get; set; } = string.Empty;

    public string SeriesName { get; set; } = string.Empty;

    public string SeasonId { get; set; } = string.Empty;

    public string SeasonName { get; set; } = string.Empty;

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }
}