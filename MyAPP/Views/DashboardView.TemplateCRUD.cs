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
        #region CRUD: Template (With Bug Fix for Selection State)

        private void BtnAddTemplate_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedPreset == null)
            {
                this.ShowToast("Pilih preset dahulu", true);
                return;
            }

            this._currentMode = ModalMode.NewTemplate;
            this.OpenModal("Tambah Templat Baru", "📝");
            this.FormTemplate.Visibility = Win.Visibility.Visible;
            this.InputTemplateTitle.Text = "";
            this.InputTemplateContent.Text = "";
        }

        private async Tasks.Task HandleSaveNewTemplateAsync()
        {
            if (this._selectedPreset == null)
            {
                return;
            }

            Sys.String title = this.InputTemplateTitle.Text;
            Sys.String content = this.InputTemplateContent.Text;

            if (Sys.String.IsNullOrWhiteSpace(title))
            {
                throw new Sys.InvalidOperationException("Judul templat tidak boleh kosong.");
            }

            Models.Template created = await this._dataService.CreateTemplateAsync(
                this._selectedPreset.Id,
                title,
                content).ConfigureAwait(true);

            await this.LoadPresetsAsync();

            this._selectedTemplate = created;
            this.SelectTemplate(created);
        }

        private async Tasks.Task HandleUpdateTemplateAsync()
        {
            if (this._selectedTemplate == null)
            {
                return;
            }

            Sys.String title = this.InputTemplateTitle.Text;
            Sys.String content = this.InputTemplateContent.Text;

            if (Sys.String.IsNullOrWhiteSpace(title))
            {
                throw new Sys.InvalidOperationException("Judul tidak boleh kosong.");
            }

            Sys.Guid templateId = this._selectedTemplate.Id;

            this._selectedTemplate.Title = title;
            this._selectedTemplate.Content = content;

            await this._dataService.UpdateTemplateAsync(this._selectedTemplate).ConfigureAwait(true);

            await this.LoadPresetsAsync();

            await this.RestoreTemplateSelectionAsync(templateId).ConfigureAwait(true);

            this.UpdatePreviewContent(content);
        }

        private async Tasks.Task RestoreTemplateSelectionAsync(Sys.Guid templateIdToSelect)
        {
            await Tasks.Task.Run(() =>
            {
                Win.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (this._selectedPreset != null)
                    {
                        Models.Preset? refreshedPreset = this._currentPresets.FirstOrDefault(p => p.Id == this._selectedPreset.Id);

                        if (refreshedPreset != null)
                        {
                            this._selectedPreset = refreshedPreset;
                            this.ListPresets.SelectedItem = refreshedPreset;

                            Models.Template? refreshedTemplate = this._selectedPreset.Templates?.FirstOrDefault(t => t.Id == templateIdToSelect);

                            if (refreshedTemplate != null)
                            {
                                this._selectedTemplate = refreshedTemplate;
                                this.SelectTemplate(this._selectedTemplate);
                            }
                        }
                    }
                });
            });
        }

        private async void BtnDeleteTemplate_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                this.ShowToast("Pilih templat dahulu", true);
                return;
            }

            try
            {
                await this._dataService.DeleteTemplateAsync(this._selectedTemplate.Id).ConfigureAwait(true);
                this._selectedTemplate = null;
                await this.LoadPresetsAsync();
                this.ShowToast("Templat dihapus");
            }
            catch (Sys.Exception ex)
            {
                this.ShowToast(Sys.String.Concat("Gagal menghapus: ", ex.Message), true);
            }
        }

        private void BtnEditPreviewContent_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                this.ShowToast("Pilih templat dahulu", true);
                return;
            }

            this._currentMode = ModalMode.EditTemplate;
            this.OpenModal("Edit Konten Templat", "📝");
            this.FormTemplate.Visibility = Win.Visibility.Visible;
            this.InputTemplateTitle.Text = this._selectedTemplate.Title;
            this.InputTemplateContent.Text = this._selectedTemplate.Content;
        }

        private void UpdatePreviewContent(Sys.String content)
        {
            this.TxtPreviewContent.Text = content;
        }

        #endregion
    }
}