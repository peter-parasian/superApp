using Win = System.Windows;
using IO = System.IO;
using Sys = System;

namespace MyAPP
{
    public partial class App : Win.Application
    {
        protected override void OnStartup(Win.StartupEventArgs e)
        {
            base.OnStartup(e);

            MyAPP.MainWindow mainWindow = new MyAPP.MainWindow();

            if (MyAPP.Services.AuthState.TryLoadFromDisk())
            {
                mainWindow.ShowModeSelection();
            }
            else
            {
                mainWindow.ShowLogin();
            }

            mainWindow.Show();
        }

        protected override void OnExit(Win.ExitEventArgs e)
        {
            this.CleanupUploadedFiles();
            base.OnExit(e);
        }

        private void CleanupUploadedFiles()
        {
            try
            {
                Sys.String uploadDir = IO.Path.Combine(
                    Sys.Environment.GetFolderPath(Sys.Environment.SpecialFolder.LocalApplicationData),
                    "MyAPP",
                    "uploaded"
                );

                if (IO.Directory.Exists(uploadDir))
                {
                    IO.DirectoryInfo dir = new IO.DirectoryInfo(uploadDir);

                    foreach (IO.FileInfo file in dir.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {
                        }
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
                    }

                    try
                    {
                        dir.Delete();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"Failed to cleanup upload directory on exit: {ex.Message}");
            }
        }
    }
}