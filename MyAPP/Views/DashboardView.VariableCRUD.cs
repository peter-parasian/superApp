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
        #region CRUD: Variable

        private void BtnAddVariable_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedPreset == null || this._selectedTemplate == null)
            {
                this.ShowToast("Pilih templat dahulu", true);
                return;
            }

            this._currentMode = ModalMode.NewVariable;
            this.OpenModal("Tambah Variabel", "🏷️");
            this.FormVariable.Visibility = Win.Visibility.Visible;

            this.InputVarTemplate.ItemsSource = this._selectedPreset.Templates;
            this.InputVarTemplate.SelectedValue = this._selectedTemplate.Id;
            this.InputVarTemplate.IsEnabled = true;

            this.InputVarKey.Text = "";
            this.InputVarValue.Text = "";
        }

        private void BtnEditVariable_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is VariableViewModel variableVm)
            {
                Models.Variable? model = this._selectedTemplate?.Variables?.FirstOrDefault(v => v.Id == variableVm.Id);
                if (model != null)
                {
                    this._currentMode = ModalMode.EditVariable;
                    this._editingVariable = model;
                    this.OpenModal("Edit Variabel", "🏷️");
                    this.FormVariable.Visibility = Win.Visibility.Visible;

                    this.InputVarTemplate.ItemsSource = this._selectedPreset?.Templates;
                    this.InputVarTemplate.SelectedValue = model.TemplateId;
                    this.InputVarTemplate.IsEnabled = false;

                    this.InputVarKey.Text = model.Name;
                    this.InputVarValue.Text = model.Value ?? "";
                }
            }
        }

        private async void BtnDeleteVariable_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is VariableViewModel variableVm)
            {
                try
                {
                    await this._dataService.DeleteVariableAsync(variableVm.Id).ConfigureAwait(true);
                    await this.LoadPresetsAsync();

                    if (this._selectedTemplate != null)
                    {
                        this.SelectTemplate(this._selectedTemplate);
                    }
                    this.ShowToast("Variabel dihapus");
                }
                catch (Sys.Exception ex)
                {
                    this.ShowToast(Sys.String.Concat("Gagal menghapus: ", ex.Message), true);
                }
            }
        }

        private async Tasks.Task HandleSaveNewVariableAsync()
        {
            if (this.InputVarTemplate.SelectedValue is not Sys.Guid templateId)
            {
                throw new Sys.InvalidOperationException("Pilih templat terlebih dahulu.");
            }

            Sys.String name = this.InputVarKey.Text;
            Sys.String? value = this.InputVarValue.Text;

            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.InvalidOperationException("Nama variabel tidak boleh kosong.");
            }

            await this._dataService.CreateVariableAsync(templateId, name, value).ConfigureAwait(true);
            await this.LoadPresetsAsync();

            Models.Template? template = this._selectedPreset?.Templates?.FirstOrDefault(t => t.Id == templateId);
            if (template != null)
            {
                this.SelectTemplate(template);
            }
        }

        private async Tasks.Task HandleUpdateVariableAsync()
        {
            if (this._editingVariable == null)
            {
                return;
            }

            Sys.String name = this.InputVarKey.Text;
            Sys.String? value = this.InputVarValue.Text;

            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.InvalidOperationException("Nama variabel tidak boleh kosong.");
            }

            this._editingVariable.Name = name;
            this._editingVariable.Value = value;

            await this._dataService.UpdateVariableAsync(this._editingVariable).ConfigureAwait(true);

            await this.LoadPresetsAsync();
            if (this._selectedTemplate != null)
            {
                this.SelectTemplate(this._selectedTemplate);
            }
        }

        #endregion
    }
}