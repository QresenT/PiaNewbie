using PiaNewbie.ViewModels;

namespace PiaNewbie.Views;

public partial class PracticeLoadingPage : System.Windows.Controls.UserControl
{
    public PracticeLoadingPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private PracticeLoadingPageViewModel? Vm => DataContext as PracticeLoadingPageViewModel;

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (Vm == null)
            return;

        await Vm.RunLoadingAsync();
    }
}
