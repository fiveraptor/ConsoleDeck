using System;
using System.Windows;
using System.Windows.Interop;
using ConsoleDeck.Core;

namespace ConsoleDeck.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmText = "Löschen")
    {
        InitializeComponent();
        TitleText.Text   = title;
        MessageText.Text = message;
        BtnConfirm.Content = confirmText;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ThemeService.ApplyTitleBar(this);
        ThemeService.ThemeChanged += RefreshTitleBar;
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.ThemeChanged -= RefreshTitleBar;
        base.OnClosed(e);
    }

    private void RefreshTitleBar() => Dispatcher.Invoke(() => ThemeService.ApplyTitleBar(this));

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e)  => DialogResult = false;
}
