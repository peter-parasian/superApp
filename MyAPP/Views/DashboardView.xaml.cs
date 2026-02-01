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
    public partial class DashboardView : Controls.UserControl
    {
        private enum ModalMode { None, NewPreset, EditPreset, NewVariable, EditVariable, NewTemplate, EditTemplate }

        private readonly Services.SupabaseDataService _dataService;
        private ModalMode _currentMode = ModalMode.None;
        private Models.Preset? _selectedPreset = null;
        private Models.Template? _selectedTemplate = null;
        private Models.Variable? _editingVariable = null;

        private Coll.List<Models.Preset> _currentPresets = new Coll.List<Models.Preset>();

        private static readonly long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

        private const Sys.Int32 FILE_BUFFER_SIZE = 131072;

        private readonly Sys.String _uploadDir = IO.Path.Combine(
            Sys.Environment.GetFolderPath(Sys.Environment.SpecialFolder.LocalApplicationData),
            "MyAPP",
            "uploaded"
        );

        private Sys.Boolean _isSuppressingSelectionChange = false;

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
    }
}