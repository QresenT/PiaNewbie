using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PiaNewbie.Enums;
using PiaNewbie.Models;
using PiaNewbie.Services;
using PiaNewbie.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PiaNewbie.ViewModels;

public partial class PracticePageViewModel : ObservableObject
{
    public const double PracticeLeadInSeconds = 3.0;
    public const double PracticeEndDelaySeconds = 1.5;

    /// <summary>Rhythm: 첫 노트만 판정 반폭을 이만큼 추가(초).</summary>
    private const double RhythmFirstNoteExtraWindowSeconds = 0.2;

    private const int FlowMinPlayNoteMs = 120;
    private const int FlowMaxPlayNoteMs = 6000;

    private const int RollRefreshFps = 30;
    private static readonly TimeSpan RollRefreshInterval = TimeSpan.FromMilliseconds(1000.0 / RollRefreshFps);

    private readonly NavigationService _navigation;
    private readonly RhythmJudgeService _rhythm = new();
    private readonly DirectionInputService _direction = new();
    private PracticeFlowService? _flow;
    private DispatcherTimer? _rollTimer;
    private Stopwatch? _practiceClock;
    private bool _inLeadIn;
    private bool _flowWaitingForInput;
    private double _flowPausedTime;
    private double _flowMidiResumeTime;
    private Stopwatch? _flowClock;
    private double _flowClockOrigin;
    private Key? _anchorKey;
    private int _maxCombo;

    [ObservableProperty]
    private int _rhythmHitCount;

    [ObservableProperty]
    private int _rhythmMissCount;

    [ObservableProperty]
    private int _comboCount;
    private int _leadInCountdownSecond = -1;
    private long _leadInLastRollSyncMs;
    private bool _pendingFinish;
    private bool _finishCompleted;
    private double _finishFrozenTime;
    private double _finishFrozenAudioTime;
    private DispatcherTimer? _finishDelayTimer;
    private string _judgmentText = string.Empty;
    private string _judgmentSubtext = string.Empty;
    private string _judgmentKind = string.Empty;

    private const string RhythmFirstNoteHint =
        "첫 노트는 아무 홈 행 키나 누르면 HIT";
    private int _rhythmPreloadGeneration;

    public event Action? RollRefreshRequested;

    public event Action? PracticeRestartRequested;

    public PracticePageViewModel(NavigationService navigation)
    {
        _navigation = navigation;
    }

    [ObservableProperty]
    private string _statusText = "연습 준비";

    [ObservableProperty]
    private PracticeMode _mode = PracticeMode.Flow;

    [ObservableProperty]
    private string _playbackClockText = "0:00 / 0:00";

    public bool ShowRhythmStats => Mode == PracticeMode.Rhythm;

    partial void OnModeChanged(PracticeMode value) => OnPropertyChanged(nameof(ShowRhythmStats));

    private AppSession Session => AppSession.Instance;

    public IReadOnlyList<GuideNote> GuideNotes => Session.GuideNotes;
    public IReadOnlyList<BackgroundNote> BackgroundNotes => Session.BackgroundNotes;

    public int CurrentIndex => _flow?.CurrentIndex ?? 0;

    public double CurrentTime
    {
        get
        {
            if (_pendingFinish)
                return _finishFrozenTime;

            if (Mode == PracticeMode.Rhythm)
                return GetRhythmRollTime();

            if (_inLeadIn && _practiceClock != null)
                return _practiceClock.Elapsed.TotalSeconds - PracticeLeadInSeconds;

            if (Mode == PracticeMode.Flow)
                return GetFlowDisplayTime();

            if (Session.Audio.IsPlaying)
                return Session.Audio.GetPlaybackSeconds();

            return _practiceClock?.Elapsed.TotalSeconds ?? 0;
        }
    }

    public int? HighlightPitch =>
        CurrentIndex < GuideNotes.Count ? GuideNotes[CurrentIndex].Pitch : null;

    public Key? AnchorKey => _anchorKey;

