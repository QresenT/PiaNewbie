namespace PiaNewbie.Models;

public class AppTheme
{
    public string BackgroundColor { get; set; } = "#050505";
    public string PanelColor { get; set; } = "#111111";
    public string PanelBorderColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#FFFFFF";
    public string SubTextColor { get; set; } = "#AAAAAA";
    public string MelodyNoteColor { get; set; } = "#66CCFF";
    public string CurrentNoteColor { get; set; } = "#BEEBFF";
    public string BackgroundNoteColor { get; set; } = "#555555";
    public string KeyboardWhiteKeyColor { get; set; } = "#F5F5F5";
    public string KeyboardBlackKeyColor { get; set; } = "#111111";
    public string KeyHighlightColor { get; set; } = "#66CCFF";

    public static AppTheme Default => new();
}
