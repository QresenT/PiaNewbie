using PiaNewbie.Models;

namespace PiaNewbie.Utils;

public static class GuideNoteColorPalette
{
    /// <summary>설정 화면 팔레트 (이름 + hex). 헥사코드를 몰라도 칸을 눌러 선택.</summary>
    public static readonly IReadOnlyList<ColorPresetItem> Palette =
    [
        new() { Name = "하늘", Hex = "#66CCFF" },
        new() { Name = "하늘 진함", Hex = "#38BDF8" },
        new() { Name = "파랑", Hex = "#60A5FA" },
        new() { Name = "남색", Hex = "#818CF8" },
        new() { Name = "보라", Hex = "#A78BFA" },
        new() { Name = "자주", Hex = "#C084FC" },
        new() { Name = "분홍", Hex = "#F472B6" },
        new() { Name = "빨강", Hex = "#F87171" },
        new() { Name = "주황", Hex = "#FB923C" },
        new() { Name = "노랑", Hex = "#FACC15" },
        new() { Name = "연두", Hex = "#A3E635" },
        new() { Name = "초록", Hex = "#4ADE80" },
        new() { Name = "민트", Hex = "#5EEAD4" },
        new() { Name = "청록", Hex = "#2DD4BF" },
        new() { Name = "베이지", Hex = "#FCD34D" },
        new() { Name = "살구", Hex = "#FDBA74" },
        new() { Name = "장미", Hex = "#FB7185" },
        new() { Name = "라벤더", Hex = "#E879F9" },
        new() { Name = "회청", Hex = "#94A3B8" },
        new() { Name = "흰색", Hex = "#F8FAFC" },
        new() { Name = "연회", Hex = "#CBD5E1" },
        new() { Name = "금색", Hex = "#EAB308" },
        new() { Name = "올리브", Hex = "#84CC16" },
        new() { Name = "청록 진함", Hex = "#14B8A6" },
    ];

    public static void ApplyBaseColor(AppTheme theme, string baseHex)
    {
        if (!ColorHelper.TryNormalizeHex(baseHex, out var hex))
            return;

        theme.MelodyNoteColor = hex;
        theme.CurrentNoteColor = ColorHelper.AdjustBrightness(hex, 48);
        theme.KeyHighlightColor = hex;
    }

    public static RollThemePayload ToRollPayload(AppTheme theme) => new()
    {
        Guide = theme.MelodyNoteColor,
        GuideNext = ColorHelper.AdjustBrightness(theme.MelodyNoteColor, 28),
        GuideCurrent = theme.CurrentNoteColor,
        KeyHighlight = ColorHelper.WithAlpha(theme.KeyHighlightColor, 0.55),
        PressableBorder = theme.KeyHighlightColor,
        PressableBg = ColorHelper.AdjustBrightness(theme.KeyHighlightColor, -60),
        PressableGlow = ColorHelper.WithAlpha(theme.KeyHighlightColor, 0.45)
    };

    public sealed class RollThemePayload
    {
        public string Guide { get; set; } = "#5b8cff";
        public string GuideNext { get; set; } = "#7eb8ff";
        public string GuideCurrent { get; set; } = "#b8e0ff";
        public string KeyHighlight { get; set; } = "rgba(91,140,255,0.55)";
        public string PressableBorder { get; set; } = "#4a9fe8";
        public string PressableBg { get; set; } = "#1e4a7a";
        public string PressableGlow { get; set; } = "rgba(74,159,232,0.45)";
    }
}
