using AStar.Dev.File.App.ViewModels;
using Avalonia.Controls;
using System.Collections.Specialized;

namespace AStar.Dev.File.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StatusMessages.CollectionChanged += OnStatusMessagesChanged;
        }
    }

    private void OnStatusMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StatusScrollViewer.ScrollToEnd();
    }
}