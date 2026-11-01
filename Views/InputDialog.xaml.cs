using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TrayFolderFlyout.Native;
using TrayFolderFlyout.Services;

namespace TrayFolderFlyout.Views
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; } = "";

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            TitleText.Text = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.SelectAll();

            Loaded += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                bool isDark = ThemeService.IsDarkTheme();
                Win32.ApplyWin11Style(hwnd, isDark, useAcrylic: true);

                if (!isDark)
                {
                    ApplyLightStyle();
                }

                InputTextBox.Focus();
            };
        }

        private void ApplyLightStyle()
        {
            DialogBorder.Background = new SolidColorBrush(Color.FromArgb(240, 248, 248, 248));
            DialogBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            PromptText.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            InputTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            InputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            InputTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
            CancelButton.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
            CancelButton.Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            CancelButton.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }
    }
}
