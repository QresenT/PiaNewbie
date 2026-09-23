using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using PiaNewbie.Models;

namespace PiaNewbie.Services;

public sealed class MidiAudioService : IDisposable
{
    private OutputDevice? _outputDevice;
    private Playback? _playback;
    private TempoMap? _tempoMap;
    private string? _loadedSongPath;
    private double _pausedAtSeconds;
    private bool _disposed;

    public event Action? PlaybackFinished;

    public bool IsPlaying => _playback?.IsRunning == true;

    public bool IsAvailable => TryEnsureDevice();

    /// <summary>출력 장치를 미리 열어 첫 PlaySong 지연·실패를 줄입니다.</summary>
    public bool WarmUp() => TryEnsureDevice();

    public bool IsPreparedFor(string? filePath) =>
        _playback != null && !string.IsNullOrEmpty(filePath) && _loadedSongPath == filePath;

    public void PlaySong(MidiSong song) => PlaySongFrom(song, 0);

    /// <summary>Rhythm: 파일을 읽고 재생 위치만 맞춘 뒤 Start는 하지 않습니다 (프리롤 중 프리로드).</summary>
    public void PrepareRhythmPlayback(MidiSong song, double startSeconds)
    {
        if (!TryEnsureDevice() || string.IsNullOrEmpty(song.FilePath))
            return;

        try
        {
            if (_playback == null || _loadedSongPath != song.FilePath)
            {
                DisposePlaybackInstance();
                var midiFile = MidiFile.Read(song.FilePath);
                AttachRhythmPlayback(song.FilePath, midiFile);
            }

            _pausedAtSeconds = startSeconds;
            SeekPlaybackToSeconds(startSeconds);
        }
        catch
        {
            StopPlayback();
        }
    }

    /// <summary>백그라운드에서 읽은 MidiFile로 UI 스레드에서만 Playback을 준비합니다.</summary>
    public void PrepareRhythmPlayback(MidiSong song, MidiFile midiFile, double startSeconds)
    {
        if (!TryEnsureDevice() || string.IsNullOrEmpty(song.FilePath))
            return;

        try
        {
            if (_playback == null || _loadedSongPath != song.FilePath)
            {
                DisposePlaybackInstance();
                AttachRhythmPlayback(song.FilePath, midiFile);
            }

            _pausedAtSeconds = startSeconds;
            SeekPlaybackToSeconds(startSeconds);
        }
        catch
        {
            StopPlayback();
        }
    }

    public bool StartPreparedRhythmPlayback()
    {
        if (_playback == null)
            return false;

        if (_playback.IsRunning)
            return true;

        try
        {
            _playback.Start();
            return true;
        }
        catch
        {
            StopPlayback();
            return false;
        }
    }

    /// <summary>Rhythm 등: 재생 인스턴스를 새로 만듭니다.</summary>
    public void PlaySongFrom(MidiSong song, double startSeconds)
    {
        PrepareRhythmPlayback(song, startSeconds);
        if (_playback != null)
            StartPreparedRhythmPlayback();
    }

    /// <summary>Flow: 곡을 한 번만 로드하고 위치만 이동 (매 노트마다 파일을 다시 열지 않음).</summary>
    public void PrepareFlowPlayback(MidiSong song, double startSeconds)
    {
        if (!TryEnsureDevice() || string.IsNullOrEmpty(song.FilePath))
            return;

        try
        {
            if (_playback == null || _loadedSongPath != song.FilePath)
            {
                DisposePlaybackInstance();
                var midiFile = MidiFile.Read(song.FilePath);
                _tempoMap = midiFile.GetTempoMap();
                _playback = midiFile.GetPlayback(_outputDevice!);
                _playback.Finished += OnPlaybackFinished;
                _loadedSongPath = song.FilePath;
            }

            if (_playback.IsRunning)
            {
                _pausedAtSeconds = GetPlaybackSeconds();
                _playback.Stop();
            }

            // 뒤로 Seek 시 DryWetMidi가 이미 지난 이벤트를 건너뛰어 일부 노트가 안 들림
            var seekTo = Math.Max(_pausedAtSeconds, startSeconds);
            if (Math.Abs(_pausedAtSeconds - seekTo) > 0.001)
            {
                _pausedAtSeconds = seekTo;
                SeekPlaybackToSeconds(seekTo);
            }
            else
            {
                _pausedAtSeconds = seekTo;
            }
        }
        catch
        {
            StopPlayback();
        }
    }

