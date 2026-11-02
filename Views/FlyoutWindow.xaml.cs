using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using TrayFolderFlyout.Models;
using TrayFolderFlyout.Native;
using TrayFolderFlyout.Services;
using TrayFolderFlyout.ViewModels;

namespace TrayFolderFlyout.Views
{
    public partial class FlyoutWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string? CurrentGroupId { get; private set; }
        public long OpenedTimestamp { get; private set; }
        public long ClosedTimestamp { get; private set; }

        public int TargetColumns => Math.Clamp(_group?.Columns ?? 5, 3, 6);

        private AppConfig _config = null!;
        private FolderGroup _group = null!;
        private readonly ObservableCollection<FlyoutItemViewModel> _items = new();
        private readonly Stack<string> _navigationStack = new();
        private string? _currentSubfolderPath;

        private int _lastCursorX;
        private int _lastCursorY;

        private bool _isMenuOpen;
        private bool _isDragActive;
        private bool _isModalOpen;
        private bool _isClosingAnimation;

        public FlyoutWindow(string? initialGroupId = null)
        {
            InitializeComponent();
            ItemsHost.ItemsSource = _items;

            SourceInitialized += FlyoutWindow_SourceInitialized;
            SizeChanged += FlyoutWindow_SizeChanged;
            ContentRendered += FlyoutWindow_ContentRendered;
            Closing += (s, e) =>
            {
                e.Cancel = true;
                HideFlyout();
            };

            // Pre-create HWND and warm up OS window resources for instant 1ms popup
            new WindowInteropHelper(this).EnsureHandle();

            if (!string.IsNullOrEmpty(initialGroupId))
            {
                CurrentGroupId = initialGroupId;
            }
        }

