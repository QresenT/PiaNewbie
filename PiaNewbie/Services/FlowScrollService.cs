namespace PiaNewbie.Services;

/// <summary>Flow 모드: 노트 처리 후 다음 노트까지 실시간 스크롤(틱당 진행량 상한).</summary>
public class FlowScrollService
{
    private const double MaxStepSeconds = 0.05;

    private double _scrollFrom;
    private double _scrollTo;
    private double _scrollDurationSeconds;
    private double _scrollProgress;

    public double DisplayTime { get; private set; }

    public bool IsScrolling { get; private set; }

    public void ResetToTime(double timeSeconds)
    {
        DisplayTime = timeSeconds;
        IsScrolling = false;
        _scrollProgress = 0;
    }

    public void BeginScrollTo(double targetStartTime)
    {
        _scrollFrom = DisplayTime;
        _scrollTo = targetStartTime;
        _scrollDurationSeconds = _scrollTo - _scrollFrom;
        _scrollProgress = 0;

        if (_scrollDurationSeconds <= 1e-6)
        {
            DisplayTime = _scrollTo;
            IsScrolling = false;
            return;
        }

        IsScrolling = true;
    }

    public bool Tick(double maxDeltaSeconds)
    {
        var step = Math.Clamp(maxDeltaSeconds, 0.001, MaxStepSeconds);
        if (!IsScrolling)
            return false;

        _scrollProgress += step / _scrollDurationSeconds;
        if (_scrollProgress >= 1.0)
        {
            _scrollProgress = 1.0;
            DisplayTime = _scrollTo;
            IsScrolling = false;
        }
        else
        {
            DisplayTime = _scrollFrom + (_scrollTo - _scrollFrom) * _scrollProgress;
        }

        return true;
    }

    public void Stop() => IsScrolling = false;
}
