using Sys = System;
using Win = System.Windows;
using Controls = System.Windows.Controls;
using Msg = System.Windows.MessageBox;
using MsgButton = System.Windows.MessageBoxButton;
using MsgImage = System.Windows.MessageBoxImage;
using MsgBoxResult = System.Windows.MessageBoxResult;
using Tasks = System.Threading.Tasks;
using Coll = System.Collections.Generic;
using Models = MyAPP.Models;
using Services = MyAPP.Services;
using Linq = System.Linq;
using IO = System.IO;
using Json = System.Text.Json;
using Windows = System.Windows;
using Media = System.Windows.Media;
using TextCopy;
using Comp = System.ComponentModel;
using MahApps.Metro.IconPacks;

namespace MyAPP.Views
{
    public partial class DashboardView : Controls.UserControl
    {
        private enum ModalMode { None, NewPreset, EditPreset, NewVariable, EditVariable, NewTemplate, EditTemplate }

        private readonly Services.SupabaseDataService _dataService;
        private ModalMode _currentMode = ModalMode.None;
        private Models.Preset? _selectedPreset = null;
        private Models.Template? _selectedTemplate = null;
        private Models.Variable? _editingVariable = null;

        private Coll.List<Models.Preset> _currentPresets = new Coll.List<Models.Preset>();
        private Coll.List<Models.Template> _currentTemplates = new Coll.List<Models.Template>();
        private Coll.List<Models.Variable> _currentVariables = new Coll.List<Models.Variable>();

        public class VariableViewModel : Comp.INotifyPropertyChanged
        {
            private Sys.String _currentValue = Sys.String.Empty;

            public Sys.Guid Id { get; set; }
            public Sys.Guid TemplateId { get; set; }
            public Sys.String Name { get; set; } = string.Empty;
            public Sys.String? OriginalValue { get; set; }

            public Sys.String CurrentValue
            {
                get => _currentValue;
                set
                {
                    if (_currentValue != value)
                    {
                        _currentValue = value;
                        OnPropertyChanged(nameof(CurrentValue));
                    }
                }
            }

            public event Comp.PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged(Sys.String propertyName)
            {
                PropertyChanged?.Invoke(this, new Comp.PropertyChangedEventArgs(propertyName));
            }

            public Models.Variable ToModel()
            {
                return new Models.Variable
                {
                    Id = this.Id,
                    TemplateId = this.TemplateId,
                    Name = this.Name,
                    Value = this.CurrentValue
                };
            }
        }

        public DashboardView()
        {
            this.InitializeComponent();
            this._dataService = new Services.SupabaseDataService();
            this.Loaded += async (s, e) => await this.LoadPresetsAsync();
        }

        #region Toast Notification Helper

        private async void ShowToast(Sys.String message, Sys.Boolean isError = false)
        {
            this.ToastText.Text = message;

            if (isError)
            {
                this.ToastIcon.Kind = PackIconMaterialKind.AlertCircle;
                this.ToastIcon.Foreground = Media.Brushes.Red;
            }
            else
            {
                this.ToastIcon.Kind = PackIconMaterialKind.CheckCircle;
                this.ToastIcon.Foreground = Media.Brushes.LightGreen;
            }

            this.ToastNotification.Visibility = Win.Visibility.Visible;
            this.ToastNotification.Opacity = 1;

            await Tasks.Task.Delay(1500);

            this.ToastNotification.Visibility = Win.Visibility.Collapsed;
        }

        #endregion

        #region Data Loading

        private async Tasks.Task LoadPresetsAsync()
        {
            try
            {
                this._currentPresets = await this._dataService.GetPresetsWithDetailsAsync().ConfigureAwait(true);

                foreach (Models.Preset preset in this._currentPresets)
                {
                    int varCount = 0;
                    if (preset.Templates != null)
                    {
                        foreach (Models.Template template in preset.Templates)
                        {
                            if (template.Variables != null)
                            {
                                varCount += template.Variables.Count;
                            }
                        }
                    }
                    preset.UserId = Sys.Guid.Empty;
                }

                this.TxtPresetCount.Text = Sys.String.Concat(this._currentPresets.Count.ToString(), " preset");
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
                    }
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
                    Margin = new Windows.Thickness(0, 0, 8, 8),
                    Height = 38,
                    Padding = new Windows.Thickness(16, 0, 16, 0)
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
        }

