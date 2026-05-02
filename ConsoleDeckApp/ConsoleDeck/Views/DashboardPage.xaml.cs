using System.Windows;
using System.Windows.Controls;
using ConsoleDeck.ViewModels;

namespace ConsoleDeck.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage()
    {
        _vm = new DashboardViewModel();
        DataContext = _vm;
        InitializeComponent();
    }

    private void ButtonCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int number)
        {
            var win = new ButtonEditWindow(number) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
                _vm.Reload();
        }
    }
}
