using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PiaNewbie.Services;
using PiaNewbie.Views;
using System.Windows;

namespace PiaNewbie.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly NavigationService _navigation;

    public MainPageViewModel(NavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void Start() => _navigation.NavigateTo(AppPage.Setup);

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void ShowHelp()
    {
        var window = new HelpWindow
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
