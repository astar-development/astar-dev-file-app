using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.Views;

public partial class ViewWindow : Window
{
    public ViewWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