    public PracticeFlowService? GetFlowService() => _flow;

    /// <summary>Flow 입력 대기·Rhythm 카운트다운(롤 고정)·종료 후 롤 고정.</summary>
    public bool FreezeRollPlayback =>
        _pendingFinish ||
        (_inLeadIn && Mode == PracticeMode.Rhythm) ||
        (Mode == PracticeMode.Flow && _flowWaitingForInput);

    public string JudgmentText => _judgmentText;

    public string JudgmentSubtext => _judgmentSubtext;

    public string JudgmentKind => _judgmentKind;

    private double GetFlowDisplayTime()
    {
        if (_flowWaitingForInput)
            return _flowPausedTime;

        if (_flowClock?.IsRunning == true)
            return _flowClockOrigin + _flowClock.Elapsed.TotalSeconds;

        return _flowPausedTime;
    }

    private double GetFlowAudioResumeTime() =>
        _flowMidiResumeTime > 0 ? _flowMidiResumeTime : _flowPausedTime;

    public IReadOnlyList<int> GetPressableKeyIndices()
    {
        if (_flow == null)
            return [];

        var current = _flow.CurrentNote;
        if (current == null)
            return [];

        return KeyboardLayoutHelper.GetPressableKeyIndices(
            _anchorKey,
            _flow.PreviousNote,
            current);
    }

    public void Initialize()
    {
        StopPracticeSession();

        _flow = new PracticeFlowService(Session.GuideNotes);
        _anchorKey = null;
        RhythmHitCount = 0;
        RhythmMissCount = 0;
        ComboCount = 0;
        _maxCombo = 0;
        ClearRhythmJudgment();
        _flowWaitingForInput = false;
        _flowPausedTime = 0;
        _flowMidiResumeTime = 0;
        Mode = Session.SelectedMode;
        Session.Audio.WarmUp();
        if (Mode == PracticeMode.Rhythm)
            BeginRhythmPreload();

        _practiceClock = Stopwatch.StartNew();
        _inLeadIn = true;
        _leadInCountdownSecond = -1;
        _leadInLastRollSyncMs = 0;

        Session.Audio.PlaybackFinished -= OnMidiPlaybackFinished;
        Session.Audio.PlaybackFinished += OnMidiPlaybackFinished;

        if (Mode == PracticeMode.Rhythm)
        {
            StatusText = GetRhythmSongTitle();
            SetRhythmCountdown((int)PracticeLeadInSeconds);
        }
        else
            StatusText = $"Flow — {PracticeLeadInSeconds:0}초 후 시작";

        UpdatePlaybackClockText();
        StartRollTimer();
        RollRefreshRequested?.Invoke();
    }

