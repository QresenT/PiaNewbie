using PiaNewbie.Enums;
using PiaNewbie.Models;

namespace PiaNewbie.Services;

public class RhythmJudgeService
{
    /// <summary>전체 250ms (앞·뒤 각 125ms).</summary>
    public const double HitWindowSeconds = 0.25;

    public const double HitWindowHalfSeconds = 0.125;

    public bool IsWithinHitWindow(double currentTime, double targetStartTime) =>
        Math.Abs(currentTime - targetStartTime) <= HitWindowHalfSeconds;

    public JudgeResult JudgeTiming(double currentTime, double targetStartTime) =>
        IsWithinHitWindow(currentTime, targetStartTime) ? JudgeResult.Hit : JudgeResult.Miss;

    public PracticeResult BuildResult(
        PracticeMode mode,
        int total,
        int hits,
        int misses,
        TimeSpan elapsed,
        int maxCombo)
    {
        var accuracy = total == 0 ? 0 : (double)hits / total * 100;
        return new PracticeResult
        {
            Mode = mode,
            TotalNotes = total,
            HitNotes = hits,
            MissedNotes = misses,
            Accuracy = accuracy,
            ElapsedTime = elapsed,
            MaxCombo = maxCombo,
            Completed = true
        };
    }
}

