using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AStar.Dev.File.App.ViewModels;

public partial class DeletePendingViewModel : ViewModelBase
{
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private readonly IFileDeleteService _fileDeleteService;

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    private int _pendingDeleteCount;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ScannedFileDisplayItem> PendingDeleteFiles { get; } = [];

    public DeletePendingViewModel(
        IDbContextFactory<FileAppDbContext> dbContextFactory,
        IFileDeleteService fileDeleteService)
    {
        _dbContextFactory = dbContextFactory;
        _fileDeleteService = fileDeleteService;
        _ = LoadPendingFilesAsync();
    }

    [RelayCommand]
    private async Task TogglePendingDelete(ScannedFileDisplayItem? item)
    {
        if (item is null) return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var file = await db.ScannedFiles.FindAsync(item.Id);
        if (file is not null)
        {
            file.PendingDelete = !file.PendingDelete;
            await db.SaveChangesAsync();
        }

        await LoadPendingFilesAsync();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAll))]
    private async Task DeleteAll()
    {
        if (PendingDeleteFiles.Count == 0)
            return;

        IsDeleting = true;
        StatusMessage = "Deleting files...";

        try
        {
            var filePaths = PendingDeleteFiles.Select(f => f.FullPath).ToList();

            await _fileDeleteService.DeleteFilesAsync(filePaths, moveToRecycleBin: true);

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var ids = PendingDeleteFiles.Select(f => f.Id).ToList();
            var filesToRemove = await db.ScannedFiles.Where(f => ids.Contains(f.Id)).ToListAsync();
            foreach (var file in filesToRemove)
            {
                db.ScannedFiles.Remove(file);
            }
            await db.SaveChangesAsync();

            StatusMessage = $"Successfully deleted {filePaths.Count} file(s) to recycle bin.";
            await LoadPendingFilesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting files: {ex.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    private bool CanDeleteAll() => !IsDeleting && PendingDeleteFiles.Count > 0;

    [RelayCommand]
    private async Task ClearMarkings()
    {
        if (PendingDeleteFiles.Count == 0)
            return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var ids = PendingDeleteFiles.Select(f => f.Id).ToList();
        var files = await db.ScannedFiles.Where(f => ids.Contains(f.Id)).ToListAsync();
        foreach (var file in files)
        {
            file.PendingDelete = false;
        }
        await db.SaveChangesAsync();

        StatusMessage = "All delete markings cleared.";
        await LoadPendingFilesAsync();
    }

    private async Task LoadPendingFilesAsync()
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var files = await db.ScannedFiles
                .Where(f => f.PendingDelete)
                .OrderBy(f => f.FolderPath)
                .ThenBy(f => f.FileName)
                .ToListAsync();

            PendingDeleteFiles.Clear();
            files.ForEach(file => PendingDeleteFiles.Add(new ScannedFileDisplayItem(file)));

            PendingDeleteCount = PendingDeleteFiles.Count;
            DeleteAllCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading pending files: {ex.Message}";
        }
    }
}
