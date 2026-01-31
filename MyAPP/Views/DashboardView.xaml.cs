using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MyAPP.Views
{
    public partial class DashboardView : UserControl
    {
        public class PresetItem
        {
            public string Name { get; set; } = string.Empty;
        }

        public class VariableItem
        {
            public string Key { get; set; } = string.Empty;
            public string DefaultValue { get; set; } = string.Empty;
            public string Template { get; set; } = "SIMPLE_1";
        }

        private enum ModalMode { None, NewPreset, EditPreset, NewVariable, EditVariable, NewTemplate, EditTemplate }

        private ModalMode _currentMode = ModalMode.None;
        private object? _currentItem = null;

        public DashboardView()
        {
            InitializeComponent();
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            ListPresets.ItemsSource = new List<PresetItem>
            {
                new() { Name = "RANGKUM" },
                new() { Name = "C (NOT FIX)_1" },
                new() { Name = "NOTEPAD" },
                new() { Name = "C (NOT FIX)_2" },
                new() { Name = "MODERN C" },
                new() { Name = "GITHUB" }
            };

            ItemsVariables.ItemsSource = new List<VariableItem>
            {
                new() { Key = "{{theme}}", DefaultValue = "Isi nilai variabel...", Template = "SIMPLE_1" },
                new() { Key = "{{transcripttitle}}", DefaultValue = "Isi nilai variabel...", Template = "SIMPLE_2" },
                new() { Key = "{{transcript}}", DefaultValue = "Isi nilai variabel...", Template = "DEPTH" }
            };

            InputVarTemplate.ItemsSource = new List<string> { "SIMPLE_1", "SIMPLE_2", "DEPTH" };
        }

        private void ListPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListPresets.SelectedItem is PresetItem item)
            {
                TxtActivePresetName.Text = item.Name;
            }
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e)
        {
            Window? window = Window.GetWindow(this);
            if (window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowModeSelection();
            }
        }

        private void OpenModal(string title, string icon = "📝")
        {
            ModalTitle.Text = title;
            ModalIcon.Text = icon;
            ModalOverlay.Visibility = Visibility.Visible;

            FormPreset.Visibility = Visibility.Collapsed;
            FormVariable.Visibility = Visibility.Collapsed;
            FormTemplate.Visibility = Visibility.Collapsed;
        }

        private void BtnCloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            _currentMode = ModalMode.None;
            _currentItem = null;
        }

        private void BtnSaveModal_Click(object sender, RoutedEventArgs e)
        {

            if (_currentMode == ModalMode.EditTemplate || _currentMode == ModalMode.NewTemplate)
            {
                TxtPreviewContent.Text = InputTemplateContent.Text;
            }
            else if (_currentMode == ModalMode.EditPreset && _currentItem is PresetItem pItem)
            {
                pItem.Name = InputPresetName.Text;
                ListPresets.Items.Refresh();
            }
            else if (_currentMode == ModalMode.EditVariable && _currentItem is VariableItem vItem)
            {
                vItem.Key = InputVarKey.Text;
                vItem.DefaultValue = InputVarValue.Text;
                vItem.Template = InputVarTemplate.SelectedItem?.ToString() ?? "SIMPLE_1";
                ItemsVariables.Items.Refresh();
            }

            MessageBox.Show("Data berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            BtnCloseModal_Click(sender, e);
        }

        private void BtnNewPreset_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ModalMode.NewPreset;
            OpenModal("Buat Preset Baru");
            FormPreset.Visibility = Visibility.Visible;
            InputPresetName.Text = "";
        }

        private void BtnEditPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PresetItem item)
            {
                _currentMode = ModalMode.EditPreset;
                _currentItem = item;
                OpenModal("Edit Preset");
                FormPreset.Visibility = Visibility.Visible;
                InputPresetName.Text = item.Name;
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Apakah Anda yakin ingin menghapus preset ini?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }

        private void BtnAddVariable_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ModalMode.NewVariable;
            OpenModal("Kelola Variabel", "🏷️");
            FormVariable.Visibility = Visibility.Visible;

            InputVarTemplate.SelectedIndex = 0;
            InputVarKey.Text = "";
            InputVarValue.Text = "";
        }

        private void BtnEditVariable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is VariableItem item)
            {
                _currentMode = ModalMode.EditVariable;
                _currentItem = item;
                OpenModal("Kelola Variabel", "🏷️");
                FormVariable.Visibility = Visibility.Visible;

                InputVarTemplate.SelectedItem = item.Template;
                InputVarKey.Text = item.Key;
                InputVarValue.Text = item.DefaultValue;
            }
        }

        private void BtnDeleteVariable_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hapus variabel?", "Konfirmasi");
        }

        private void BtnAddTemplate_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ModalMode.NewTemplate;
            OpenModal("Kelola Templat", "📝");
            FormTemplate.Visibility = Visibility.Visible;

            InputTemplateTitle.Text = "";
            InputTemplateContent.Text = "";
        }

        private void BtnEditPreviewContent_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ModalMode.EditTemplate;
            OpenModal("Kelola Templat", "📝");
            FormTemplate.Visibility = Visibility.Visible;

            InputTemplateTitle.Text = "SIMPLE_1";
            InputTemplateContent.Text = TxtPreviewContent.Text;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MyAPP.Services.AuthState.ClearToken();

            Window? window = Window.GetWindow(this);
            if (window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowLogin();
            }
        }
    }
}