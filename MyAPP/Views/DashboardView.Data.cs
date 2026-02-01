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
        #region Data Loading (Supabase) - Optimized for 8GB RAM

        private async Tasks.Task LoadPresetsAsync()
        {
            try
            {
                Coll.List<Models.Preset> presets = await Tasks.Task.Run(async () =>
                {
                    return await this._dataService.GetPresetsWithDetailsAsync().ConfigureAwait(false);
                }).ConfigureAwait(true);

                this._currentPresets = presets;

                foreach (Models.Preset preset in this._currentPresets)
                {
                    preset.UserId = Sys.Guid.Empty;
                }

                this.TxtPresetCount.Text = Sys.String.Concat(this._currentPresets.Count.ToString(), " preset");

                this._isSuppressingSelectionChange = true;
                try
                {
                    this.ListPresets.ItemsSource = this._currentPresets;

                    if (this._currentPresets.Count > 0 && this._selectedPreset == null)
                    {
                        this.ListPresets.SelectedIndex = 0;
                    }
                    else if (this._selectedPreset != null)
                    {
                        Models.Preset? existing = this._currentPresets.FirstOrDefault(p => p.Id == this._selectedPreset.Id);
                        if (existing != null)
                        {
                            this.ListPresets.SelectedItem = existing;
                            this._selectedPreset = existing; 
                        }
                    }
                }
                finally
                {
                    this._isSuppressingSelectionChange = false;
                }

                if (this._selectedPreset != null)
                {
                    this.RefreshCurrentPresetUI();
                }
                else
                {
                    this.ViewActivePreset.Visibility = Win.Visibility.Collapsed;
                }
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("LoadPresets Error: ", ex));
                this.ShowToast("Gagal memuat data", true);
            }
        }

        private void RefreshCurrentPresetUI()
        {
            if (this._selectedPreset == null)
            {
                return;
            }

            this.TxtActivePresetName.Text = this._selectedPreset.Name;
            this.ViewActivePreset.Visibility = Win.Visibility.Visible;

            if (this._selectedPreset.Templates != null && this._selectedPreset.Templates.Count > 0)
            {
                this.PanelContent.Visibility = Win.Visibility.Visible;
                this.PanelEmptyState.Visibility = Win.Visibility.Collapsed;
                this.TxtActivePresetSubtitle.Text = "Pilih preset dari daftar untuk mulai mengelola.";

                if (this._selectedTemplate != null)
                {
                    Models.Template? existingTemplate = this._selectedPreset.Templates.FirstOrDefault(t => t.Id == this._selectedTemplate.Id);
                    if (existingTemplate != null)
                    {
                        this.SelectTemplate(existingTemplate);
                    }
                    else
                    {
                        this.SelectTemplate(this._selectedPreset.Templates[0]);
                    }
                }
                else
                {
                    this.SelectTemplate(this._selectedPreset.Templates[0]);
                }
            }
            else
            {
                this._selectedTemplate = null;
                this.WrapTemplateButtons.Children.Clear();
                this.ItemsVariables.ItemsSource = null;
                this.TxtPreviewContent.Text = "";
                this.TxtProcessedPreview.Visibility = Win.Visibility.Collapsed;

                this.PanelContent.Visibility = Win.Visibility.Collapsed;
                this.PanelEmptyState.Visibility = Win.Visibility.Visible;
                this.TxtActivePresetSubtitle.Text = "Terakhir diperbarui: Baru saja";
            }
        }

        private void LoadTemplatesUI()
        {
            this.WrapTemplateButtons.Children.Clear();

            if (this._selectedPreset?.Templates == null)
            {
                return;
            }

            foreach (Models.Template template in this._selectedPreset.Templates)
            {
                Controls.Button btn = new Controls.Button
                {
                    Content = Sys.String.Concat("📄 ", template.Title),
                    Tag = template,
                    Margin = new Win.Thickness(0, 0, 8, 8),
                    Height = 38,
                    Padding = new Win.Thickness(16, 0, 16, 0)
                };

                if (this._selectedTemplate != null && this._selectedTemplate.Id == template.Id)
                {
                    btn.Style = (Win.Style)this.FindResource("TemplateTabActiveStyle");
                }
                else
                {
                    btn.Style = (Win.Style)this.FindResource("TemplateTabStyle");
                }

                btn.Click += (s, e) => this.SelectTemplate(template);

                this.WrapTemplateButtons.Children.Add(btn);
            }
        }

        private void SelectTemplate(Models.Template template)
        {
            this._selectedTemplate = template;
            this.LoadTemplatesUI();
            this.LoadVariablesForCurrentTemplate();
            this.UpdatePreviewContent(template.Content);

            this.LoadFileManager();
        }

        private void LoadVariablesForCurrentTemplate()
        {
            if (this._selectedTemplate == null)
            {
                this.ItemsVariables.ItemsSource = null;
                this.ItemsVariables.Visibility = Win.Visibility.Collapsed;
                this.PanelEmptyVariables.Visibility = Win.Visibility.Collapsed;
                return;
            }

            Coll.List<VariableViewModel> viewModels = new Coll.List<VariableViewModel>();

            if (this._selectedTemplate.Variables != null)
            {
                foreach (Models.Variable variable in this._selectedTemplate.Variables)
                {
                    viewModels.Add(new VariableViewModel
                    {
                        Id = variable.Id,
                        TemplateId = variable.TemplateId,
                        Name = variable.Name,
                        OriginalValue = variable.Value,
                        CurrentValue = Sys.String.Empty
                    });
                }
            }

            this.ItemsVariables.ItemsSource = viewModels;
            this.TxtProcessedPreview.Visibility = Win.Visibility.Collapsed;

            if (viewModels.Count == 0)
            {
                this.ItemsVariables.Visibility = Win.Visibility.Collapsed;
                this.PanelEmptyVariables.Visibility = Win.Visibility.Visible;
            }
            else
            {
                this.ItemsVariables.Visibility = Win.Visibility.Visible;
                this.PanelEmptyVariables.Visibility = Win.Visibility.Collapsed;
            }
        }

        private void ListPresets_SelectionChanged(Sys.Object sender, Controls.SelectionChangedEventArgs e)
        {
            if (this._isSuppressingSelectionChange)
            {
                return;
            }

            if (this.ListPresets.SelectedItem is Models.Preset preset)
            {
                this._selectedPreset = preset;
                this.TxtActivePresetName.Text = preset.Name;
                this.ViewActivePreset.Visibility = Win.Visibility.Visible;

                if (preset.Templates != null && preset.Templates.Count > 0)
                {
                    this.PanelContent.Visibility = Win.Visibility.Visible;
                    this.PanelEmptyState.Visibility = Win.Visibility.Collapsed;
                    this.TxtActivePresetSubtitle.Text = "Pilih preset dari daftar untuk mulai mengelola.";

                    this.SelectTemplate(preset.Templates[0]);
                }
                else
                {
                    this._selectedTemplate = null;
                    this.WrapTemplateButtons.Children.Clear();
                    this.ItemsVariables.ItemsSource = null;
                    this.TxtPreviewContent.Text = "";
                    this.TxtProcessedPreview.Visibility = Win.Visibility.Collapsed;

                    this.PanelContent.Visibility = Win.Visibility.Collapsed;
                    this.PanelEmptyState.Visibility = Win.Visibility.Visible;
                    this.TxtActivePresetSubtitle.Text = "Terakhir diperbarui: Baru saja";
                }
            }
        }

        #endregion
    }
}