using PiaNewbie.Enums;
using PiaNewbie.Models;

namespace PiaNewbie.Services;

public sealed class AppSession
{
    public static AppSession Instance { get; } = new();

    public MidiSong? CurrentSong { get; set; }
    public MidiTrackInfo? SelectedMelodyTrack { get; set; }
    public List<GuideNote> GuideNotes { get; set; } = [];
    public List<BackgroundNote> BackgroundNotes { get; set; } = [];
    public PracticeMode SelectedMode { get; set; } = PracticeMode.Flow;

    /// <summary>피아노 롤 스크롤 배율 (1.0=기본). 음악 재생 속도와 무관.</summary>
    public double RollScrollSpeed { get; set; } = 1.0;
    public PracticeResult? LastResult { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.Default;

    public MidiAudioService Audio { get; } = new();

    public void ResetPracticeData()
    {
        GuideNotes = [];
        BackgroundNotes = [];
        LastResult = null;
    }
}
