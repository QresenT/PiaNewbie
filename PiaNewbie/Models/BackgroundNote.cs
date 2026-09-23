namespace PiaNewbie.Models;

public class BackgroundNote
{
    public int Pitch { get; set; }
    public double StartTime { get; set; }
    public double Duration { get; set; }
    public int Velocity { get; set; } = 80;
    public int TrackIndex { get; set; }
    public int Channel { get; set; }
}

