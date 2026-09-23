using Melanchall.DryWetMidi.Core;
using PiaNewbie.Enums;
using PiaNewbie.Models;
using System.Windows;
using System.Windows.Threading;

namespace PiaNewbie.Services;

public class PracticePreloadService
{
    public const double MinimumLoadingSeconds = 2.0;

    /// <summary>Rhythm: 재생 시작 시 첫 노트가 히트 라인 위에 있을 시간(초).</summary>
    public const double RhythmFirstNoteApproachSeconds = 1.0;

    public static double GetRhythmStartOffset(IReadOnlyList<GuideNote> guides)
    {
        if (guides.Count == 0)
            return 0;

        return Math.Max(0, guides[0].StartTime - RhythmFirstNoteApproachSeconds);
    }

    public async Task PrepareAsync(
        PracticeMode mode,
        MidiSong? song,
        IReadOnlyList<GuideNote> guides,
        CancellationToken cancellationToken = default)
    {
        if (song == null || string.IsNullOrEmpty(song.FilePath))
            return;

        var session = AppSession.Instance;
        session.Audio.WarmUp();

        if (mode != PracticeMode.Rhythm || session.Audio.IsPreparedFor(song.FilePath))
            return;

        var filePath = song.FilePath;
        var startOffset = GetRhythmStartOffset(guides);

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var midiFile = MidiFile.Read(filePath);
            var dispatcher = Application.Current?.Dispatcher;
            dispatcher?.Invoke(DispatcherPriority.Background, () =>
            {
                if (session.CurrentSong?.FilePath != filePath)
                    return;

                session.Audio.PrepareRhythmPlayback(song, midiFile, startOffset);
            });
        }, cancellationToken);
    }
}
