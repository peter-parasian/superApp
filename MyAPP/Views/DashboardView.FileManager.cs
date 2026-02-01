using MahApps.Metro.IconPacks;
using System.Reflection;
using TextCopy;
using Coll = System.Collections.Generic;
using Comp = System.ComponentModel;
using Controls = System.Windows.Controls;
using Dialogs = Microsoft.Win32;
using IO = System.IO;
using Linq = System.Linq;
using Media = System.Windows.Media;
using Models = MyAPP.Models;
using Msg = System.Windows.MessageBox;
using Obj = System.Collections.ObjectModel;
using Services = MyAPP.Services;
using Sys = System;
using Tasks = System.Threading.Tasks;
using Threading = System.Threading;
using Win = System.Windows;

namespace MyAPP.Views
{
    public partial class DashboardView
    {
        #region File Manager Logic (Memory Optimized for 8GB RAM + SSD)

        private void LoadFileManager()
        {
            if (!IO.Directory.Exists(_uploadDir))
            {
                IO.Directory.CreateDirectory(_uploadDir);
            }

            Obj.ObservableCollection<FileSystemItem> items = new Obj.ObservableCollection<FileSystemItem>();
            BuildTree(_uploadDir, items);
            this.TreeFiles.ItemsSource = items;

            this.TxtCurrentPath.Text = "Storage/";
        }

        private static void BuildTree(Sys.String path, Obj.ObservableCollection<FileSystemItem> collection)
        {
            try
            {
                foreach (Sys.String dir in IO.Directory.EnumerateDirectories(path).OrderBy(d => d))
                {
                    FileSystemItem item = new FileSystemItem
                    {
                        Name = IO.Path.GetFileName(dir),
                        FullPath = dir,
                        IsFolder = true,
                        IsExpanded = true
                    };
                    BuildTree(dir, item.Children);
                    collection.Add(item);
                }

                foreach (Sys.String file in IO.Directory.EnumerateFiles(path).OrderBy(f => f))
                {
                    IO.FileInfo fi = new IO.FileInfo(file);
                    collection.Add(new FileSystemItem
                    {
                        Name = fi.Name,
                        FullPath = file,
                        IsFolder = false,
                        SizeDisplay = FormatSize(fi.Length)
                    });
                }
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine("Tree Build Error: " + ex.Message);
            }
        }

        private static Sys.String FormatSize(Sys.Int64 bytes)
        {
            if (bytes == 0)
            {
                return "0 B";
            }

            Sys.String[] suffixes = { "B", "KB", "MB", "GB" };
            Sys.Int32 i = 0;
            Sys.Double dbl = bytes;

            while (dbl >= 1024 && i < suffixes.Length - 1)
            {
                dbl /= 1024;
                i++;
            }

            return $"{dbl:0.#} {suffixes[i]}";
        }

        private void BtnRefreshFiles_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            this.LoadFileManager();
            this.ShowToast("File direfresh");
        }

        private async void BtnUploadFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            Dialogs.OpenFileDialog dlg = new Dialogs.OpenFileDialog
            {
                Multiselect = true,
                Title = "Pilih File untuk Diunggah"
            };

