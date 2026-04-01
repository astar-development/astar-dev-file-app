using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using AStar.Dev.File.App.ViewModels;
using AStar.Dev.File.App.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace AStar.Dev.File.App;

public partial class App : Application
{
    private IServiceProvider? _services;

    public IServiceProvider? Services => _services;

    public T? GetService<T>() where T : class => _services?.GetService(typeof(T)) as T;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        // Apply EF migrations on startup
        var factory = _services.GetRequiredService<IDbContextFactory<FileAppDbContext>>();
        using var ctx = factory.CreateDbContext();
        ctx.Database.Migrate();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AStar.Dev.File.App",
            "files.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        services.AddDbContextFactory<FileAppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IFileTypeClassifier, FileTypeClassifier>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IFileDeleteService, FileDeleteService>();
        services.AddTransient<IFileScannerService, FileScannerService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DeletePendingViewModel>();

        return services.BuildServiceProvider();
    }
}