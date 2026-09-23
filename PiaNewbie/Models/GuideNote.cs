using PiaNewbie.Enums;

namespace PiaNewbie.Models;

public class GuideNote
{
    public int Index { get; set; }
    public int Pitch { get; set; }
    /// <summary>전체 88건반 기준 KeyIndex (A0=0 … C8=87)</summary>
    public int KeyIndex { get; set; }
    public double StartTime { get; set; }
    public double Duration { get; set; }
    public NoteDirection DirectionFromPrevious { get; set; }
    public bool IsCurrent { get; set; }
    public MidiNote? SourceNote { get; set; }
}
