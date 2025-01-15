using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace MergeFiles2;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<FileItem> Files { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSortOrClear))]
    [NotifyCanExecuteChangedFor(nameof(MergeFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(SortFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearFilesCommand))]
    private int _fileCount;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "拖入文件夹或文件";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SortFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearFilesCommand))]
    private bool isRunning;

    public bool CanSortOrClear => !IsRunning && FileCount > 0;

    public MainWindowViewModel()
    {
        Files = new ObservableCollection<FileItem>();
        Files.CollectionChanged += (s, e) => FileCount = Files.Count;
    }

    [RelayCommand]
    private void DragDrop(object parameter)
    {
        if (parameter is not DragEventArgs e)
            return;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        var filePaths = GetFilesFromPaths(paths);

        foreach (var path in filePaths)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Extension.ToLower() != ".ts")
                continue;
            Files.Add(new FileItem
            {
                FilePath = fileInfo.FullName,
                ModifiedTime = fileInfo.LastWriteTime.ToString(),
                CreatedTime = fileInfo.CreationTime.ToString()
            });
        }
    }

    private IEnumerable<string> GetFilesFromPaths(string[] paths)
    {
        var filePaths = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                // 获取文件夹中的所有文件
                filePaths.AddRange(Directory.GetFiles(path, "*.ts"));
            }
            else if (File.Exists(path))
            {
                filePaths.Add(path);
            }
        }
        return filePaths.Distinct();
    }

    [RelayCommand(CanExecute = nameof(CanSortOrClear))]
    private void ClearFiles()
    {
        Files.Clear();
        StatusMessage = "";
    }

    [RelayCommand(CanExecute = nameof(CanSortOrClear))]
    private async Task MergeFilesAsync()
    {
        if (!Files.Any()) return;

        // 打开文件保存对话框
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "TS Files (*.ts)|*.ts|All Files (*.*)|*.*",
            FileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ts",
            Title = "选择输出文件位置"
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        string outputPath = saveFileDialog.FileName;

        try
        {
            IsRunning = true;
            StatusMessage = "正在合并文件...";
            Progress = 0;

            // 合并文件的异步逻辑
            await Task.Run(() =>
            {
                using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                var totalSize = Files.Sum(file => new FileInfo(file.FilePath).Length);
                long processedSize = 0;

                foreach (var file in Files)
                {
                    var fileInfo = new FileInfo(file.FilePath);
                    using var inputStream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                    int bytesRead;
                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                        processedSize += bytesRead;

                        // 更新进度
                        Progress = processedSize * 100.0 / totalSize;
                        StatusMessage = $"正在合并文件... {Progress / 100.0:P}";
                    }
                }
            });

            StatusMessage = $"文件合并成功！输出位置：{outputPath}";
            Progress = 100;
            MessageBox.Show($"文件合并成功！输出位置：{outputPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"文件合并失败：{ex.Message}";
            MessageBox.Show($"文件合并失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Progress = 0;
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSortOrClear))]
    private void SortFiles()
    {
        var sortedFiles = Files.OrderBy(file => file, new NaturalStringComparer()).ToList();
        Files.Clear();
        foreach (var file in sortedFiles)
        {
            Files.Add(file);
        }
        StatusMessage = "已排序完成";
    }
}

public class FileItem
{
    public string FilePath { get; set; }
    public string ModifiedTime { get; set; }
    public string CreatedTime { get; set; }
}
public class NaturalStringComparer : IComparer<FileItem>
{
    public int Compare(FileItem x, FileItem y)
    {
        return StrCmpLogicalW(x.FilePath, y.FilePath);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);
}
