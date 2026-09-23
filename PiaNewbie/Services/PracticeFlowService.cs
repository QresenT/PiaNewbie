using PiaNewbie.Models;

namespace PiaNewbie.Services;

public class PracticeFlowService
{
    private readonly List<GuideNote> _notes;
    private int _currentIndex;
    private bool _finished;

    public PracticeFlowService(IReadOnlyList<GuideNote> notes)
    {
        _notes = notes.ToList();
        _currentIndex = 0;
        UpdateCurrentFlags();
    }

    public int CurrentIndex => _currentIndex;
    public GuideNote? CurrentNote => _notes.Count > 0 && _currentIndex < _notes.Count ? _notes[_currentIndex] : null;
    public GuideNote? NextNote => _currentIndex + 1 < _notes.Count ? _notes[_currentIndex + 1] : null;
    public GuideNote? PreviousNote => _currentIndex > 0 ? _notes[_currentIndex - 1] : null;
    public bool IsCompleted => _finished || _notes.Count == 0;
    public int TotalNotes => _notes.Count;
    public int ProgressedNotes => _finished ? _notes.Count : _currentIndex + 1;

    public bool TryAdvance()
    {
        if (_finished || _notes.Count == 0)
            return false;

        // 마지막 노트를 친 뒤에만 완료 (여기서 끝내지 않음)
        if (_currentIndex >= _notes.Count - 1)
        {
            _finished = true;
            UpdateCurrentFlags();
            return true;
        }

        _currentIndex++;
        UpdateCurrentFlags();
        return true;
    }
    
    public void MarkCompleted()
    {
        _finished = true;
        _currentIndex = _notes.Count;
        UpdateCurrentFlags();
    }

    private void UpdateCurrentFlags()
    {
        for (var i = 0; i < _notes.Count; i++)
            _notes[i].IsCurrent = !_finished && i == _currentIndex;
    }
}
