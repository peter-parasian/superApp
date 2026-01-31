using Sys = System;
using Win = System.Windows;
using Controls = System.Windows.Controls;
using Msg = System.Windows.MessageBox;
using Tasks = System.Threading.Tasks;
using Coll = System.Collections.Generic;
using Models = MyAPP.Models;
using Services = MyAPP.Services;
using Linq = System.Linq;
using IO = System.IO;
using Media = System.Windows.Media;
using TextCopy;
using Comp = System.ComponentModel;
using MahApps.Metro.IconPacks;
using Obj = System.Collections.ObjectModel;
using Dialogs = Microsoft.Win32;

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

        private readonly Sys.String _uploadDir = IO.Path.Combine(
            Sys.Environment.GetFolderPath(Sys.Environment.SpecialFolder.LocalApplicationData),
            "MyAPP",
            "uploaded"
        );

        // Map ekstensi file ke bahasa pemrograman untuk syntax highlighting markdown (Bagian dari fitur {{xxc}})
        // FIX: Menghapus duplikasi key (.R dan .r) karena Dictionary menggunakan IgnoreCase
        private static readonly Coll.Dictionary<Sys.String, Sys.String> _langMap = new Coll.Dictionary<Sys.String, Sys.String>(Sys.StringComparer.OrdinalIgnoreCase)
        {
            {".py", "python"}, {".ipynb", "python"}, {".js", "javascript"}, {".mjs", "javascript"},
            {".cjs", "javascript"}, {".ts", "typescript"}, {".tsx", "tsx"}, {".jsx", "jsx"},
            {".java", "java"}, {".kt", "kotlin"}, {".kts", "kotlin"}, {".cs", "csharp"},
            {".vb", "vbnet"}, {".rb", "ruby"}, {".php", "php"}, {".go", "go"}, {".rs", "rust"},
            {".swift", "swift"}, {".dart", "dart"}, {".scala", "scala"}, {".lua", "lua"},
            {".pl", "perl"}, {".pm", "perl"}, {".t", "perl"}, {".groovy", "groovy"},
            {".clj", "clojure"}, {".cljs", "clojure"}, {".edn", "clojure"}, {".lisp", "lisp"},
            {".scm", "scheme"}, {".rkt", "racket"}, {".hs", "haskell"}, {".lhs", "haskell"},
            {".ml", "ocaml"}, {".mli", "ocaml"}, {".erl", "erlang"}, {".hrl", "erlang"},
            {".ex", "elixir"}, {".exs", "elixir"}, {".r", "r"}, {".jl", "julia"}, // .R removed to prevent crash
            {".mat", "matlab"}, {".m", "matlab"},
            {".c", "c"}, {".h", "c"}, {".cpp", "cpp"}, {".cc", "cpp"}, {".cxx", "cpp"},
            {".hpp", "cpp"}, {".hh", "cpp"}, {".hxx", "cpp"}, {".ino", "arduino"},
            {".asm", "assembly"}, {".s", "assembly"},
            {".html", "html"}, {".htm", "html"}, {".xhtml", "html"}, {".xml", "xml"},
            {".svg", "xml"}, {".css", "css"}, {".scss", "scss"}, {".sass", "sass"},
            {".less", "less"}, {".vue", "vue"},
            {".md", "markdown"}, {".markdown", "markdown"}, {".rst", "restructuredtext"},
            {".tex", "latex"}, {".bib", "bibtex"}, {".txt", "text"}, {".adoc", "asciidoc"},
            {".json", "json"}, {".jsonc", "jsonc"}, {".yaml", "yaml"}, {".yml", "yaml"},
            {".toml", "toml"}, {".ini", "ini"}, {".cfg", "ini"}, {".conf", "ini"},
            {".env", "env"}, {".properties", "ini"},
            {".sh", "bash"}, {".bash", "bash"}, {".zsh", "bash"}, {".ksh", "bash"},
            {".fish", "fish"}, {".ps1", "powershell"}, {".psm1", "powershell"},
            {".bat", "batch"}, {".cmd", "batch"},
            {".sql", "sql"}, {".sqlite", "sql"}, {".csv", "csv"}, {".tsv", "tsv"},
            {"dockerfile", "docker"}, {".dockerfile", "docker"}, {".cshtml", "razor"},
            {".razor", "razor"}, {".gradle", "gradle"}, {".make", "makefile"},
            {"makefile", "makefile"}, {"cmakelists.txt", "cmake"}, {".cmake", "cmake"},
            {"package.json", "json"}, {"composer.json", "json"}, {"pom.xml", "xml"},
            {".log", "log"}, {".lock", "text"}
        };

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
        }

        public class FileSystemItem
        {
            public Sys.String Name { get; set; } = string.Empty;
            public Sys.String FullPath { get; set; } = string.Empty;
            public Sys.Boolean IsFolder { get; set; }

            public PackIconMaterialKind IconKind => IsFolder ? PackIconMaterialKind.Folder : PackIconMaterialKind.FileDocumentOutline;
            public Media.Brush IconColor => IsFolder ? Media.Brushes.Orange : Media.Brushes.Gray;

            public Sys.String SizeDisplay { get; set; } = string.Empty;
            public Sys.Boolean IsExpanded { get; set; } = false;
            public Obj.ObservableCollection<FileSystemItem> Children { get; set; } = new Obj.ObservableCollection<FileSystemItem>();
        }

        public DashboardView()
        {
            this.InitializeComponent();
            this._dataService = new Services.SupabaseDataService();

            if (!IO.Directory.Exists(_uploadDir))
            {
                IO.Directory.CreateDirectory(_uploadDir);
            }

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

        #region Data Loading (Supabase)

        private async Tasks.Task LoadPresetsAsync()
        {
            try
            {
                this._currentPresets = await this._dataService.GetPresetsWithDetailsAsync().ConfigureAwait(true);

                foreach (Models.Preset preset in this._currentPresets)
                {
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

        #endregion

        #region File Manager Logic (Optimized I/O)

        private void LoadFileManager()
        {
            if (!IO.Directory.Exists(_uploadDir)) IO.Directory.CreateDirectory(_uploadDir);

            Obj.ObservableCollection<FileSystemItem> items = new Obj.ObservableCollection<FileSystemItem>();
            BuildTree(_uploadDir, items);
            this.TreeFiles.ItemsSource = items;

            this.TxtCurrentPath.Text = "Storage/";
        }

        private static void BuildTree(Sys.String path, Obj.ObservableCollection<FileSystemItem> collection)
        {
            try
            {
                foreach (Sys.String dir in IO.Directory.EnumerateDirectories(path))
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

                foreach (Sys.String file in IO.Directory.EnumerateFiles(path))
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
            if (bytes == 0) return "0 B";
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

        private void BtnUploadFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            Dialogs.OpenFileDialog dlg = new Dialogs.OpenFileDialog
            {
                Multiselect = true,
                Title = "Pilih File untuk Diunggah"
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (Sys.String file in dlg.FileNames)
                {
                    Sys.String dest = IO.Path.Combine(_uploadDir, IO.Path.GetFileName(file));
                    try
                    {
                        IO.File.Copy(file, dest, true);
                    }
                    catch (Sys.Exception ex)
                    {
                        Sys.Console.WriteLine($"Upload Fail: {ex.Message}");
                    }
                }
                this.LoadFileManager();
                this.ShowToast("File berhasil disimpan");
            }
        }

        private void BtnUploadFolder_Click(Sys.Object sender, Win.RoutedEventArgs e)
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

                CopyDirectory(dialog.FolderName, destDir);

                this.LoadFileManager();
                this.ShowToast("Folder disalin ke storage");
            }
        }

        private static void CopyDirectory(Sys.String sourceDir, Sys.String destinationDir)
        {
            IO.DirectoryInfo dir = new IO.DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            IO.Directory.CreateDirectory(destinationDir);

            foreach (IO.FileInfo file in dir.GetFiles())
            {
                file.CopyTo(IO.Path.Combine(destinationDir, file.Name), true);
            }

            foreach (IO.DirectoryInfo subDir in dir.GetDirectories())
            {
                CopyDirectory(subDir.FullName, IO.Path.Combine(destinationDir, subDir.Name));
            }
        }

        private void BtnDeleteFile_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (sender is Controls.Button btn && btn.Tag is FileSystemItem item)
            {
                try
                {
                    if (item.IsFolder)
                    {
                        IO.Directory.Delete(item.FullPath, true);
                    }
                    else
                    {
                        IO.File.Delete(item.FullPath);
                    }

                    this.LoadFileManager();
                    this.ShowToast("Item berhasil dihapus");
                }
                catch (Sys.Exception ex)
                {
                    this.ShowToast("Gagal menghapus: " + ex.Message, true);
                }
            }
        }

        #endregion

        #region UI Event Handlers (Navigasi & Ekspor)

        private void ListPresets_SelectionChanged(Sys.Object sender, Controls.SelectionChangedEventArgs e)
        {
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
                this.ShowToast("Data berhasil disimpan");
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("SaveModal Error: ", ex));
                this.ShowToast(ex.Message, true);
            }
        }

        #endregion

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

        #region CRUD: Template

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

        #region Preview Processing (Text Replacement) & {{xxc}} Logic

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

            // --- FEATURE ADDITION: {{xxc}} Parsing ---
            // Mengganti {{xxc}} dengan struktur folder, list file, dan konten kode
            if (content.Contains("{{xxc}}"))
            {
                Sys.String xxcContent = this.GenerateXxcContent();
                content = content.Replace("{{xxc}}", xxcContent);
            }
            // -----------------------------------------

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

        // --- Helper Methods untuk {{xxc}} ---

        private Sys.String GenerateXxcContent()
        {
            if (!IO.Directory.Exists(_uploadDir)) return "Directory 'uploaded' not found.";

            Sys.Text.StringBuilder sb = new Sys.Text.StringBuilder();

            // 1. Generate Folder Tree
            sb.AppendLine("<folder_tree>");
            sb.AppendLine(this.GenerateTree(_uploadDir));
            sb.AppendLine("</folder_tree>\n");

            // 2. Generate File List
            sb.AppendLine("<file_list>");
            sb.AppendLine(this.ListUploadedFiles(_uploadDir));
            sb.AppendLine("</file_list>\n");

            // 3. Generate Code Content
            sb.AppendLine(this.GetCodeTemplate(_uploadDir));

            return sb.ToString();
        }

        private Sys.String GenerateTree(Sys.String dirPath, Sys.String prefix = "")
        {
            Sys.Text.StringBuilder treeBuilder = new Sys.Text.StringBuilder();
            if (!IO.Directory.Exists(dirPath))
            {
                return "Direktori tidak ditemukan.";
            }

            var directories = IO.Directory.GetDirectories(dirPath).OrderBy(d => d).ToList();
            var files = IO.Directory.GetFiles(dirPath).OrderBy(f => f).ToList();

            for (int i = 0; i < directories.Count; i++)
            {
                var d = directories[i];
                bool isLast = (i == directories.Count - 1) && (files.Count == 0);
                treeBuilder.Append($"{prefix}{(isLast ? "└──" : "├──")} {IO.Path.GetFileName(d)}/\n");
                string newPrefix = prefix + (isLast ? "    " : "│   ");
                treeBuilder.Append(GenerateTree(d, newPrefix));
            }

            for (int i = 0; i < files.Count; i++)
            {
                var f = files[i];
                bool isLast = (i == files.Count - 1);
                treeBuilder.Append($"{prefix}{(isLast ? "└──" : "├──")} {IO.Path.GetFileName(f)}\n");
            }

            return treeBuilder.ToString();
        }

        private Sys.String ListUploadedFiles(Sys.String rootDir)
        {
            if (!IO.Directory.Exists(rootDir)) return "";

            // Streaming (Enumerable) untuk hemat memori
            var filenames = IO.Directory.EnumerateFiles(rootDir, "*", IO.SearchOption.AllDirectories)
                                        .Select(IO.Path.GetFileName)
                                        .Where(name => name != null)
                                        .OrderBy(name => name);

            return Sys.String.Join(", ", filenames.Select(name => $"`{name}`"));
        }

        private Sys.String GetCodeTemplate(Sys.String rootDir)
        {
            Sys.Text.StringBuilder entries = new Sys.Text.StringBuilder();
            if (!IO.Directory.Exists(rootDir)) return "";

            // Menggunakan EnumerateFiles untuk menghindari loading array besar ke memori (4GB RAM Optimization)
            var allFiles = IO.Directory.EnumerateFiles(rootDir, "*", IO.SearchOption.AllDirectories).OrderBy(f => f);

            foreach (var fullPath in allFiles)
            {
                try
                {
                    IO.FileInfo fi = new IO.FileInfo(fullPath);
                    // Skip file jika terlalu besar (> 10MB) untuk mencegah OOM pada 4GB RAM
                    if (fi.Length > 10 * 1024 * 1024) continue;

                    Sys.String fname = IO.Path.GetFileName(fullPath);
                    Sys.String ext = IO.Path.GetExtension(fname).ToLower();
                    Sys.String lang = _langMap.GetValueOrDefault(ext, "text");

                    // Membaca text. Untuk file sangat besar, ReadAllText bisa berat, tapi dengan limit 10MB di atas masih aman.
                    Sys.String content = IO.File.ReadAllText(fullPath).TrimEnd();

                    entries.AppendLine($"<{fname}>\n");
                    entries.AppendLine($"```{lang}\n{content}\n```\n");
                    entries.AppendLine($"</{fname}>\n");
                }
                catch
                {
                    continue;
                }
            }
            return entries.ToString().TrimEnd();
        }

        #endregion
    }
}