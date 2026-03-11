using AStar.Dev.File.App.Models;
using System;

namespace AStar.Dev.File.App.ViewModels;

public class ScannedFileDisplayItem
{
    public string FileName { get; }
    public string FolderPath { get; }
    public string FormattedSize { get; }
    public string FileType { get; }
    public string LastModified { get; }
    public string LastViewed { get; }
    public bool PendingDelete { get; }

    public ScannedFileDisplayItem(ScannedFile file)
    {
        FileName = file.FileName;
        FolderPath = file.FolderPath;
        FormattedSize = FormatSize(file.SizeInBytes);
        FileType = file.FileType.ToString();
        LastModified = file.LastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        LastViewed = file.LastViewed.HasValue
            ? file.LastViewed.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            : "—";
        PendingDelete = file.PendingDelete;
    }

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824L)
            return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576L)
            return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024L)
            return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }
}