        private void FlyoutWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Note: AllowsTransparency="True" handles smooth per-pixel alpha transparency,
            // 12px rounded corners, and soft DropShadow. DWM system backdrop is deliberately
            // NOT applied here because DWM would paint an opaque gray rectangle across the transparent margins.
        }

        public void ShowFlyout(string groupId, int cursorX = 0, int cursorY = 0)
        {
            CurrentGroupId = groupId;
            _navigationStack.Clear();
            _currentSubfolderPath = null;
            _isClosingAnimation = false;

            _config = ConfigService.Instance.LoadConfig();
            var group = _config.Groups.FirstOrDefault(g => g.Id == groupId) ?? _config.Groups.FirstOrDefault();

            if (group == null)
            {
                HideFlyout();
                return;
            }

            _group = group;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetColumns)));
            UpdateHeader(_group.Name, _group.CustomIconPath);
            LoadCurrentView();

            // Guarantee size is determined by content
            SizeToContent = SizeToContent.WidthAndHeight;

            // Initially set Opacity = 0 to prevent any visible jump/flicker during measurement
            Opacity = 0.0;

            // 1. Initial measurement and positioning using DesiredSize
            PositionWindow(cursorX, cursorY);

            // 2. Show window and perform layout pass
            Show();
            UpdateLayout();

            // 3. Re-calculate PositionWindow with accurate rendered dimensions
            PositionWindow(cursorX, cursorY);

            // Bring to foreground
            var hwnd = new WindowInteropHelper(this).Handle;
            Win32.SetForegroundWindow(hwnd);
            try { Activate(); } catch { }
            try { Focus(); } catch { }

            OpenedTimestamp = Environment.TickCount64;

            // 4. Smoothly fade in and slide up to Opacity = 1
            PlayEnterAnimation();
        }

        public void HideFlyout()
        {
            if (_isClosingAnimation || Visibility != Visibility.Visible) return;

            ClosedTimestamp = Environment.TickCount64;
            _isClosingAnimation = true;
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(90))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var slideDown = new DoubleAnimation(0.0, 8.0, TimeSpan.FromMilliseconds(90))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            slideDown.Completed += (s, e) =>
            {
                Hide();
                _isClosingAnimation = false;
            };

            BeginAnimation(OpacityProperty, fadeOut);
            FlyoutTransform.BeginAnimation(TranslateTransform.YProperty, slideDown);
        }

        private void UpdateHeader(string title, string? iconPath)
        {
            HeaderTitleText.Text = title ?? "";
            HeaderTitleText.Visibility = string.IsNullOrWhiteSpace(title) ? Visibility.Collapsed : Visibility.Visible;
            HeaderGroupIcon.Source = IconService.GetIcon(null, iconPath, 32);
            BackButton.Visibility = _navigationStack.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ReloadCurrentViewFromConfig()
        {
            if (_group == null) return;
            _config = ConfigService.Instance.LoadConfig();
            var updated = _config.Groups.FirstOrDefault(g => g.Id == _group.Id);
            if (updated != null)
            {
                _group = updated;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetColumns)));
                UpdateHeader(_group.Name, _group.CustomIconPath);
                LoadCurrentView();
            }
        }

        private void FlyoutWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible && !_isClosingAnimation && (e.WidthChanged || e.HeightChanged))
            {
                PositionWindow(_lastCursorX, _lastCursorY);
            }
        }

        private void FlyoutWindow_ContentRendered(object? sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible && !_isClosingAnimation)
            {
                PositionWindow(_lastCursorX, _lastCursorY);
            }
        }

        private void LoadCurrentView()
        {
            _items.Clear();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetColumns)));
            var panel = FindVisualChild<System.Windows.Controls.Primitives.UniformGrid>(ItemsHost);
            if (panel != null)
            {
                panel.Columns = TargetColumns;
            }

            bool showLabels = _group?.ShowLabels ?? true;

            if (!string.IsNullOrEmpty(_currentSubfolderPath) && Directory.Exists(_currentSubfolderPath))
            {
                LoadDirectoryContents(_currentSubfolderPath, showLabels);
            }
            else if (_group != null && _group.Type == FolderType.Synchronized && !string.IsNullOrEmpty(_group.SyncPath) && Directory.Exists(_group.SyncPath))
            {
                LoadDirectoryContents(_group.SyncPath, showLabels);
            }
            else if (_group != null)
            {
                foreach (var item in _group.Items)
                {
                    _items.Add(new FlyoutItemViewModel(item, showLabels));
                }
            }

            EmptyStateText.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadDirectoryContents(string dirPath, bool showLabels)
        {
            try
            {
                var dirs = Directory.GetDirectories(dirPath)
                    .Select(d => new DirectoryInfo(d))
                    .Where(d => (d.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    .OrderBy(d => d.Name);

                foreach (var d in dirs)
                {
                    _items.Add(new FlyoutItemViewModel(new FolderItem
                    {
                        Name = d.Name,
                        TargetPath = d.FullName
                    }, showLabels));
                }

                var files = Directory.GetFiles(dirPath)
                    .Select(f => new FileInfo(f))
                    .Where(f => (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    .OrderBy(f => f.Name);

                foreach (var f in files)
                {
                    string displayName = f.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ||
                                         f.Extension.Equals(".url", StringComparison.OrdinalIgnoreCase)
                        ? Path.GetFileNameWithoutExtension(f.Name)
                        : f.Name;

                    _items.Add(new FlyoutItemViewModel(new FolderItem
                    {
                        Name = displayName,
                        TargetPath = f.FullName
                    }, showLabels));
                }
            }
            catch { }
        }

        private void PositionWindow(int cursorX, int cursorY)
        {
            _lastCursorX = cursorX;
            _lastCursorY = cursorY;

            var pt = new POINT(cursorX, cursorY);
            if (cursorX == 0 && cursorY == 0)
            {
                Win32.GetCursorPos(out pt);
            }

            Win32.GetMonitorAndWorkArea(pt, out RECT rcMonitor, out RECT rcWork);

            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiScaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiScaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            double workWidth = (rcWork.Right - rcWork.Left);
            double workHeight = (rcWork.Bottom - rcWork.Top);

            double workLeftDip = rcWork.Left / dpiScaleX;
            double workTopDip = rcWork.Top / dpiScaleY;
            double workRightDip = rcWork.Right / dpiScaleX;
            double workBottomDip = rcWork.Bottom / dpiScaleY;

            double cursorXDip = pt.X / dpiScaleX;
            double cursorYDip = pt.Y / dpiScaleY;

            // Force visual tree template application and content measurement
            ItemsHost.ApplyTemplate();
            this.Measure(new Size(workWidth / dpiScaleX, workHeight / dpiScaleY));

            double flyoutWidth = this.DesiredSize.Width > 50 ? this.DesiredSize.Width : (ActualWidth > 50 ? ActualWidth : 420);
            double flyoutHeight = this.DesiredSize.Height > 50 ? this.DesiredSize.Height : (ActualHeight > 50 ? ActualHeight : 350);

            double left, top;

            // Determine taskbar position by comparing work area with full monitor rectangle:
            // 1. Taskbar at TOP: work area starts below monitor top
            // 2. Taskbar at LEFT: work area starts right of monitor left
            // 3. Taskbar at RIGHT: work area ends left of monitor right
            // 4. Taskbar at BOTTOM: work area ends above monitor bottom (standard)
            if (rcWork.Top > rcMonitor.Top)
            {
                // Top taskbar: position flyout 12px below top of work area, centered horizontally on cursor
                top = workTopDip + 12.0;
                left = cursorXDip - (flyoutWidth / 2.0);
            }
            else if (rcWork.Left > rcMonitor.Left)
            {
                // Left taskbar: position flyout 12px right of left of work area, centered vertically on cursor
                left = workLeftDip + 12.0;
                top = cursorYDip - (flyoutHeight / 2.0);
            }
            else if (rcWork.Right < rcMonitor.Right)
            {
                // Right taskbar: position flyout 12px left of right of work area, centered vertically on cursor
                left = workRightDip - flyoutWidth - 12.0;
                top = cursorYDip - (flyoutHeight / 2.0);
            }
            else
            {
                // Bottom taskbar (default): position flyout 12px above bottom of work area, centered horizontally on cursor
                top = workBottomDip - flyoutHeight - 12.0;
                left = cursorXDip - (flyoutWidth / 2.0);
            }

            // Strictly clamp within work area bounds with 12px padding on all four sides
            left = Math.Max(workLeftDip + 12.0, Math.Min(left, workRightDip - flyoutWidth - 12.0));
            top = Math.Max(workTopDip + 12.0, Math.Min(top, workBottomDip - flyoutHeight - 12.0));

            Left = left;
            Top = top;
        }

        private void PlayEnterAnimation()
        {
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation(10.0, 0.0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            fadeIn.Completed += (s, e) => Opacity = 1.0;

            Opacity = 0.0;
            BeginAnimation(OpacityProperty, fadeIn);
            FlyoutTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: FlyoutItemViewModel vm })
            {
                ExecuteItem(vm, runAsAdmin: false);
            }
        }

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Middle click (wheel) opens folder in Explorer
            if (e.ChangedButton == MouseButton.Middle &&
                sender is Button { DataContext: FlyoutItemViewModel vm } &&
                vm.IsDirectory)
            {
                OpenInExplorer(vm.TargetPath);
                HideFlyout();
            }
        }

        private void Item_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void ExecuteItem(FlyoutItemViewModel vm, bool runAsAdmin)
        {
            if (vm.IsDirectory)
            {
                _navigationStack.Push(_currentSubfolderPath ?? "");
                _currentSubfolderPath = vm.TargetPath;
                UpdateHeader(vm.Name, null);
                LoadCurrentView();
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = vm.TargetPath,
                    Arguments = vm.SourceItem.Arguments ?? "",
                    UseShellExecute = true
                };

                if (runAsAdmin || vm.SourceItem.RunAsAdmin)
                {
                    psi.Verb = "runas";
                }

                Process.Start(psi);
                HideFlyout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить элемент: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationStack.Count > 0)
            {
                string previous = _navigationStack.Pop();
                _currentSubfolderPath = string.IsNullOrEmpty(previous) ? null : previous;

                if (string.IsNullOrEmpty(_currentSubfolderPath))
                {
                    UpdateHeader(_group.Name, _group.CustomIconPath);
                }
                else
                {
                    UpdateHeader(Path.GetFileName(_currentSubfolderPath), null);
                }

                LoadCurrentView();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideFlyout();
        }

        // Context menu item actions
        private void MenuItemRun_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                ExecuteItem(vm, runAsAdmin: false);
            }
        }

        private void MenuItemRunAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                ExecuteItem(vm, runAsAdmin: true);
            }
        }

        private void MenuItemOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                OpenLocation(vm.TargetPath);
                HideFlyout();
            }
        }

        private void MenuItemRename_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                _isModalOpen = true;
                try
                {
                    var dlg = new InputDialog("Переименовать", "Введите новое название для ярлыка:", vm.Name);
                    if (dlg.ShowDialog() == true && dlg.ResponseText != null)
                    {
                        vm.Name = dlg.ResponseText;
                        vm.SourceItem.Name = dlg.ResponseText;
                        ConfigService.Instance.SaveConfig(_config);
                        App.RefreshAllUI();
                    }
                }
                finally
                {
                    _isModalOpen = false;
                }
            }
        }

        private void MenuItemChangeIcon_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                _isModalOpen = true;
                try
                {
                    var ofd = new OpenFileDialog
                    {
                        Title = "Выберите новую иконку для элемента",
                        Filter = "Иконки и изображения (*.ico;*.png;*.exe;*.jpg;*.bmp)|*.ico;*.png;*.exe;*.jpg;*.jpeg;*.bmp|Все файлы (*.*)|*.*"
                    };

                    if (ofd.ShowDialog() == true)
                    {
                        string newIco = IconService.ConvertAndAssignCustomIcon(ofd.FileName, vm.SourceItem.Id, vm.SourceItem.CustomIconPath);
                        vm.SourceItem.CustomIconPath = newIco;
                        ConfigService.Instance.SaveConfig(_config);
                        App.RefreshAllUI();
                    }
                }
                finally
                {
                    _isModalOpen = false;
                }
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (GetContextItem(sender) is FlyoutItemViewModel vm)
            {
                _group.Items.Remove(vm.SourceItem);
                _items.Remove(vm);
                if (vm.SourceItem.TargetPath.StartsWith(ConfigService.Instance.SavedLinksDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    ConfigService.TryDeleteOldFile(vm.SourceItem.TargetPath);
                }
                ConfigService.TryDeleteOldFile(vm.SourceItem.CustomIconPath);
                ConfigService.Instance.SaveConfig(_config);
                EmptyStateText.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                App.RefreshAllUI();
            }
        }

        // Background context menu
        private void MenuAddFile_Click(object sender, RoutedEventArgs e)
        {
            _isModalOpen = true;
            try
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Выберите файл или приложение для добавления",
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    AddItemToGroup(ofd.FileName);
                }
            }
            finally
            {
                _isModalOpen = false;
            }
        }

        private void MenuAddFolder_Click(object sender, RoutedEventArgs e)
        {
            _isModalOpen = true;
            try
            {
                var ofd = new OpenFolderDialog
                {
                    Title = "Выберите папку для добавления"
                };

                if (ofd.ShowDialog() == true && !string.IsNullOrEmpty(ofd.FolderName))
                {
                    AddItemToGroup(ofd.FolderName);
                }
            }
            finally
            {
                _isModalOpen = false;
            }
        }

        private void MenuAddUrl_Click(object sender, RoutedEventArgs e)
        {
            _isModalOpen = true;
            try
            {
                var dlg = new InputDialog("Добавить ссылку", "Введите URL адрес (например: https://github.com):", "https://");
                if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResponseText))
                {
                    var item = ConfigService.Instance.CreateFolderItem(dlg.ResponseText);
                    _group.Items.Add(item);
                    ConfigService.Instance.SaveConfig(_config);
                    LoadCurrentView();
                    App.RefreshAllUI();
                }
            }
            finally
            {
                _isModalOpen = false;
            }
        }

        private void MenuChangeIcon_Click(object sender, RoutedEventArgs e)
        {
            _isModalOpen = true;
            try
            {
                var picker = new IconPickerWindow(_group.Id);
                if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedIconPath))
                {
                    string? oldIcon = _group.CustomIconPath;
                    _group.CustomIconPath = picker.SelectedIconPath;
                    ConfigService.TryDeleteOldFile(oldIcon);
                    ConfigService.Instance.SaveConfig(_config);
                    ConfigService.Instance.CreateTaskbarShortcut(_group, _config.Groups);
                    App.RefreshAllUI();
                }
            }
            finally
            {
                _isModalOpen = false;
            }
        }

        private void MenuConfigure_Click(object sender, RoutedEventArgs e)
        {
            HideFlyout();
            App.ShowManagerWindow(_group.Id);
        }

        private void AddItemToGroup(string path)
        {
            var item = ConfigService.Instance.CreateFolderItem(path);
            _group.Items.Add(item);
            ConfigService.Instance.SaveConfig(_config);
            LoadCurrentView();
            App.RefreshAllUI();
        }

        // Drag & Drop
        private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                _isDragActive = true;
                DropOverlay.Visibility = Visibility.Visible;
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                _isDragActive = true;
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            _isDragActive = false;
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _isDragActive = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        AddItemToGroup(file);
                    }
                }
            }
        }

        // Auto-close handling on click outside
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (Environment.TickCount64 - OpenedTimestamp < 200) return;

            if (!_isMenuOpen && !_isDragActive && !_isModalOpen && !_isClosingAnimation)
            {
                HideFlyout();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_navigationStack.Count > 0)
                {
                    BackButton_Click(this, new RoutedEventArgs());
                }
                else
                {
                    HideFlyout();
                }
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => _isMenuOpen = true;
        private void ContextMenu_Closed(object sender, RoutedEventArgs e) => _isMenuOpen = false;
        private void MainBorder_ContextMenuOpening(object sender, ContextMenuEventArgs e) => _isMenuOpen = true;

        private static FlyoutItemViewModel? GetContextItem(object sender)
        {
            if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Button btn)
            {
                return btn.DataContext as FlyoutItemViewModel;
            }
            return null;
        }

        private static void OpenInExplorer(string folderPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{folderPath}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static void OpenLocation(string targetPath)
        {
            try
            {
                string pathToSelect = targetPath;
                if (targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
                {
                    var details = IconService.RunInSta(() => ShellShortcutHelper.ResolveShortcut(targetPath));
                    if (details != null && !string.IsNullOrEmpty(details.TargetPath) &&
                        (File.Exists(details.TargetPath) || Directory.Exists(details.TargetPath)))
                    {
                        pathToSelect = details.TargetPath;
                    }
                }

                if (File.Exists(pathToSelect) || Directory.Exists(pathToSelect))
                {
                    Process.Start("explorer.exe", $"/select,\"{pathToSelect}\"");
                }
            }
            catch { }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }
    }
}
