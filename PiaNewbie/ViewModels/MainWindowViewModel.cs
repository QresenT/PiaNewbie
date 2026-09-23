using CommunityToolkit.Mvvm.ComponentModel;
using PiaNewbie.Services;

namespace PiaNewbie.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public NavigationService Navigation { get; } = new();

    public MainWindowViewModel()
    {
        MainPage = new MainPageViewModel(Navigation);
        SetupPage = new SetupPageViewModel(Navigation);
        PracticePage = new PracticePageViewModel(Navigation);
        PracticeLoadingPage = new PracticeLoadingPageViewModel(Navigation);
        ResultPage = new ResultPageViewModel(Navigation);

        Navigation.PageChanged += OnPageChanged;
        CurrentViewModel = MainPage;
    }

    public MainPageViewModel MainPage { get; }
    public SetupPageViewModel SetupPage { get; }
    public PracticePageViewModel PracticePage { get; }
    public PracticeLoadingPageViewModel PracticeLoadingPage { get; }
    public ResultPageViewModel ResultPage { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    private void OnPageChanged(AppPage page)
    {
        CurrentViewModel = page switch
        {
            AppPage.Main => MainPage,
            AppPage.Setup => SetupPage,
            AppPage.PracticeLoading => PracticeLoadingPage,
            AppPage.Practice => PracticePage,
            AppPage.Result => ResultPage,
            _ => MainPage
        };
    }

    public void RefreshPractice() => PracticePage.Initialize();
}