    public void OnKeyDown(Key key)
    {
        if (_inLeadIn || _pendingFinish || _flow == null ||
            !_direction.IsMappedKey(key) || _flow.IsCompleted)
            return;

        var current = _flow.CurrentNote;
        if (current == null)
            return;

        if (Mode == PracticeMode.Flow)
        {
            if (!_flowWaitingForInput)
            {
                if (_flowClock?.IsRunning != true ||
                    !_rhythm.IsWithinHitWindow(GetFlowDisplayTime(), current.StartTime))
                {
                    StatusText = "판정 범위 밖 (±125ms)";
                    RollRefreshRequested?.Invoke();
                    return;
                }
            }

            if (!KeyboardLayoutHelper.IsKeyPressable(key, GetPressableKeyIndices()))
            {
                StatusText = "강조된 방향의 키만 입력할 수 있습니다.";
                RollRefreshRequested?.Invoke();
                return;
            }

            if (!_flowWaitingForInput)
            {
                _flowPausedTime = GetFlowDisplayTime();
                _flowMidiResumeTime = _flowPausedTime;
            }

            _anchorKey = key;
            PlayFlowGuideNote(current);
            _flow.TryAdvance();
            StatusText = $"노트 {Math.Min(_flow.CurrentIndex + 1, GuideNotes.Count)} / {GuideNotes.Count}";

            if (_flow.IsCompleted)
                ScheduleFinishPractice(completed: true);
            else
                ResumeFlowScrollToNextNote();

            return;
        }

        AdvanceRhythmPastMissedNotes();

        if (_flow.CurrentIndex == 0 && _flow.CurrentNote != null)
        {
            _anchorKey = key;
            RhythmHitCount++;
            ComboCount++;
            _maxCombo = Math.Max(_maxCombo, ComboCount);
            _flow.TryAdvance();
            SetRhythmJudgment("hit", "HIT");
        }
        else
        {
            var time = CurrentTime;
            var hitTarget = FindRhythmNoteInWindow(time);
            if (hitTarget != null)
            {
                while (_flow.CurrentIndex < hitTarget.Index)
                    _flow.TryAdvance();

                if (KeyboardLayoutHelper.IsKeyPressable(key, GetPressableKeyIndices()))
                {
                    _anchorKey = key;
                    RhythmHitCount++;
                    ComboCount++;
                    _maxCombo = Math.Max(_maxCombo, ComboCount);
                    _flow.TryAdvance();
                    SetRhythmJudgment("hit", "HIT");
                }
                else
                {
                    RhythmMissCount++;
                    ComboCount = 0;
                    SetRhythmJudgment("miss", "MISS");
                }
            }
            else
            {
                RhythmMissCount++;
                ComboCount = 0;
                SetRhythmJudgment("miss", "MISS");
            }
        }

        if (_flow.IsCompleted)
            ScheduleFinishPractice(completed: true);
        else
            RollRefreshRequested?.Invoke();
    }

    private void OnMidiPlaybackFinished()
    {
        if (_flow == null || _inLeadIn)
            return;

        if (Mode == PracticeMode.Flow)
            return;

        if (_pendingFinish)
        {
            StartFinishDelayTimer();
            return;
        }

        AdvanceRhythmPastMissedNotes();
        if (_flow.IsCompleted)
            ScheduleFinishPractice(completed: true);
    }

    private void ScheduleFinishPractice(bool completed)
    {
        if (_pendingFinish)
            return;

        _finishFrozenTime = CurrentTime;
        _finishFrozenAudioTime = Mode == PracticeMode.Rhythm
            ? Session.Audio.GetPlaybackSeconds()
            : 0;
        _pendingFinish = true;
        _finishCompleted = completed;
        _flowWaitingForInput = false;
        _flowClock?.Stop();
        _flowClock = null;

        if (Mode == PracticeMode.Rhythm)
        {
            StatusText = "Track Complete";
            RollRefreshRequested?.Invoke();
            if (!Session.Audio.IsPlaying)
                StartFinishDelayTimer();
            return;
        }

        Session.Audio.StopPlayback();
        StatusText = "Track Complete";
        RollRefreshRequested?.Invoke();
        StartFinishDelayTimer();
    }

