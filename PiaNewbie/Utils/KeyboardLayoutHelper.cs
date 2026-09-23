using PiaNewbie.Enums;
using PiaNewbie.Models;
using System.Windows.Input;

namespace PiaNewbie.Utils;

/// <summary>홈 행(A~') — 높은 음=오른쪽, 낮은 음=왼쪽. 방향에 쓸 키가 없으면 전체 허용.</summary>
public static class KeyboardLayoutHelper
{
    public static readonly Key[] HomeRowKeys =
    [
        Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L,
        Key.OemSemicolon, Key.OemQuotes
    ];

    private static readonly Dictionary<Key, double> KeyX = BuildLayout();

    public static bool IsMappedKey(Key key) => KeyX.ContainsKey(key);

    public static bool TryGetX(Key key, out double x) => KeyX.TryGetValue(key, out x);

    public static bool TryGetHomeRowIndex(Key key, out int index)
    {
        index = Array.IndexOf(HomeRowKeys, key);
        return index >= 0;
    }

    public static bool IsKeyPressable(Key pressedKey, IReadOnlyList<int> pressableIndices)
    {
        if (!TryGetHomeRowIndex(pressedKey, out var idx))
            return false;

        return pressableIndices.Contains(idx);
    }

    public static IReadOnlyList<Key> GetKeysInDirection(Key anchor, NoteDirection direction)
    {
        if (!TryGetX(anchor, out var ax))
            return [];

        return HomeRowKeys
            .Where(k => k != anchor && IsKeyInDirection(anchor, k, direction, ax))
            .ToList();
    }

    public static bool IsPressAllowed(Key anchor, Key pressed, NoteDirection direction)
    {
        if (!IsMappedKey(pressed))
            return false;

        if (direction == NoteDirection.Same)
            return true;

        if (GetKeysInDirection(anchor, direction).Count == 0)
            return true;

        return IsKeyInDirection(anchor, pressed, direction);
    }

    /// <summary>지금 눌러도 되는 홈 행 키 인덱스(0=A … 10=').</summary>
    public static IReadOnlyList<int> GetPressableKeyIndices(
        Key? anchorKey,
        GuideNote? fromNote,
        GuideNote? currentNote)
    {
        if (currentNote == null)
            return [];

        if (fromNote == null)
            return Enumerable.Range(0, HomeRowKeys.Length).ToArray();

        if (!anchorKey.HasValue || !IsMappedKey(anchorKey.Value))
            return Enumerable.Range(0, HomeRowKeys.Length).ToArray();

        var dir = PianoKeyboardMap.GetPitchDirection(fromNote.Pitch, currentNote.Pitch);
        if (dir == NoteDirection.Same)
            return Enumerable.Range(0, HomeRowKeys.Length).ToArray();

        var keys = GetKeysInDirection(anchorKey.Value, dir);
        if (keys.Count == 0)
            return Enumerable.Range(0, HomeRowKeys.Length).ToArray();

        return keys.Select(k => Array.IndexOf(HomeRowKeys, k)).Where(i => i >= 0).ToArray();
    }

    public static string GetAllowedKeysHint(Key anchor, NoteDirection direction)
    {
        if (direction == NoteDirection.Same)
            return string.Join("", HomeRowKeys.Select(KeyToLabel));

        var directional = GetKeysInDirection(anchor, direction);
        if (directional.Count == 0)
            return string.Join("", HomeRowKeys.Select(KeyToLabel));

        return string.Join("", directional.Select(KeyToLabel));
    }

    public static string KeyToLabel(Key key) => key switch
    {
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        >= Key.A and <= Key.Z => ((char)('A' + (key - Key.A))).ToString(),
        _ => "?"
    };

    private static bool IsKeyInDirection(Key anchor, Key pressed, NoteDirection direction, double? anchorX = null)
    {
        if (!TryGetX(anchor, out var ax) || !TryGetX(pressed, out var px))
            return false;

        ax = anchorX ?? ax;

        return direction switch
        {
            NoteDirection.Up => px > ax,
            NoteDirection.Down => px < ax,
            NoteDirection.Same => Math.Abs(px - ax) < 0.01,
            _ => true
        };
    }

    private static bool IsKeyInDirection(Key anchor, Key pressed, NoteDirection direction) =>
        IsKeyInDirection(anchor, pressed, direction, null);

    private static Dictionary<Key, double> BuildLayout()
    {
        var map = new Dictionary<Key, double>();
        for (var i = 0; i < HomeRowKeys.Length; i++)
            map[HomeRowKeys[i]] = i;
        return map;
    }
}
