using Microsoft.Web.WebView2.Core;
using PiaNewbie.Enums;
using PiaNewbie.Models;
using PiaNewbie.Services;
using PiaNewbie.Utils;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PiaNewbie.Controls;

/// <summary>
/// NeoTheresia(PolyMeilex/Neothesia)는 Rust/wgpu 독립 실행형이라 WPF에 직접 삽입할 수 없습니다.
/// 동일한 낙하 노트 방식은 WebView2 + Canvas로 렌더합니다 (GPU 합성, SkiaSharp 대체).
/// </summary>
public partial class PianoRollWebViewControl : System.Windows.Controls.UserControl
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private bool _isReady;
    private bool _pendingInit;
    private InitPayload? _lastInit;
    private UpdatePayload? _lastUpdate;
    private string? _lastPostedUpdateJson;

    public PianoRollWebViewControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public async Task EnsureInitializedAsync()
    {
        if (_isReady)
            return;

        try
        {
            await WebView.EnsureCoreWebView2Async();
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var htmlPath = Path.Combine(
                AppContext.BaseDirectory,
                "wwwroot", "pianoroll", "index.html");

            if (!File.Exists(htmlPath))
            {
                MessageBox.Show(
                    $"피아노 롤 UI 파일을 찾을 수 없습니다:\n{htmlPath}",
                    "PiaNewbie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            WebView.Source = new Uri(htmlPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WebView2 초기화 실패:\n{ex.Message}\n\nMicrosoft Edge WebView2 런타임이 필요합니다.",
                "PiaNewbie",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>곡·모드가 바뀔 때 한 번만 호출 (무거운 init JSON).</summary>
    public void LoadSong(
        IReadOnlyList<GuideNote> guides,
        IReadOnlyList<BackgroundNote> backgrounds,
        PracticeMode mode,
        AppTheme? theme = null)
    {
        var pitches = guides.Select(g => g.Pitch).ToList();
        if (pitches.Count == 0)
            pitches.Add(60);

        var minPitch = Math.Max(PianoKeyboardMap.FirstPitch, pitches.Min() - 2);
        var maxPitch = Math.Min(PianoKeyboardMap.LastPitch, pitches.Max() + 2);

        theme ??= AppSession.Instance.Theme;
        var rollTheme = GuideNoteColorPalette.ToRollPayload(theme);

        _lastInit = new InitPayload
        {
            Type = "init",
            GuideNotes = guides.Select(g => NotePayload.FromGuide(g)).ToList(),
            BackgroundNotes = backgrounds.Select(NotePayload.FromBackground).ToList(),
            Mode = mode == PracticeMode.Rhythm ? "rhythm" : "flow",
            MinPitch = minPitch,
            MaxPitch = maxPitch,
            Theme = rollTheme,
            ScrollSpeed = AppSession.Instance.RollScrollSpeed
        };

        _pendingInit = true;
        _lastPostedUpdateJson = null;
        PostMessages();
    }

    /// <summary>재생·판정 상태만 갱신 (가벼운 update JSON).</summary>
    public void UpdatePlayback(
        int currentIndex,
        double currentTime,
        PracticeMode mode,
        int? highlightPitch = null,
        Key? anchorKey = null,
        GuideNote? previousNote = null,
        GuideNote? currentNote = null,
        IReadOnlyList<int>? pressableKeyIndices = null,
        bool freezePlayback = false,
        string? judgmentText = null,
        string? judgmentSubtext = null,
        string? judgmentKind = null)
    {
        _lastUpdate = BuildUpdate(
            currentIndex, currentTime, mode, highlightPitch,
            anchorKey, previousNote, currentNote, pressableKeyIndices, freezePlayback,
            judgmentText, judgmentSubtext, judgmentKind);

        PostUpdateIfChanged();
    }

    public void Refresh() => PostMessages();

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        _ = EnsureInitializedAsync();

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
            return;

        _isReady = true;
        PostMessages();
    }

    private void PostMessages()
    {
        if (!_isReady || WebView.CoreWebView2 == null)
            return;

        if (_pendingInit && _lastInit != null)
        {
            PostJson(_lastInit);
            _pendingInit = false;
        }

        PostUpdateIfChanged();
    }

    private void PostUpdateIfChanged()
    {
        if (_lastUpdate == null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(_lastUpdate, JsonOptions);
            if (json == _lastPostedUpdateJson)
                return;

            _lastPostedUpdateJson = json;
            WebView.CoreWebView2?.PostWebMessageAsJson(json);
        }
        catch
        {
            // ignore serialization errors during shutdown
        }
    }

    private void PostJson(object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            WebView.CoreWebView2?.PostWebMessageAsJson(json);
        }
        catch
        {
            // ignore serialization errors during shutdown
        }
    }

    private static UpdatePayload BuildUpdate(
        int currentIndex,
        double currentTime,
        PracticeMode mode,
        int? highlightPitch,
        Key? anchorKey,
        GuideNote? previousNote,
        GuideNote? currentNote,
        IReadOnlyList<int>? pressableKeyIndices,
        bool freezePlayback,
        string? judgmentText,
        string? judgmentSubtext,
        string? judgmentKind)
    {
        var pressable = pressableKeyIndices?.ToList()
            ?? KeyboardLayoutHelper.GetPressableKeyIndices(anchorKey, previousNote, currentNote).ToList();

        return new UpdatePayload
        {
            Type = "update",
            CurrentGuideIndex = currentIndex,
            PlaybackTime = Math.Round(currentTime, 4),
            IsRhythmMode = mode == PracticeMode.Rhythm,
            FreezePlayback = freezePlayback,
            HighlightPitch = highlightPitch,
            PressableKeyIndices = pressable,
            JudgmentText = judgmentText ?? string.Empty,
            JudgmentSubtext = judgmentSubtext ?? string.Empty,
            JudgmentKind = judgmentKind ?? string.Empty
        };
    }

    private sealed class InitPayload
    {
        public string Type { get; set; } = "init";
        public List<NotePayload> GuideNotes { get; set; } = [];
        public List<NotePayload> BackgroundNotes { get; set; } = [];
        public string Mode { get; set; } = "flow";
        public int MinPitch { get; set; }
        public int MaxPitch { get; set; }
        public GuideNoteColorPalette.RollThemePayload? Theme { get; set; }
        public double ScrollSpeed { get; set; } = 1.0;
    }

    private sealed class UpdatePayload
    {
        public string Type { get; set; } = "update";
        public int CurrentGuideIndex { get; set; }
        public double PlaybackTime { get; set; }
        public bool IsRhythmMode { get; set; }
        public bool FreezePlayback { get; set; }
        public int? HighlightPitch { get; set; }
        public List<int> PressableKeyIndices { get; set; } = [];
        public string JudgmentText { get; set; } = string.Empty;
        public string JudgmentSubtext { get; set; } = string.Empty;
        public string JudgmentKind { get; set; } = string.Empty;
    }

    private sealed class NotePayload
    {
        public int Index { get; set; }
        public int Pitch { get; set; }
        public double StartTime { get; set; }
        public double Duration { get; set; }

        public static NotePayload FromGuide(GuideNote g) => new()
        {
            Index = g.Index,
            Pitch = g.Pitch,
            StartTime = g.StartTime,
            Duration = g.Duration
        };

        public static NotePayload FromBackground(BackgroundNote b) => new()
        {
            Index = -1,
            Pitch = b.Pitch,
            StartTime = b.StartTime,
            Duration = b.Duration
        };
    }
}
