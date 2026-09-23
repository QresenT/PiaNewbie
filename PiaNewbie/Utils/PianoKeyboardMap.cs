using PiaNewbie.Enums;

namespace PiaNewbie.Utils;

/// <summary>
/// 88건반 전체(A0=21 ~ C8=108)에 0부터 끝까지 고정 KeyIndex 부여.
/// 높/낮음·화면 X좌표는 모두 이 인덱스 기준 (옥타브 국소 범위 사용 안 함).
/// </summary>
public static class PianoKeyboardMap
{
    public const int FirstPitch = 21;
    public const int LastPitch = 108;
    public const int KeyCount = LastPitch - FirstPitch + 1;

    public static int ToKeyIndex(int midiPitch) =>
        Math.Clamp(midiPitch, FirstPitch, LastPitch) - FirstPitch;

    public static int ToMidiPitch(int keyIndex) =>
        Math.Clamp(keyIndex, 0, KeyCount - 1) + FirstPitch;

    public static int CompareKeyIndex(int fromMidi, int toMidi) =>
        ToKeyIndex(fromMidi).CompareTo(ToKeyIndex(toMidi));

    public static int GetKeyIndexDelta(int fromMidi, int toMidi) =>
        ToKeyIndex(toMidi) - ToKeyIndex(fromMidi);

    public static NoteDirection GetPitchDirection(int fromMidi, int toMidi)
    {
        var delta = GetKeyIndexDelta(fromMidi, toMidi);
        if (delta > 0)
            return NoteDirection.Up;
        if (delta < 0)
            return NoteDirection.Down;
        return NoteDirection.Same;
    }

    public static bool IsHigher(int fromMidi, int toMidi) => GetKeyIndexDelta(fromMidi, toMidi) > 0;

    public static bool IsLower(int fromMidi, int toMidi) => GetKeyIndexDelta(fromMidi, toMidi) < 0;

    /// <summary>KeyIndex 칸의 왼쪽 X</summary>
    public static float KeyIndexLeftX(int keyIndex, float width) =>
        keyIndex / (float)KeyCount * width;

    /// <summary>KeyIndex 칸의 오른쪽 X</summary>
    public static float KeyIndexRightX(int keyIndex, float width) =>
        (keyIndex + 1) / (float)KeyCount * width;

    /// <summary>해당 MIDI 음의 중심 X</summary>
    public static float PitchCenterX(int midiPitch, float width)
    {
        var idx = ToKeyIndex(midiPitch);
        return (idx + 0.5f) / KeyCount * width;
    }

    /// <summary>백건 [left, right) — MIDI pitch 경계</summary>
    public static (int LeftPitch, int RightPitch) GetWhiteKeyPitchSpan(int midiPitch)
    {
        return MidiNoteHelper.GetPitchClass(midiPitch) switch
        {
            0 or 2 or 5 or 7 or 9 => (midiPitch, midiPitch + 2),
            4 or 11 => (midiPitch, midiPitch + 1),
            _ => (midiPitch, midiPitch)
        };
    }

    public static (int LeftPitch, int RightPitch) GetBlackKeyPitchSpan(int midiPitch)
    {
        if (!MidiNoteHelper.IsBlackKey(midiPitch))
            return (midiPitch, midiPitch);

        return (midiPitch, midiPitch + 1);
    }

    public static float PitchSpanLeftX(int leftPitch, float width) =>
        KeyIndexLeftX(ToKeyIndex(leftPitch), width);

    public static float PitchSpanRightX(int rightPitch, float width) =>
        KeyIndexLeftX(ToKeyIndex(rightPitch), width);
}
