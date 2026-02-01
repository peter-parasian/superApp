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
        private void BtnBackToMenu_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            Win.Window? window = Win.Window.GetWindow(this);
            if (window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowModeSelection();
            }
        }

        private void BtnLogout_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            Services.AuthState.ClearToken();

            Win.Window? window = Win.Window.GetWindow(this);
            if (window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowLogin();
            }
        }

        private async void BtnExport_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            try
            {
                Dialogs.SaveFileDialog dialog = new Dialogs.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    FileName = Sys.String.Concat("backup-presets-", Sys.DateTime.Now.ToString("yyyy-MM-dd"), ".json")
                };

                if (dialog.ShowDialog() == true)
                {
                    await this._dataService.ExportPresetsToJsonAsync(dialog.FileName).ConfigureAwait(true);
                    this.ShowToast("Ekspor berhasil");
                }
            }
            catch (Sys.Exception ex)
            {
                this.ShowToast(Sys.String.Concat("Gagal ekspor: ", ex.Message), true);
            }
        }

        private async void BtnImport_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            try
            {
                Dialogs.OpenFileDialog dialog = new Dialogs.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    await this._dataService.ImportPresetsFromJsonAsync(dialog.FileName).ConfigureAwait(true);
                    await this.LoadPresetsAsync();
                    this.ShowToast("Impor berhasil");
                }
            }
            catch (Sys.Exception ex)
            {
                this.ShowToast(Sys.String.Concat("Gagal impor: ", ex.Message), true);
            }
        }
    }
}