using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.File.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileScannerService _fileScannerService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyCanExecuteChangedFor(nameof(StartScanCommand))]
    private string _selectedFolderPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyCanExecuteChangedFor(nameof(StartScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isScanning;

    [ObservableProperty]
    private string _currentScanFolder = string.Empty;

    [ObservableProperty]
    private int _totalFilesProcessed;

    public bool CanScan => !IsScanning && !string.IsNullOrWhiteSpace(SelectedFolderPath);

    public ObservableCollection<string> StatusMessages { get; } = [];
    public ObservableCollection<ScannedFileDisplayItem> ScannedFiles { get; } = [];

    public MainWindowViewModel(
        IFileScannerService fileScannerService,
        IFolderPickerService folderPickerService,
        IDbContextFactory<FileAppDbContext> dbContextFactory)
    {
        _fileScannerService = fileScannerService;
        _folderPickerService = folderPickerService;
        _dbContextFactory = dbContextFactory;
    }

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private async Task SelectFolder()
    {
        var path = await _folderPickerService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
            SelectedFolderPath = path;
    }

    private bool CanSelectFolder() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task StartScan()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath) || IsScanning)
            return;

        IsScanning = true;
        StatusMessages.Clear();
        ScannedFiles.Clear();
        TotalFilesProcessed = 0;
        CurrentScanFolder = string.Empty;

        _cts = new CancellationTokenSource();

        var progress = new Progress<ScanProgressUpdate>(update =>
        {
            CurrentScanFolder = update.CurrentFolder;
            TotalFilesProcessed = update.TotalFilesProcessed;
            if (!string.IsNullOrEmpty(update.StatusMessage))
                StatusMessages.Add(update.StatusMessage);
        });

        try
        {
            await Task.Run(() => _fileScannerService.ScanAsync(SelectedFolderPath, progress, _cts.Token), _cts.Token);
            await LoadScannedFilesAsync();
        }
        catch (OperationCanceledException)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            StatusMessages.Add($"[{time}] [CANCELLED] Scan cancelled by user.");
        }
        finally
        {
            IsScanning = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsScanning))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private async Task LoadScannedFilesAsync()
    {
        var root = SelectedFolderPath;
        // Normalise so the StartsWith check works regardless of trailing separator
        var prefix = root.TrimEnd(System.IO.Path.DirectorySeparatorChar,
                                  System.IO.Path.AltDirectorySeparatorChar)
                     + System.IO.Path.DirectorySeparatorChar;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var files = await db.ScannedFiles
            .Where(f => f.FullPath.StartsWith(prefix))
            .OrderBy(f => f.FolderPath)
            .ThenBy(f => f.FileName)
            .ToListAsync();

        ScannedFiles.Clear();
        foreach (var file in files)
            ScannedFiles.Add(new ScannedFileDisplayItem(file));
    }
}
