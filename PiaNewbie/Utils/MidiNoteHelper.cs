using PiaNewbie.Enums;

namespace PiaNewbie.Utils;

/// <summary>MIDI 음이름·건반 종류. 높/낮음·X좌표는 <see cref="PianoKeyboardMap"/> 사용.</summary>
public static class MidiNoteHelper
{
    public const int MinMidiPitch = PianoKeyboardMap.FirstPitch;
    public const int MaxMidiPitch = PianoKeyboardMap.LastPitch;

    public static readonly IReadOnlyList<string> ChromaticSharpNames =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    private static readonly string[] FlatNames =
        ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];

    public static int GetPitchClass(int pitch) => ((pitch % 12) + 12) % 12;

    public static int GetOctave(int pitch) => pitch / 12 - 1;

    public static string GetPitchClassName(int pitch, bool preferFlats = false)
    {
        var pc = GetPitchClass(pitch);
        return preferFlats ? FlatNames[pc] : ChromaticSharpNames[pc];
    }

    public static bool IsBlackKey(int pitch) => GetPitchClass(pitch) is 1 or 3 or 6 or 8 or 10;

    public static bool IsWhiteKey(int pitch) => !IsBlackKey(pitch);

    public static int GetSemitoneDelta(int fromPitch, int toPitch) =>
        PianoKeyboardMap.GetKeyIndexDelta(fromPitch, toPitch);

    public static NoteDirection GetPitchDirection(int fromMidi, int toMidi) =>
        PianoKeyboardMap.GetPitchDirection(fromMidi, toMidi);

    public static string PitchToName(int pitch, bool preferFlats = false) =>
        $"{GetPitchClassName(pitch, preferFlats)}{GetOctave(pitch)}";
}
