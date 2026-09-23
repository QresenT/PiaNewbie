using PiaNewbie.ViewModels;
using System.Windows.Controls;

namespace PiaNewbie.Views;

public partial class ResultPage : System.Windows.Controls.UserControl
{
    public ResultPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ResultPageViewModel vm)
                vm.LoadFromSession();
        };
    }
}
