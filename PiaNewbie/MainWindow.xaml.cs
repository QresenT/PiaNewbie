using PiaNewbie.Services;
using PiaNewbie.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace PiaNewbie;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        viewModel.Navigation.PageChanged += OnPageChanged;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainWindowViewModel vm)
        {
            switch (vm.Navigation.CurrentPage)
            {
                case AppPage.Main:
                    vm.MainPage.ExitCommand.Execute(null);
                    e.Handled = true;
                    return;
                case AppPage.Setup:
                    vm.SetupPage.BackCommand.Execute(null);
                    e.Handled = true;
                    return;
                case AppPage.Result:
                    vm.ResultPage.RetrySetupCommand.Execute(null);
                    e.Handled = true;
                    return;
            }
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnPageChanged(AppPage page)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (page == AppPage.Result)
            vm.ResultPage.LoadFromSession();
    }
}
