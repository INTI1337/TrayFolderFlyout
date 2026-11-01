using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using TrayFolderFlyout.Native;
using TrayFolderFlyout.Services;

namespace TrayFolderFlyout.Views
{
    public partial class IconPickerWindow : Window
    {
        public string? SelectedIconPath { get; private set; }
        private readonly string _groupId;

        public IconPickerWindow(string groupId)
        {
            InitializeComponent();
            _groupId = groupId;

            PresetsItemsControl.ItemsSource = PresetIconsService.GetPresets();

            SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                Win32.ApplyWin11Style(hwnd, isDark: true, useAcrylic: true);
            };
        }

        private void PresetItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: IconPresetItem preset })
            {
                string newIco = ConfigService.Instance.GenerateUniqueIconPath(_groupId);
                File.Copy(preset.IconPath, newIco, true);
                SelectedIconPath = newIco;
                DialogResult = true;
                Close();
            }
        }

        private void UploadCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Выберите файл для генерации иконки папки",
                Filter = "Иконки и изображения (*.ico;*.png;*.jpg;*.exe;*.bmp)|*.ico;*.png;*.jpg;*.jpeg;*.exe;*.bmp|Все файлы (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string targetIco = ConfigService.Instance.GenerateUniqueIconPath(_groupId);
                IcoConverter.CreateMultiResIcon(ofd.FileName, targetIco);
                SelectedIconPath = targetIco;
                DialogResult = true;
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
