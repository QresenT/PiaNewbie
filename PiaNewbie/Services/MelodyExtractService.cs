using PiaNewbie.Enums;
using PiaNewbie.Models;
using PiaNewbie.Utils;

namespace PiaNewbie.Services;

public class MelodyExtractService
{
    public (List<GuideNote> GuideNotes, List<BackgroundNote> BackgroundNotes) Extract(
        MidiSong song,
        MidiTrackInfo melodyTrack)
    {
        var guideNotes = BuildGuideNotes(melodyTrack);
        var backgroundNotes = song.Tracks
            .Where(t => t.TrackIndex != melodyTrack.TrackIndex)
            .SelectMany(t => t.Notes.Select(n => new BackgroundNote
            {
                Pitch = n.Pitch,
                StartTime = n.StartTime,
                Duration = n.Duration,
                Velocity = n.Velocity,
                TrackIndex = n.TrackIndex,
                Channel = n.Channel
            }))
            .ToList();

        AddMelodyTrackAccompanimentNotes(melodyTrack, guideNotes, backgroundNotes);
        backgroundNotes.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        return (guideNotes, backgroundNotes);
    }

    private static List<GuideNote> BuildGuideNotes(MidiTrackInfo track)
    {
        var grouped = track.Notes
            .GroupBy(n => Math.Round(n.StartTime, 3))
            .OrderBy(g => g.Key)
            .Select(g => g.OrderByDescending(n => n.Pitch).First())
            .ToList();

        var guides = new List<GuideNote>();
        for (var i = 0; i < grouped.Count; i++)
        {
            var note = grouped[i];
            guides.Add(new GuideNote
            {
                Index = i,
                Pitch = note.Pitch,
                KeyIndex = PianoKeyboardMap.ToKeyIndex(note.Pitch),
                StartTime = note.StartTime,
                Duration = note.Duration,
                DirectionFromPrevious = i == 0
                    ? NoteDirection.None
                    : PianoKeyboardMap.GetPitchDirection(grouped[i - 1].Pitch, note.Pitch),
                IsCurrent = i == 0,
                SourceNote = note
            });
        }

        return guides;
    }

    private static void AddMelodyTrackAccompanimentNotes(
        MidiTrackInfo track,
        List<GuideNote> guides,
        List<BackgroundNote> background)
    {
        foreach (var note in track.Notes)
        {
            var isGuide = guides.Any(g =>
                Math.Abs(g.StartTime - note.StartTime) < 1e-3 && g.Pitch == note.Pitch);
            if (isGuide)
                continue;

            background.Add(new BackgroundNote
            {
                Pitch = note.Pitch,
                StartTime = note.StartTime,
                Duration = note.Duration,
                Velocity = note.Velocity,
                TrackIndex = note.TrackIndex,
                Channel = note.Channel
            });
        }
    }
}

