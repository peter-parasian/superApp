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
        #region CRUD: Preset

        private void BtnNewPreset_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            this._currentMode = ModalMode.NewPreset;
            this.OpenModal("Buat Preset Baru", "📁");
            this.FormPreset.Visibility = Win.Visibility.Visible;
            this.InputPresetName.Text = "";
        }

        private void BtnEditPreset_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is Models.Preset preset)
            {
                this._currentMode = ModalMode.EditPreset;
                this._selectedPreset = preset;
                this.OpenModal("Edit Preset", "📁");
                this.FormPreset.Visibility = Win.Visibility.Visible;
                this.InputPresetName.Text = preset.Name;
            }
        }

        private async void BtnDeletePreset_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is Models.Preset preset)
            {
                try
                {
                    await this._dataService.DeletePresetAsync(preset.Id).ConfigureAwait(true);

                    if (this._selectedPreset?.Id == preset.Id)
                    {
                        this._selectedPreset = null;
                        this._selectedTemplate = null;
                    }

                    await this.LoadPresetsAsync();
                    this.ShowToast("Preset dihapus");
                }
                catch (Sys.Exception ex)
                {
                    this.ShowToast(Sys.String.Concat("Gagal menghapus: ", ex.Message), true);
                }
            }
        }

        private async Tasks.Task HandleSaveNewPresetAsync()
        {
            Sys.String name = this.InputPresetName.Text;
            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.InvalidOperationException("Nama preset tidak boleh kosong.");
            }

            await this._dataService.CreatePresetAsync(name).ConfigureAwait(true);
            await this.LoadPresetsAsync();
        }

        private async Tasks.Task HandleUpdatePresetAsync()
        {
            if (this._selectedPreset == null)
            {
                return;
            }

            Sys.String name = this.InputPresetName.Text;
            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.InvalidOperationException("Nama preset tidak boleh kosong.");
            }

            this._selectedPreset.Name = name;
            await this._dataService.UpdatePresetAsync(this._selectedPreset).ConfigureAwait(true);
            await this.LoadPresetsAsync();
        }

        #endregion
    }
}