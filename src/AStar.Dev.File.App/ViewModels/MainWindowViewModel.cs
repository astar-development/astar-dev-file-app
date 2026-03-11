using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    // Guards against cascading reloads when programmatically resetting CurrentPage
    private bool _suppressPageReload;

    public static IReadOnlyList<int> PageSizes { get; } = [25, 50, 75, 100, 125, 150, 175, 200];

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages), nameof(PagingInfo))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _pageSize = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PagingInfo))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages), nameof(PagingInfo))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _totalFileCount;

    public int TotalPages => TotalFileCount == 0 ? 1 : (int)Math.Ceiling((double)TotalFileCount / PageSize);

    public string PagingInfo => $"PAGE {CurrentPage} OF {TotalPages}  [{TotalFileCount} FILES]";

    public bool CanScan => !IsScanning && !string.IsNullOrWhiteSpace(SelectedFolderPath);

    public ObservableCollection<string> StatusMessages { get; } = [];
    public ObservableCollection<ScannedFileDisplayItem> ScannedFiles { get; } = [];

    public event Action<ScannedFileDisplayItem>? ViewFileRequested;

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
        TotalFileCount = 0;
        CurrentScanFolder = string.Empty;
        _suppressPageReload = true;
        CurrentPage = 1;
        _suppressPageReload = false;

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

    [RelayCommand]
    private async Task ViewFile(ScannedFileDisplayItem? item)
    {
        if (item is null) return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var file = await db.ScannedFiles.FindAsync(item.Id);
        if (file is not null)
        {
            file.LastViewed = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        ViewFileRequested?.Invoke(item);
    }

    [RelayCommand(CanExecute = nameof(IsScanning))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanGoFirst))]
    private void FirstPage() { CurrentPage = 1; }
    private bool CanGoFirst() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage() { CurrentPage--; }
    private bool CanGoPrevious() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage() { CurrentPage++; }
    private bool CanGoNext() => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanGoLast))]
    private void LastPage() { CurrentPage = TotalPages; }
    private bool CanGoLast() => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        if (!_suppressPageReload)
            _ = LoadScannedFilesAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        _suppressPageReload = true;
        CurrentPage = 1;
        _suppressPageReload = false;
        _ = LoadScannedFilesAsync();
    }

    private async Task LoadScannedFilesAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath))
            return;

        var prefix = SelectedFolderPath.TrimEnd(System.IO.Path.DirectorySeparatorChar,
                                                System.IO.Path.AltDirectorySeparatorChar)
                     + System.IO.Path.DirectorySeparatorChar;

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var query = db.ScannedFiles
            .Where(f => f.FullPath.StartsWith(prefix))
            .OrderBy(f => f.FolderPath)
            .ThenBy(f => f.FileName);

        TotalFileCount = await query.CountAsync();

        // Clamp current page if the page count shrank (e.g. after a page-size increase)
        if (CurrentPage > TotalPages)
        {
            _suppressPageReload = true;
            CurrentPage = TotalPages;
            _suppressPageReload = false;
        }

        var files = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ScannedFiles.Clear();
        foreach (var file in files)
            ScannedFiles.Add(new ScannedFileDisplayItem(file));
    }
}