    public void StartFlowPlayback()
    {
        if (_playback == null)
            return;

        if (!_playback.IsRunning)
            _playback.Start();
    }

    public void PauseFlowPlayback()
    {
        if (_playback == null)
            return;

        if (_playback.IsRunning)
        {
            _pausedAtSeconds = GetPlaybackSeconds();
            _playback.Stop();
        }
    }

    public void HoldSongAt(MidiSong song, double positionSeconds) =>
        PrepareFlowPlayback(song, positionSeconds);

    public void PausePlaybackAt(double positionSeconds)
    {
        _pausedAtSeconds = positionSeconds;
        if (_playback?.IsRunning == true)
            _playback.Stop();

        if (_playback != null && _tempoMap != null)
            SeekPlaybackToSeconds(positionSeconds);
    }

    public double GetPlaybackSeconds()
    {
        if (_playback == null)
            return _pausedAtSeconds;

        try
        {
            return _playback.GetCurrentTime<MetricTimeSpan>().TotalMicroseconds / 1_000_000.0;
        }
        catch
        {
            return _pausedAtSeconds;
        }
    }

    public void PlayNote(int pitch, int velocity = 90, int durationMs = 280)
    {
        if (!TryEnsureDevice())
            return;

        var note = (SevenBitNumber)Math.Clamp(pitch, 0, 127);
        var vel = (SevenBitNumber)Math.Clamp(velocity, 1, 127);

        try
        {
            _outputDevice!.SendEvent(new NoteOnEvent(note, vel));
            Task.Delay(durationMs).ContinueWith(_ =>
            {
                try
                {
                    _outputDevice?.SendEvent(new NoteOffEvent(note, SevenBitNumber.MinValue));
                }
                catch
                {
                    // ignore
                }
            });
        }
        catch
        {
            // ignore
        }
    }

    public void StopPlayback()
    {
        DisposePlaybackInstance();
        _loadedSongPath = null;
        _tempoMap = null;
        _pausedAtSeconds = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopPlayback();
        _outputDevice?.Dispose();
        _outputDevice = null;
        _disposed = true;
    }

    private void SeekPlaybackToSeconds(double seconds)
    {
        if (_playback == null || _tempoMap == null)
            return;

        var metric = TimeConverter.ConvertTo<MetricTimeSpan>(
            new MetricTimeSpan((long)Math.Round(Math.Max(0, seconds) * 1_000_000.0)),
            _tempoMap);

        _playback.MoveToTime(metric);
    }

    private void DisposePlaybackInstance()
    {
        if (_playback != null)
            _playback.Finished -= OnPlaybackFinished;

        try
        {
            if (_playback?.IsRunning == true)
                _playback.Stop();

            _playback?.Dispose();
        }
        catch
        {
            // ignore
        }

        _playback = null;
    }

    private void AttachRhythmPlayback(string filePath, MidiFile midiFile)
    {
        _tempoMap = midiFile.GetTempoMap();
        _playback = midiFile.GetPlayback(_outputDevice!);
        _playback.Finished += OnPlaybackFinished;
        _loadedSongPath = filePath;
    }

    private void OnPlaybackFinished(object? sender, EventArgs e) => PlaybackFinished?.Invoke();

    private bool TryEnsureDevice()
    {
        if (_outputDevice != null)
            return true;

        try
        {
            var devices = OutputDevice.GetAll();
            if (devices.Count == 0)
                return false;

            _outputDevice = devices.FirstOrDefault(d =>
                d.Name.Contains("Microsoft GS", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("Wavetable", StringComparison.OrdinalIgnoreCase))
                ?? devices.First();

            return true;
        }
        catch
        {
            return false;
        }
    }
}
