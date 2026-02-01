using MahApps.Metro.IconPacks;
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
        #region File Manager Logic (Robust & Memory-Optimized for 8GB RAM)

        private static readonly Threading.SemaphoreSlim _fileOperationSemaphore = new Threading.SemaphoreSlim(1, 1);
        private Sys.Boolean _isFileManagerBusy = false;

        private async void LoadFileManager()
        {
            if (this._isFileManagerBusy)
            {
                return;
            }

            this._isFileManagerBusy = true;

            try
            {
                Obj.ObservableCollection<FileSystemItem> items = await Tasks.Task.Run(() =>
                {
                    if (!IO.Directory.Exists(_uploadDir))
                    {
                        IO.Directory.CreateDirectory(_uploadDir);
                    }

                    Obj.ObservableCollection<FileSystemItem> collection = new Obj.ObservableCollection<FileSystemItem>();
                    this.BuildTreeRecursive(_uploadDir, collection);
                    return collection;
                }).ConfigureAwait(false);

                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.TreeFiles.ItemsSource = items;
                    this.TxtCurrentPath.Text = "Storage/";
                });
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"LoadFileManager Error: {ex.Message}");
                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.ShowToast("Gagal memuat file manager", true);
                });
            }
            finally
            {
                this._isFileManagerBusy = false;
            }
        }

        private void BuildTreeRecursive(Sys.String path, Obj.ObservableCollection<FileSystemItem> collection)
        {
            try
            {
                Sys.String[] directories = IO.Directory.GetDirectories(path).OrderBy(d => d).ToArray();
                foreach (Sys.String dir in directories)
                {
                    FileSystemItem item = new FileSystemItem
                    {
                        Name = IO.Path.GetFileName(dir),
                        FullPath = dir,
                        IsFolder = true,
                        IsExpanded = true
                    };

                    this.BuildTreeRecursive(dir, item.Children);
                    collection.Add(item);
                }

                Sys.String[] files = IO.Directory.GetFiles(path).OrderBy(f => f).ToArray();
                foreach (Sys.String file in files)
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
            catch (Sys.UnauthorizedAccessException ex)
            {
                Sys.Console.WriteLine($"Access Denied: {path} - {ex.Message}");
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"BuildTree Error for {path}: {ex.Message}");
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
        }

        private async void BtnUploadFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (!await _fileOperationSemaphore.WaitAsync(0).ConfigureAwait(false))
            {
                this.ShowToast("Operasi file sedang berlangsung, mohon tunggu...", true);
                return;
            }

            Dialogs.OpenFileDialog dlg = new Dialogs.OpenFileDialog
            {
                Multiselect = true,
                Title = "Pilih File untuk Diunggah"
            };

            if (dlg.ShowDialog() == true)
            {
                if (dlg.FileNames == null || dlg.FileNames.Length == 0)
                {
                    _fileOperationSemaphore.Release();
                    return;
                }

                try
                {
                    Sys.Int32 successCount = await this.UploadFilesAsync(dlg.FileNames).ConfigureAwait(false);

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.LoadFileManager();
                        this.ShowToast($"File disimpan ({successCount}/{dlg.FileNames.Length})");
                    });
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Upload operation failed: {ex.Message}");
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.ShowToast("Upload gagal: Terjadi kesalahan sistem", true);
                    });
                }
                finally
                {
                    _fileOperationSemaphore.Release();
                }
            }
            else
            {
                _fileOperationSemaphore.Release();
            }
        }

        private async Tasks.Task<Sys.Int32> UploadFilesAsync(Sys.String[] files)
        {
            Sys.Int32 successCount = 0;

            foreach (Sys.String file in files)
            {
                Sys.String fileName = IO.Path.GetFileName(file);
                Sys.String dest = IO.Path.Combine(_uploadDir, fileName);

                Sys.Int32 counter = 1;
                Sys.String originalDest = dest;
                while (IO.File.Exists(dest))
                {
                    Sys.String nameWithoutExt = IO.Path.GetFileNameWithoutExtension(originalDest);
                    Sys.String extension = IO.Path.GetExtension(originalDest);
                    dest = IO.Path.Combine(_uploadDir, $"{nameWithoutExt} ({counter}){extension}");
                    counter++;
                }

                try
                {
                    await this.CopyFileWithStreamingAsync(file, dest).ConfigureAwait(false);
                    successCount++;
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Upload Fail: {file} - {ex.Message}");
                }

                await Tasks.Task.Yield();
            }

            return successCount;
        }

        private async Tasks.Task CopyFileWithStreamingAsync(Sys.String sourcePath, Sys.String destPath)
        {
            using var sourceStream = new IO.FileStream(
                sourcePath,
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

        private async void BtnUploadFolder_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (!await _fileOperationSemaphore.WaitAsync(0).ConfigureAwait(false))
            {
                this.ShowToast("Operasi file sedang berlangsung, mohon tunggu...", true);
                return;
            }

            Dialogs.OpenFolderDialog dialog = new Dialogs.OpenFolderDialog
            {
                Title = "Pilih Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                Sys.String folderName = IO.Path.GetFileName(dialog.FolderName);
                Sys.String destDir = IO.Path.Combine(_uploadDir, folderName);

                try
                {
                    await this.CopyDirectoryAsync(dialog.FolderName, destDir).ConfigureAwait(false);

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.LoadFileManager();
                        this.ShowToast("Folder disalin ke storage");
                    });
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Folder upload failed: {ex.Message}");
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.ShowToast("Upload folder gagal", true);
                    });
                }
                finally
                {
                    _fileOperationSemaphore.Release();
                }
            }
            else
            {
                _fileOperationSemaphore.Release();
            }
        }

        private async Tasks.Task CopyDirectoryAsync(Sys.String sourceDir, Sys.String destinationDir)
        {
            IO.DirectoryInfo dir = new IO.DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                return;
            }

            await Tasks.Task.Run(() => IO.Directory.CreateDirectory(destinationDir)).ConfigureAwait(false);

            IO.FileInfo[] files = dir.GetFiles();
            foreach (IO.FileInfo file in files)
            {
                Sys.String destPath = IO.Path.Combine(destinationDir, file.Name);

                try
                {
                    await this.CopyFileWithStreamingAsync(file.FullName, destPath).ConfigureAwait(false);
                }
                catch (Sys.Exception ex)
                {
                    Sys.Console.WriteLine($"Skip file {file.Name}: {ex.Message}");
                }

                await Tasks.Task.Yield();
            }

            IO.DirectoryInfo[] subDirs = dir.GetDirectories();
            foreach (IO.DirectoryInfo subDir in subDirs)
            {
                Sys.String newDestDir = IO.Path.Combine(destinationDir, subDir.Name);
                await this.CopyDirectoryAsync(subDir.FullName, newDestDir).ConfigureAwait(false);
            }
        }

        private async void BtnDeleteFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is FileSystemItem item)
            {
                if (!await _fileOperationSemaphore.WaitAsync(0).ConfigureAwait(false))
                {
                    this.ShowToast("Operasi file sedang berlangsung...", true);
                    return;
                }

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
                    }).ConfigureAwait(false);

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.LoadFileManager();
                        this.ShowToast("Item berhasil dihapus");
                    });
                }
                catch (Sys.Exception ex)
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.ShowToast("Gagal menghapus: " + ex.Message, true);
                    });
                }
                finally
                {
                    _fileOperationSemaphore.Release();
                }
            }
        }

        private async Tasks.Task ClearUploadedDataAsync()
        {
            if (!IO.Directory.Exists(_uploadDir))
            {
                return;
            }

            await Tasks.Task.Run(async () =>
            {
                try
                {
                    IO.DirectoryInfo dir = new IO.DirectoryInfo(_uploadDir);

                    foreach (IO.FileInfo file in dir.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {
                        }

                        await Tasks.Task.Yield();
                    }

                    foreach (IO.DirectoryInfo subDir in dir.GetDirectories())
                    {
                        try
                        {
                            subDir.Delete(true);
                        }
                        catch
                        {
                        }

                        await Tasks.Task.Yield();
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