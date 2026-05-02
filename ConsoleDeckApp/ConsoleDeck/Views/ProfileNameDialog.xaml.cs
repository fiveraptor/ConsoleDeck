using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ConsoleDeck.Core;

namespace ConsoleDeck.Views;

public partial class ProfileNameDialog : Window
{
    public string ProfileName => TbName.Text.Trim();

    public ProfileNameDialog(string title, string initialName)
    {
        InitializeComponent();
        TitleText.Text = title;
        TbName.Text = initialName;
        Loaded += (_, _) => { TbName.Focus(); TbName.SelectAll(); };
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

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbName.Text)) return;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TbName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  Ok_Click(sender, e);
        if (e.Key == Key.Escape) Cancel_Click(sender, e);
    }
}