            if (dlg.ShowDialog() == true)
            {
                await this.UploadFilesAsync(dlg.FileNames).ConfigureAwait(true);
            }
        }

        private async Tasks.Task UploadFilesAsync(Sys.String[] files)
        {
            Sys.Int32 successCount = 0;

            foreach (Sys.String file in files)
            {
                Sys.String dest = IO.Path.Combine(_uploadDir, IO.Path.GetFileName(file));

                try
                {
                    using var sourceStream = new IO.FileStream(
                        file,
                        IO.FileMode.Open,
                        IO.FileAccess.Read,
                        IO.FileShare.Read,
                        FILE_BUFFER_SIZE,
                        IO.FileOptions.SequentialScan | IO.FileOptions.Asynchronous);

                    using var destStream = new IO.FileStream(
                        dest,
                        IO.FileMode.Create,
                        IO.FileAccess.Write,
                        IO.FileShare.None,
                        FILE_BUFFER_SIZE,
                        IO.FileOptions.Asynchronous);

                    await sourceStream.CopyToAsync(destStream).ConfigureAwait(false);
                    successCount++;
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Upload Fail: {file} - {ex.Message}");
                }
            }

            Win.Application.Current.Dispatcher.Invoke(() =>
            {
                this.LoadFileManager();
                this.ShowToast($"File disimpan ({successCount}/{files.Length})");
            });
        }

        private async void BtnUploadFolder_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            Dialogs.OpenFolderDialog dialog = new Dialogs.OpenFolderDialog
            {
                Title = "Pilih Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                Sys.String folderName = IO.Path.GetFileName(dialog.FolderName);
                Sys.String destDir = IO.Path.Combine(_uploadDir, folderName);

                await Tasks.Task.Run(() => this.CopyDirectoryAsync(dialog.FolderName, destDir)).ConfigureAwait(true);

                this.LoadFileManager();
                this.ShowToast("Folder disalin ke storage");
            }
        }

        private async Tasks.Task CopyDirectoryAsync(Sys.String sourceDir, Sys.String destinationDir)
        {
            IO.DirectoryInfo dir = new IO.DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                return;
            }

            IO.Directory.CreateDirectory(destinationDir);

            foreach (IO.FileInfo file in dir.GetFiles())
            {
                Sys.String destPath = IO.Path.Combine(destinationDir, file.Name);

                try
                {
                    using var sourceStream = new IO.FileStream(
                        file.FullName,
                        IO.FileMode.Open,
                        IO.FileAccess.Read,
                        IO.FileShare.Read,
                        FILE_BUFFER_SIZE,
                        IO.FileOptions.SequentialScan | IO.FileOptions.Asynchronous);

                    using var destStream = new IO.FileStream(
                        destPath,
                        IO.FileMode.Create,
                        IO.FileAccess.Write,
                        IO.FileShare.None,
                        FILE_BUFFER_SIZE,
                        IO.FileOptions.Asynchronous);

                    await sourceStream.CopyToAsync(destStream).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            foreach (IO.DirectoryInfo subDir in dir.GetDirectories())
            {
                Sys.String newDestDir = IO.Path.Combine(destinationDir, subDir.Name);
                await this.CopyDirectoryAsync(subDir.FullName, newDestDir).ConfigureAwait(false);
            }
        }

        private async void BtnDeleteFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is FileSystemItem item)
            {
                try
                {
                    await Tasks.Task.Run(() =>
                    {
                        if (item.IsFolder)
                        {
                            IO.Directory.Delete(item.FullPath, true);
                        }
                        else
                        {
                            IO.File.Delete(item.FullPath);
                        }
                    }).ConfigureAwait(true);

                    this.LoadFileManager();
                    this.ShowToast("Item berhasil dihapus");
                }
                catch (Sys.Exception ex)
                {
                    this.ShowToast("Gagal menghapus: " + ex.Message, true);
                }
            }
        }

        private async Tasks.Task ClearUploadedDataAsync()
        {
            if (!IO.Directory.Exists(_uploadDir))
            {
                return;
            }

            await Tasks.Task.Run(() =>
            {
                try
                {
                    IO.DirectoryInfo dir = new IO.DirectoryInfo(_uploadDir);

                    foreach (IO.FileInfo file in dir.GetFiles())
                    {
                        try { file.Delete(); } catch { /* Ignore individual file errors */ }
                    }

                    foreach (IO.DirectoryInfo subDir in dir.GetDirectories())
                    {
                        try { subDir.Delete(true); } catch { /* Ignore individual folder errors */ }
                    }
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Gagal membersihkan folder upload: {ex.Message}");
                }
            }).ConfigureAwait(false);
        }

        #endregion
    }
}