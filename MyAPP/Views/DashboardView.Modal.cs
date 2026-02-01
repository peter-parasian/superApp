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
        #region Modal Management

        private void OpenModal(Sys.String title, Sys.String icon = "📝")
        {
            this.ModalTitle.Text = title;
            this.ModalIcon.Text = icon;
            this.ModalOverlay.Visibility = Win.Visibility.Visible;

            this.FormPreset.Visibility = Win.Visibility.Collapsed;
            this.FormVariable.Visibility = Win.Visibility.Collapsed;
            this.FormTemplate.Visibility = Win.Visibility.Collapsed;
        }

        private void BtnCloseModal_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            this.ModalOverlay.Visibility = Win.Visibility.Collapsed;
            this._currentMode = ModalMode.None;
            this._editingVariable = null;
        }

        private async void BtnSaveModal_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            try
            {
                switch (this._currentMode)
                {
                    case ModalMode.NewPreset:
                        await this.HandleSaveNewPresetAsync();
                        break;
                    case ModalMode.EditPreset:
                        await this.HandleUpdatePresetAsync();
                        break;
                    case ModalMode.NewTemplate:
                        await this.HandleSaveNewTemplateAsync();
                        break;
                    case ModalMode.EditTemplate:
                        await this.HandleUpdateTemplateAsync();
                        break;
                    case ModalMode.NewVariable:
                        await this.HandleSaveNewVariableAsync();
                        break;
                    case ModalMode.EditVariable:
                        await this.HandleUpdateVariableAsync();
                        break;
                }

                this.BtnCloseModal_Click(sender, e);
                this.ShowToast("Data berhasil disimpan");
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("SaveModal Error: ", ex));
                this.ShowToast(ex.Message, true);
            }
        }

        #endregion
    }
}