        private void LoadVariablesForCurrentTemplate()
        {
            if (this._selectedTemplate == null)
            {
                this.ItemsVariables.ItemsSource = null;
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
        }

        #endregion

        #region UI Event Handlers

        private void ListPresets_SelectionChanged(Sys.Object sender, Controls.SelectionChangedEventArgs e)
        {
            if (this.ListPresets.SelectedItem is Models.Preset preset)
            {
                this._selectedPreset = preset;
                this.TxtActivePresetName.Text = preset.Name;
                this.ViewActivePreset.Visibility = Win.Visibility.Visible;

                if (preset.Templates != null && preset.Templates.Count > 0)
                {
                    this.SelectTemplate(preset.Templates[0]);
                }
                else
                {
                    this._selectedTemplate = null;
                    this.WrapTemplateButtons.Children.Clear();
                    this.ItemsVariables.ItemsSource = null;
                    this.TxtPreviewContent.Text = "Tidak ada templat. Buat templat baru.";
                    this.TxtProcessedPreview.Visibility = Win.Visibility.Collapsed;
                }
            }
        }

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
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
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
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
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

        #endregion

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
                this.ShowToast("Disimpan"); 
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("SaveModal Error: ", ex));
                this.ShowToast(ex.Message, true);
            }
        }

        #endregion

        #region Preset CRUD

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

        #region Template CRUD

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

            this._selectedTemplate.Title = title;
            this._selectedTemplate.Content = content;

            await this._dataService.UpdateTemplateAsync(this._selectedTemplate).ConfigureAwait(true);

            await this.LoadPresetsAsync();
            this.UpdatePreviewContent(content);
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

        #region Variable CRUD

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

        #region Preview & Processing

        private void BtnCheckPreview_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                return;
            }

            Sys.String processed = this.GenerateProcessedContent(this._selectedTemplate);
            this.TxtProcessedPreview.Text = processed;
            this.TxtProcessedPreview.Visibility = Win.Visibility.Visible;
        }

        private async void BtnCopyResult_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                return;
            }

            Sys.String processed = this.GenerateProcessedContent(this._selectedTemplate);

            try
            {
                await TextCopy.ClipboardService.SetTextAsync(processed);

                if (this.ItemsVariables.ItemsSource is Coll.List<VariableViewModel> variables)
                {
                    foreach (var variable in variables)
                    {
                        variable.CurrentValue = Sys.String.Empty;
                    }
                }

                this.ShowToast("Tersalin & input bersih");
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("Copy Error: ", ex));
                this.ShowToast(Sys.String.Concat("Gagal menyalin: ", ex.Message), true);
            }
        }

        private Sys.String GenerateProcessedContent(Models.Template template)
        {
            Sys.String content = template.Content ?? "";
            Coll.List<VariableViewModel>? variables = this.ItemsVariables.ItemsSource as Coll.List<VariableViewModel>;

            if (variables == null)
            {
                return content;
            }

            foreach (VariableViewModel variable in variables)
            {
                Sys.String safeName = variable.Name.Replace(@"\", @"\\").Replace("[", @"\[").Replace("]", @"\]");
                Sys.String pattern = Sys.String.Concat(@"\{\{\s*", safeName, @"\s*\}\}");

                Sys.String valueToUse = Sys.String.IsNullOrWhiteSpace(variable.CurrentValue)
                                        ? (variable.OriginalValue ?? "")
                                        : variable.CurrentValue;

                content = Sys.Text.RegularExpressions.Regex.Replace(
                    content,
                    pattern,
                    valueToUse,
                    Sys.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return content;
        }

        #endregion
    }
}