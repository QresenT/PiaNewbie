namespace PiaNewbie.Models;

public class MidiTrackInfo
{
    public int TrackIndex { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public int NoteCount { get; set; }
    public int MinPitch { get; set; }
    public int MaxPitch { get; set; }
    public double AveragePitch { get; set; }
    public List<int> Channels { get; set; } = [];
    public string ChannelsDisplay => string.Join(", ", Channels);
    public bool IsDrumTrack { get; set; }
    public List<MidiNote> Notes { get; set; } = [];
}
