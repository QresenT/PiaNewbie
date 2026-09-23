using PiaNewbie.Enums;

namespace PiaNewbie.Models;

public class PracticeResult
{
    public PracticeMode Mode { get; set; }
    public int TotalNotes { get; set; }
    public int HitNotes { get; set; }
    public int MissedNotes { get; set; }
    public double Accuracy { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public int MaxCombo { get; set; }
    public bool Completed { get; set; }
}
