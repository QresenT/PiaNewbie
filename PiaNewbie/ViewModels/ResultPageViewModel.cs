using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PiaNewbie.Enums;
using PiaNewbie.Services;

namespace PiaNewbie.ViewModels;

public partial class ResultPageViewModel : ObservableObject
{
    private readonly NavigationService _navigation;

    public ResultPageViewModel(NavigationService navigation)
    {
        _navigation = navigation;
    }

    [ObservableProperty]
    private string _title = "Result";

    [ObservableProperty]
    private string _modeText = string.Empty;

    [ObservableProperty]
    private string _detailText = string.Empty;

    [ObservableProperty]
    private string _accuracyText = string.Empty;

    public void LoadFromSession()
    {
        var result = AppSession.Instance.LastResult;
        if (result == null)
        {
            Title = "No Result";
            ModeText = string.Empty;
            DetailText = string.Empty;
            AccuracyText = string.Empty;
            return;
        }

        ModeText = result.Mode == PracticeMode.Flow ? "Flow Mode" : "Rhythm Mode";
        Title = result.Completed ? "Track Complete" : "Track Ended";

        if (result.Mode == PracticeMode.Flow)
        {
            DetailText = $"진행한 노트: {result.HitNotes} / {result.TotalNotes}";
            AccuracyText = result.Completed ? string.Empty : "Stopped";
        }
        else
        {
            DetailText =
                $"전체 노트: {result.TotalNotes}\n" +
                $"성공: {result.HitNotes}\n" +
                $"놓침: {result.MissedNotes}\n" +
                $"콤보: {result.MaxCombo}";
            AccuracyText = $"{result.Accuracy:F1}%";
        }
    }

    [RelayCommand]
    private void RetrySetup() => _navigation.NavigateTo(AppPage.Setup);

    [RelayCommand]
    private void GoMain() => _navigation.NavigateTo(AppPage.Main);
}
