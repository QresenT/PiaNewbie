namespace PiaNewbie.Models;

public class MidiSong
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public double Duration { get; set; }
    public int? Bpm { get; set; }
    public List<MidiTrackInfo> Tracks { get; set; } = [];
}
