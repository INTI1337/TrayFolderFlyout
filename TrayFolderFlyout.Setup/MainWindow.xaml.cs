using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TrayFolderFlyout.Setup
{
    public partial class MainWindow : Window
    {
        private readonly bool _isUpdate;

        public MainWindow()
        {
            InitializeComponent();

            _isUpdate = InstallerLogic.IsInstalled();
            VersionText.Text = InstallerLogic.AppVersion;

            if (_isUpdate)
            {
                Title = $"Обновление TrayFolderFlyout {InstallerLogic.AppVersion}";
                HeaderTitleText.Text = "Обновление TrayFolderFlyout";
                HeaderSubtitleText.Text = "Обновление до актуальной версии";
                InstallButton.Content = "Обновить";
            }
            else
            {
                Title = $"Установка TrayFolderFlyout {InstallerLogic.AppVersion}";
                HeaderTitleText.Text = "Установка TrayFolderFlyout";
                HeaderSubtitleText.Text = "Кастомные компактные папки на панели задач Windows 11";
                InstallButton.Content = "Установить";
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            OptionsPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            bool createDesktop = DesktopShortcutCheckBox.IsChecked == true;
            bool enableAutoRun = AutoRunCheckBox.IsChecked == true;

            var progress = new Progress<(int percent, string status)>(update =>
            {
                InstallProgressBar.Value = update.percent;
                StatusTextBlock.Text = update.status;
            });

            try
            {
                await InstallerLogic.InstallAsync(createDesktop, enableAutoRun, progress);

                // Success state
                CancelButton.Visibility = Visibility.Collapsed;
                InstallButton.Visibility = Visibility.Collapsed;
                LaunchButton.Visibility = Visibility.Visible;

                StatusTextBlock.Text = _isUpdate
                    ? "✅ TrayFolderFlyout успешно обновлён!"
                    : "✅ TrayFolderFlyout успешно установлен!";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                InstallProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                InstallButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(InstallerLogic.TargetExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = InstallerLogic.TargetExePath,
                        UseShellExecute = true
                    });
                }
            }
            catch { }

            Close();
        }
    }
}
