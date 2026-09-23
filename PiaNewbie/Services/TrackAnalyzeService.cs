using PiaNewbie.Models;

namespace PiaNewbie.Services;

public class TrackAnalyzeService
{
    public List<MidiTrackInfo> Analyze(MidiSong song) => song.Tracks;
}
