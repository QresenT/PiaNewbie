namespace PiaNewbie.Services;

public enum AppPage
{
    Main,
    Setup,
    PracticeLoading,
    Practice,
    Result
}

public class NavigationService
{
    public event Action<AppPage>? PageChanged;
    public AppPage CurrentPage { get; private set; } = AppPage.Main;
    public void NavigateTo(AppPage page)
    {
        CurrentPage = page;
        PageChanged?.Invoke(page);
    }
}
