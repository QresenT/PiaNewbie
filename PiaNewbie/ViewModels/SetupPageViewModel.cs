using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PiaNewbie.Enums;
using PiaNewbie.Models;
using PiaNewbie.Services;
using PiaNewbie.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace PiaNewbie.ViewModels;

public partial class SetupPageViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly MidiParseService _midiParse = new();
    private readonly TrackAnalyzeService _trackAnalyze = new();
    private readonly MelodyExtractService _melodyExtract = new();

    public SetupPageViewModel(NavigationService navigation)
    {
        _navigation = navigation;
        _guideNoteColorHex = AppSession.Instance.Theme.MelodyNoteColor;

        foreach (var item in GuideNoteColorPalette.Palette)
            PaletteColors.Add(item);

        foreach (var speed in new[] { 1.0, 1.25, 1.5, 1.75, 2.0 })
        {
            ScrollSpeedChoices.Add(new ScrollSpeedOptionItem
            {
                Value = speed,
                Label = FormatScrollSpeedLabel(speed)
            });
        }

        RollScrollSpeed = NormalizeScrollSpeed(AppSession.Instance.RollScrollSpeed);
        SyncPaletteSelection();
        SyncScrollSpeedSelection();
    }

    public ObservableCollection<MidiTrackInfo> Tracks { get; } = [];
    public ObservableCollection<ColorPresetItem> PaletteColors { get; } = [];
    public ObservableCollection<ScrollSpeedOptionItem> ScrollSpeedChoices { get; } = [];

    [ObservableProperty]
    private string _selectedFileName = "파일을 선택하세요";

    [ObservableProperty]
    private MidiTrackInfo? _selectedTrack;

    [ObservableProperty]
    private bool _isFlowMode = true;

    [ObservableProperty]
    private bool _isRhythmMode;

    [ObservableProperty]
    private string _modeHintMessage = "FLOW 모드에서는 반주가 나오지 않습니다.";

    [ObservableProperty]
    private double _rollScrollSpeed = 1.0;

    partial void OnRollScrollSpeedChanged(double value)
    {
        AppSession.Instance.RollScrollSpeed = value;
        SyncScrollSpeedSelection();
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _songAnalysisSummary = string.Empty;

    [ObservableProperty]
    private bool _canStart;

    [ObservableProperty]
    private string _guideNoteColorHex;

    public System.Windows.Media.Brush GuideColorPreviewBrush
    {
        get
        {
            if (!ColorHelper.TryNormalizeHex(GuideNoteColorHex, out var hex))
                return System.Windows.Media.Brushes.Gray;

            var c = (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(c);
        }
    }

    partial void OnGuideNoteColorHexChanged(string value)
    {
        if (ColorHelper.TryNormalizeHex(value, out var hex))
            GuideNoteColorPalette.ApplyBaseColor(AppSession.Instance.Theme, hex);

        SyncPaletteSelection();
        OnPropertyChanged(nameof(GuideColorPreviewBrush));
    }

    partial void OnSelectedTrackChanged(MidiTrackInfo? value) => UpdateCanStart();

    partial void OnIsFlowModeChanged(bool value)
    {
        if (value)
        {
            IsRhythmMode = false;
            ModeHintMessage = "FLOW 모드에서는 반주가 나오지 않습니다.";
        }
        UpdateCanStart();
    }

    partial void OnIsRhythmModeChanged(bool value)
    {
        if (value)
        {
            IsFlowMode = false;
            ModeHintMessage = "카운트다운 후 연주가 시작됩니다. 첫 노트는 아무 홈 행 키나 누르면 HIT입니다.";
        }
        UpdateCanStart();
    }

    [RelayCommand]
    private void BrowseMidi()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "MIDI 파일 (*.mid;*.midi)|*.mid;*.midi|모든 파일 (*.*)|*.*",
            Title = "MIDI 파일 선택"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var song = _midiParse.Parse(dialog.FileName);
            var session = AppSession.Instance;
            session.CurrentSong = song;
            session.ResetPracticeData();

            Tracks.Clear();
            foreach (var track in _trackAnalyze.Analyze(song))
                Tracks.Add(track);

            SelectedFileName = song.FileName;
            SelectedTrack = Tracks.FirstOrDefault();
            SongAnalysisSummary = BuildSongAnalysisSummary(song);
            StatusMessage = "분석이 완료되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"MIDI 분석 실패: {ex.Message}";
            SongAnalysisSummary = string.Empty;
            Tracks.Clear();
            SelectedTrack = null;
            CanStart = false;
        }
    }

    [RelayCommand]
    private void StartPractice()
    {
        if (SelectedTrack == null || AppSession.Instance.CurrentSong == null)
            return;

        var session = AppSession.Instance;
        session.SelectedMelodyTrack = SelectedTrack;
        session.SelectedMode = IsRhythmMode ? PracticeMode.Rhythm : PracticeMode.Flow;
        session.RollScrollSpeed = RollScrollSpeed;

        var (guides, backgrounds) = _melodyExtract.Extract(session.CurrentSong, SelectedTrack);
        session.GuideNotes = guides;
        session.BackgroundNotes = backgrounds;

        if (guides.Count == 0)
        {
            StatusMessage = "선택한 트랙에 멜로디 노트가 없습니다.";
            return;
        }

        _navigation.NavigateTo(AppPage.PracticeLoading);
    }

    private static string BuildSongAnalysisSummary(MidiSong song)
    {
        var bpm = song.Bpm.HasValue ? $"{song.Bpm} BPM" : "BPM —";
        return $"{bpm} · 길이 {FormatDurationMmSs(song.Duration)}";
    }

    private static string FormatDurationMmSs(double seconds)
    {
        var total = Math.Max(0, (int)Math.Round(seconds));
        return $"{total / 60}:{total % 60:D2}";
    }

    [RelayCommand]
    private void SelectGuideColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return;

        GuideNoteColorHex = hex.Trim();
    }

    [RelayCommand]
    private void SelectScrollSpeed(double speed) =>
        RollScrollSpeed = NormalizeScrollSpeed(speed);

    private static double NormalizeScrollSpeed(double speed) =>
        speed switch
        {
            >= 1.875 => 2.0,
            >= 1.625 => 1.75,
            >= 1.375 => 1.5,
            >= 1.125 => 1.25,
            _ => 1.0
        };

    private static string FormatScrollSpeedLabel(double speed) =>
        Math.Abs(speed % 1) < 0.001 ? $"{speed:0}×" : $"{speed:0.##}×";

    private void SyncScrollSpeedSelection()
    {
        foreach (var item in ScrollSpeedChoices)
            item.IsSelected = Math.Abs(item.Value - RollScrollSpeed) < 0.001;
    }

    [RelayCommand]
    private void Back() => _navigation.NavigateTo(AppPage.Main);

    [RelayCommand]
    private static void OpenMusescore()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://musescore.com/sheetmusic",
            UseShellExecute = true
        });
    }

    private void SyncPaletteSelection()
    {
        var selected = GuideNoteColorHex.Trim().ToUpperInvariant();
        if (!selected.StartsWith('#'))
            selected = "#" + selected;

        foreach (var item in PaletteColors)
            item.IsSelected = string.Equals(item.Hex, selected, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateCanStart() =>
        CanStart = SelectedTrack != null && Tracks.Count > 0 && (IsFlowMode || IsRhythmMode);
}
