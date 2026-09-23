using PiaNewbie.Models;

namespace PiaNewbie.Services;

public class AccompanimentPlaybackService
{
    private BackgroundNote[] _events = [];
    private int _nextIndex;
    private double _lastEmittedTime;

    public void Load(IReadOnlyList<BackgroundNote> notes)
    {
        _events = notes as BackgroundNote[] ?? notes.ToArray();
        if (_events.Length > 1)
            Array.Sort(_events, (a, b) => a.StartTime.CompareTo(b.StartTime));
    }

    public void Reset(double timeSeconds)
    {
        _lastEmittedTime = timeSeconds;
        _nextIndex = FindFirstAfter(timeSeconds);
    }

    public void AdvanceTo(double newTime, MidiAudioService audio, double maxSpanSeconds = 0.5, int maxPerStep = 128)
    {
        if (_events.Length == 0 || newTime <= _lastEmittedTime + 1e-9)
            return;

        while (_lastEmittedTime < newTime - 1e-9 && _nextIndex < _events.Length)
        {
            var to = maxSpanSeconds > 0
                ? Math.Min(newTime, _lastEmittedTime + maxSpanSeconds)
                : newTime;

            var played = Emit(_lastEmittedTime, to, audio, maxPerStep);
            _lastEmittedTime = to;

            if (played == 0 && to < newTime - 1e-9)
                break;
        }
    }

    private int Emit(double fromExclusive, double toInclusive, MidiAudioService audio, int maxCount)
    {
        var played = 0;
        while (_nextIndex < _events.Length && played < maxCount)
        {
            var e = _events[_nextIndex];
            if (e.StartTime > toInclusive + 1e-9)
                break;

            if (e.StartTime > fromExclusive + 1e-9)
            {
                var ms = (int)Math.Clamp(e.Duration * 1000, 40, 4000);
                audio.PlayNote(e.Pitch, e.Velocity > 0 ? e.Velocity : 80, ms);
                played++;
            }

            _nextIndex++;
        }

        return played;
    }

    private int FindFirstAfter(double t)
    {
        var lo = 0;
        var hi = _events.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (_events[mid].StartTime <= t + 1e-9)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }
}