    private void StartFinishDelayTimer()
    {
        if (_finishDelayTimer != null)
            return;

        _finishDelayTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            System.Windows.Application.Current.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(PracticeEndDelaySeconds)
        };
        _finishDelayTimer.Tick += (_, _) =>
        {
            _finishDelayTimer?.Stop();
            _finishDelayTimer = null;
            FinishPractice(_finishCompleted);
        };
        _finishDelayTimer.Start();
    }

    private static void PlayFlowGuideNote(GuideNote note)
    {
        var velocity = note.SourceNote?.Velocity ?? 90;
        var durationMs = (int)Math.Clamp(
            note.Duration * 1000,
            FlowMinPlayNoteMs,
            FlowMaxPlayNoteMs);
        AppSession.Instance.Audio.PlayNote(note.Pitch, velocity, durationMs);
    }

    private void ResumeFlowScrollToNextNote()
    {
        _flowWaitingForInput = false;

        var resumeAt = GetFlowAudioResumeTime();
        _flowMidiResumeTime = resumeAt;
        _flowClockOrigin = resumeAt;
        _flowClock = Stopwatch.StartNew();

        RollRefreshRequested?.Invoke();
    }

    private void PauseFlowAtCurrentNote()
    {
        var note = _flow?.CurrentNote;
        if (note == null)
            return;

        _flowClock?.Stop();
        _flowClock = null;

        var now = GetFlowDisplayTime();
        // note.StartTime으로 되감으면 롤이 뒤로 튀어 보임 — 실제 시각 유지(앞으로만 보정)
        _flowPausedTime = Math.Max(note.StartTime, now);
        _flowMidiResumeTime = _flowPausedTime;
        _flowWaitingForInput = true;
        StatusText = "강조된 노트를 누르세요";
        RollRefreshRequested?.Invoke();
    }

    private void RequestRollRefreshThrottled()
    {
        if (!_inLeadIn)
        {
            RollRefreshRequested?.Invoke();
            return;
        }

        var now = Environment.TickCount64;
        if (now - _leadInLastRollSyncMs < 100)
            return;

        _leadInLastRollSyncMs = now;
        RollRefreshRequested?.Invoke();
    }

    private void StartRollTimer()
    {
        if (_rollTimer != null)
            return;

        _rollTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            System.Windows.Application.Current.Dispatcher)
        {
            Interval = RollRefreshInterval
        };
        _rollTimer.Tick += OnRollTick;
        _rollTimer.Start();
    }

    private void OnRollTick(object? sender, EventArgs e)
    {
        if (_inLeadIn)
        {
            var elapsed = _practiceClock?.Elapsed.TotalSeconds ?? 0;
            if (elapsed < PracticeLeadInSeconds)
            {
                var sec = (int)Math.Ceiling(PracticeLeadInSeconds - elapsed);
                if (sec != _leadInCountdownSecond)
                {
                    _leadInCountdownSecond = sec;
                    if (Mode == PracticeMode.Rhythm)
                        SetRhythmCountdown(sec);
                    else
                        StatusText = $"Flow — {sec}초 후 시작";
                }

                RequestRollRefreshThrottled();
                UpdatePlaybackClockText();
                return;
            }

            CompleteLeadIn();
        }

        if (Mode == PracticeMode.Flow && !_inLeadIn)
        {
            if (_flowWaitingForInput)
            {
                UpdatePlaybackClockText();
                RollRefreshRequested?.Invoke();
                return;
            }

            if (_flowClock?.IsRunning == true)
            {
                var t = GetFlowDisplayTime();
                if (_flow?.CurrentNote is { } n && t >= n.StartTime - 0.001)
                    PauseFlowAtCurrentNote();
            }
        }
        else if (Mode == PracticeMode.Rhythm && !_pendingFinish && !_inLeadIn)
            AdvanceRhythmPastMissedNotes();

        UpdatePlaybackClockText();
        RollRefreshRequested?.Invoke();
    }

    private void CompleteLeadIn()
    {
        _inLeadIn = false;

        if (Mode == PracticeMode.Rhythm)
        {
            if (Session.CurrentSong != null)
            {
                ClearRhythmJudgment();
                StartRhythmPlayback();
                ApplyRhythmPlaybackStatus();
            }
            else
                StatusText = "Rhythm Mode — MIDI 파일 없음";
        }
        else
        {
            var t0 = GuideNotes.Count > 0 ? GuideNotes[0].StartTime : 0;
            _flowPausedTime = t0;
            _flowMidiResumeTime = t0;
            _flowWaitingForInput = true;

            StatusText = "강조된 노트를 누르세요";
        }

        RollRefreshRequested?.Invoke();
    }

    private string GetRhythmSongTitle()
    {
        var name = Session.CurrentSong?.FileName;
        return string.IsNullOrWhiteSpace(name) ? "Rhythm Mode" : name;
    }

    private double GetRhythmPlaybackStartOffset() =>
        PracticePreloadService.GetRhythmStartOffset(GuideNotes);

    private double GetRhythmRollTime()
    {
        if (_inLeadIn)
            return GetRhythmPlaybackStartOffset();

        if (Session.Audio.IsPlaying)
            return Session.Audio.GetPlaybackSeconds();

        var song = Session.CurrentSong;
        if (song != null && Session.Audio.IsPreparedFor(song.FilePath))
            return Session.Audio.GetPlaybackSeconds();

        return GetRhythmPlaybackStartOffset();
    }

    private void ApplyRhythmPlaybackStatus()
    {
        StatusText = Session.Audio.IsPlaying
            ? GetRhythmSongTitle()
            : "Rhythm Mode — MIDI 출력을 사용할 수 없습니다";
    }

    private double GetRhythmHitWindowHalfSeconds(int noteIndex) =>
        RhythmJudgeService.HitWindowHalfSeconds +
        (noteIndex == 0 ? RhythmFirstNoteExtraWindowSeconds : 0);

    private bool IsWithinRhythmHitWindow(double playbackTime, double noteStart, int noteIndex) =>
        Math.Abs(playbackTime - noteStart) <= GetRhythmHitWindowHalfSeconds(noteIndex);

    private GuideNote? FindRhythmNoteInWindow(double playbackTime)
    {
        for (var i = _flow!.CurrentIndex; i < GuideNotes.Count; i++)
        {
            var n = GuideNotes[i];
            if (IsWithinRhythmHitWindow(playbackTime, n.StartTime, i))
                return n;
            var half = GetRhythmHitWindowHalfSeconds(i);
            if (n.StartTime > playbackTime + half)
                break;
        }

        return null;
    }

    private void AdvanceRhythmPastMissedNotes()
    {
        if (_flow == null || Mode != PracticeMode.Rhythm || _inLeadIn || !Session.Audio.IsPlaying)
            return;

        var time = CurrentTime;
        while (_flow.CurrentNote is { } n &&
               _flow.CurrentIndex > 0 &&
               time > n.StartTime + GetRhythmHitWindowHalfSeconds(_flow.CurrentIndex))
        {
            RhythmMissCount++;
            ComboCount = 0;
            SetRhythmJudgment("miss", "MISS");
            if (!_flow.TryAdvance())
                break;
            if (_flow.IsCompleted)
            {
                ScheduleFinishPractice(completed: true);
                return;
            }
        }
    }

    private void BeginRhythmPreload()
    {
        var song = Session.CurrentSong;
        if (song == null || string.IsNullOrEmpty(song.FilePath))
            return;

        if (Session.Audio.IsPreparedFor(song.FilePath))
            return;

        var generation = ++_rhythmPreloadGeneration;
        var startOffset = GetRhythmPlaybackStartOffset();
        var filePath = song.FilePath;

        _ = Task.Run(() =>
        {
            try
            {
                var midiFile = Melanchall.DryWetMidi.Core.MidiFile.Read(filePath);
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                dispatcher?.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (generation != _rhythmPreloadGeneration || Mode != PracticeMode.Rhythm)
                        return;

                    Session.Audio.PrepareRhythmPlayback(song, midiFile, startOffset);
                });
            }
            catch
            {
                // 프리로드 실패 시 CompleteLeadIn에서 동기 로드
            }
        });
    }

    private void StartRhythmPlayback()
    {
        var song = Session.CurrentSong;
        if (song == null)
            return;

        var startOffset = GetRhythmPlaybackStartOffset();
        Session.Audio.WarmUp();

        if (!Session.Audio.IsPreparedFor(song.FilePath))
            Session.Audio.PrepareRhythmPlayback(song, startOffset);

        if (!Session.Audio.StartPreparedRhythmPlayback())
        {
            Session.Audio.PlaySongFrom(song, startOffset);
            if (!Session.Audio.IsPlaying)
                Session.Audio.PlaySongFrom(song, startOffset);
        }
    }

    private double TotalDurationSeconds => Session.CurrentSong?.Duration ?? 0;

    private double GetDisplayPlaybackSeconds()
    {
        if (_inLeadIn)
            return 0;

        if (Mode == PracticeMode.Flow)
            return Math.Max(0, GetFlowDisplayTime());

        if (Session.Audio.IsPlaying)
            return Math.Max(0, Session.Audio.GetPlaybackSeconds());

        return Math.Max(0, CurrentTime);
    }

    private void UpdatePlaybackClockText()
    {
        var text = FormatPlaybackClock(GetDisplayPlaybackSeconds(), TotalDurationSeconds);
        if (PlaybackClockText != text)
            PlaybackClockText = text;
    }

    private static string FormatPlaybackClock(double currentSeconds, double totalSeconds)
    {
        return $"{FormatMmSs(currentSeconds)} / {FormatMmSs(totalSeconds)}";
    }

    private static string FormatMmSs(double seconds)
    {
        var total = Math.Max(0, (int)Math.Floor(seconds));
        return $"{total / 60}:{total % 60:D2}";
    }

    private void SetRhythmCountdown(int seconds)
    {
        _judgmentKind = "countdown";
        _judgmentText = seconds.ToString();
        _judgmentSubtext = $"초 후 시작  ·  {RhythmFirstNoteHint}";
    }

    private void SetRhythmJudgment(string kind, string text)
    {
        _judgmentKind = kind;
        _judgmentText = text;
        _judgmentSubtext = string.Empty;
    }

    private void ClearRhythmJudgment()
    {
        _judgmentKind = string.Empty;
        _judgmentText = string.Empty;
        _judgmentSubtext = string.Empty;
    }

    private void StopRollTimer()
    {
        if (_rollTimer == null)
            return;

        _rollTimer.Tick -= OnRollTick;
        _rollTimer.Stop();
        _rollTimer = null;
    }

    private void StopPracticeSession()
    {
        if (_finishDelayTimer != null)
        {
            _finishDelayTimer.Stop();
            _finishDelayTimer = null;
        }

        _pendingFinish = false;
        _finishFrozenTime = 0;
        _finishFrozenAudioTime = 0;
        StopRollTimer();
        _flowWaitingForInput = false;
        _flowMidiResumeTime = 0;
        _flowClock?.Stop();
        _flowClock = null;
        _inLeadIn = false;
        Session.Audio.PlaybackFinished -= OnMidiPlaybackFinished;
        Session.Audio.StopPlayback();
        _practiceClock?.Stop();
    }

    private void FinishPractice(bool completed)
    {
        var elapsed = Mode == PracticeMode.Rhythm
            ? TimeSpan.FromSeconds(_finishFrozenAudioTime > 0
                ? _finishFrozenAudioTime
                : Session.Audio.GetPlaybackSeconds())
            : _practiceClock?.Elapsed ?? TimeSpan.Zero;

        StopPracticeSession();

        if (Mode == PracticeMode.Flow)
        {
            Session.LastResult = new PracticeResult
            {
                Mode = Mode,
                TotalNotes = GuideNotes.Count,
                HitNotes = _flow?.ProgressedNotes ?? 0,
                MissedNotes = 0,
                Accuracy = GuideNotes.Count == 0 ? 0 : 100,
                ElapsedTime = elapsed,
                Completed = completed
            };
        }
        else
        {
            var total = RhythmHitCount + RhythmMissCount;
            Session.LastResult = _rhythm.BuildResult(
                Mode, total, RhythmHitCount, RhythmMissCount, elapsed, _maxCombo);
        }

        _navigation.NavigateTo(AppPage.Result);
    }

    [RelayCommand]
    private void EndPractice()
    {
        StopPracticeSession();
        _navigation.NavigateTo(AppPage.Main);
    }

    [RelayCommand]
    private void GoSetup()
    {
        StopPracticeSession();
        _navigation.NavigateTo(AppPage.Setup);
    }

    [RelayCommand]
    private void RestartPractice()
    {
        Initialize();
        PracticeRestartRequested?.Invoke();
    }
}
