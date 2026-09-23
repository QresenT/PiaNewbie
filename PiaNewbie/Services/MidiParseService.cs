using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using PiaNewbie.Models;

namespace PiaNewbie.Services;

public class MidiParseService
{
    public MidiSong Parse(string filePath)
    {
        var midiFile = MidiFile.Read(filePath);
        var tempoMap = midiFile.GetTempoMap();
        var trackChunks = midiFile.GetTrackChunks().ToList();

        var song = new MidiSong
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        double maxEnd = 0;

        for (var i = 0; i < trackChunks.Count; i++)
        {
            var chunk = trackChunks[i];
            var trackName = chunk.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text
                            ?? $"Track {i + 1}";

            var notes = chunk.GetNotes()
                .Select(n => ToMidiNote(n, i, tempoMap))
                .OrderBy(n => n.StartTime)
                .ToList();

            if (notes.Count == 0)
                continue;

            var endTime = notes.Max(n => n.StartTime + n.Duration);
            if (endTime > maxEnd)
                maxEnd = endTime;

            var channels = notes.Select(n => n.Channel).Distinct().OrderBy(c => c).ToList();
            var isDrum = channels.Contains(9) || channels.Contains(10);

            song.Tracks.Add(new MidiTrackInfo
            {
                TrackIndex = i,
                TrackName = string.IsNullOrWhiteSpace(trackName) ? $"Track {i + 1}" : trackName,
                NoteCount = notes.Count,
                MinPitch = notes.Min(n => n.Pitch),
                MaxPitch = notes.Max(n => n.Pitch),
                AveragePitch = notes.Average(n => n.Pitch),
                Channels = channels,
                IsDrumTrack = isDrum,
                Notes = notes
            });
        }

        if (song.Tracks.Count == 0)
        {
            var allNotes = midiFile.GetNotes()
                .Select(n => ToMidiNote(n, 0, tempoMap))
                .OrderBy(n => n.StartTime)
                .ToList();

            if (allNotes.Count > 0)
            {
                maxEnd = allNotes.Max(n => n.StartTime + n.Duration);
                song.Tracks.Add(new MidiTrackInfo
                {
                    TrackIndex = 0,
                    TrackName = "Track 1",
                    NoteCount = allNotes.Count,
                    MinPitch = allNotes.Min(n => n.Pitch),
                    MaxPitch = allNotes.Max(n => n.Pitch),
                    AveragePitch = allNotes.Average(n => n.Pitch),
                    Channels = allNotes.Select(n => n.Channel).Distinct().OrderBy(c => c).ToList(),
                    Notes = allNotes
                });
            }
        }

        song.Duration = maxEnd;
        song.Bpm = ExtractInitialBpm(tempoMap);
        return song;
    }

    private static int? ExtractInitialBpm(TempoMap tempoMap)
    {
        try
        {
            var tempo = tempoMap.GetTempoAtTime(new MetricTimeSpan(0));
            return (int)Math.Round(tempo.BeatsPerMinute);
        }
        catch
        {
            return null;
        }
    }

    private static MidiNote ToMidiNote(Note note, int trackIndex, TempoMap tempoMap)
    {
        var start = note.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1_000_000.0;
        var length = note.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1_000_000.0;

        return new MidiNote
        {
            Pitch = note.NoteNumber,
            StartTime = start,
            Duration = Math.Max(length, 0.05),
            Velocity = note.Velocity,
            TrackIndex = trackIndex,
            Channel = note.Channel
        };
    }
}
