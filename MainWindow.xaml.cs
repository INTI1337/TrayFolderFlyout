using System;
using System.Collections.ObjectModel;
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
using TrayFolderFlyout.Views;

namespace TrayFolderFlyout
{
    public class SidebarGroupViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public FolderGroup Group { get; }
        public string Name => string.IsNullOrWhiteSpace(Group.Name) ? "(Без названия)" : Group.Name;
        public ImageSource GroupIcon => IconService.GetIcon(null, Group.CustomIconPath, 34);

        public string Subtitle => Group.Type == FolderType.Virtual
            ? $"{Group.Items.Count} эл. • {Group.Columns} в ряд"
            : $"Папка на диске • {Group.Columns} в ряд";

        public SidebarGroupViewModel(FolderGroup group)
        {
            Group = group;
        }

        public void NotifyChanges()
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(GroupIcon)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Subtitle)));
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class MainWindow : Window
    {
        private AppConfig _config = null!;
        private FolderGroup? _selectedGroup;
        private readonly ObservableCollection<SidebarGroupViewModel> _sidebarGroups = new();
        private readonly string? _initialSelectGroupId;
        private bool _isUpdatingUi;
        private readonly System.Windows.Threading.DispatcherTimer _saveDebounceTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };

        // Drag & drop reorder state
        private Border? _draggedCard;
        private FolderItem? _draggedItem;
        private Point _dragStartPoint;

        public MainWindow(string? selectGroupId = null)
        {
            InitializeComponent();
            _initialSelectGroupId = selectGroupId;

            GroupsListBox.ItemsSource = _sidebarGroups;

            _saveDebounceTimer.Tick += (s, e) => DebounceSaveAndSync();

            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            Win32.ApplyWin11Style(hwnd, isDark: true, useAcrylic: false);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AutoRunCheckBox.IsChecked = AutoRunService.IsAutoRunEnabled();
            RefreshGroupsList();

            if (!string.IsNullOrEmpty(_initialSelectGroupId))
            {
                var target = _sidebarGroups.FirstOrDefault(g => g.Group.Id == _initialSelectGroupId);
                if (target != null)
                {
                    GroupsListBox.SelectedItem = target;
                    return;
                }
            }

            if (GroupsListBox.Items.Count > 0)
            {
                GroupsListBox.SelectedIndex = 0;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Instead of shutting down the daemon host, hide to tray!
            e.Cancel = true;
            Hide();
        }

        public void RefreshGroupsList()
        {
            string? currentSelectedId = _selectedGroup?.Id;
            _config = ConfigService.Instance.LoadConfig();
            _sidebarGroups.Clear();

            foreach (var g in _config.Groups)
            {
                _sidebarGroups.Add(new SidebarGroupViewModel(g));
            }

            if (!string.IsNullOrEmpty(currentSelectedId))
            {
                var target = _sidebarGroups.FirstOrDefault(vm => vm.Group.Id == currentSelectedId);
                if (target != null)
                {
                    GroupsListBox.SelectedItem = target;
                }
            }
        }

        private void GroupsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupsListBox.SelectedItem is SidebarGroupViewModel vm)
            {
                _selectedGroup = vm.Group;
                LoadSelectedGroupDetails();
            }
        }

        private void LoadSelectedGroupDetails()
        {
            if (_selectedGroup == null) return;

            _isUpdatingUi = true;
            try
            {
                GroupNameTextBox.Text = _selectedGroup.Name ?? "";
                GroupNamePlaceholder.Visibility = string.IsNullOrEmpty(GroupNameTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                GroupIconPreview.Source = IconService.GetIcon(null, _selectedGroup.CustomIconPath, 64);

                // Type Radio
                if (_selectedGroup.Type == FolderType.Virtual)
                {
                    TypeVirtualRadio.IsChecked = true;
                    SyncPathCard.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TypeSyncRadio.IsChecked = true;
                    SyncPathCard.Visibility = Visibility.Visible;
                }
                SyncPathTextBox.Text = _selectedGroup.SyncPath ?? "";

                // Columns Radio
                Col3Radio.IsChecked = _selectedGroup.Columns == 3;
                Col4Radio.IsChecked = _selectedGroup.Columns == 4;
                Col5Radio.IsChecked = _selectedGroup.Columns == 5;

                // View Mode Radio
                ViewLabelsRadio.IsChecked = _selectedGroup.ShowLabels;
                ViewIconsOnlyRadio.IsChecked = !_selectedGroup.ShowLabels;

                RefreshLiveCanvas();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        public void RefreshLiveCanvas()
        {
            if (_selectedGroup == null) return;

            // Remove all item cards except the dashed DropZoneCard
            var cardsToRemove = LiveItemsWrapPanel.Children.OfType<Border>()
                .Where(b => b != DropZoneCard)
                .ToList();

            foreach (var card in cardsToRemove)
            {
                LiveItemsWrapPanel.Children.Remove(card);
            }

            int index = 0;
            foreach (var item in _selectedGroup.Items)
            {
                var card = CreateLiveItemCard(item);
                LiveItemsWrapPanel.Children.Insert(index, card);
                index++;
            }
        }

        private Border CreateLiveItemCard(FolderItem item)
        {
            bool showLabels = _selectedGroup?.ShowLabels ?? true;
            double cardWidth = showLabels ? 82 : 68;
            double cardHeight = showLabels ? 84 : 68;
            double iconSize = showLabels ? 38 : 44;

            var card = new Border
            {
                Width = cardWidth,
                Height = cardHeight,
                CornerRadius = new CornerRadius(10),
                Background = (Brush)FindResource("CardBgBrush"),
                BorderBrush = (Brush)FindResource("GlassBorderBrush"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4),
                Padding = new Thickness(2),
                Cursor = Cursors.Hand,
                DataContext = item,
                ToolTip = item.TargetPath
            };

            var grid = new Grid();

            // Content: Icon + Title
            var contentStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var img = new Image
            {
                Source = IconService.GetIcon(item.TargetPath, item.CustomIconPath, (int)iconSize),
                Width = iconSize,
                Height = iconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, showLabels ? 3 : 0)
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            contentStack.Children.Add(img);

            if (showLabels)
            {
                var tb = new TextBlock
                {
                    Text = item.Name,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("TextPrimaryBrush"),
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = cardWidth - 8,
                    MaxHeight = 28,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                contentStack.Children.Add(tb);
            }

            grid.Children.Add(contentStack);

            // Delete button (✕) in top-right corner
            var delBtn = new Button
            {
                Content = "✕",
                FontSize = 9,
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 1, 0),
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromArgb(160, 39, 39, 42)),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                BorderThickness = new Thickness(0),
                Opacity = 0.0,
                ToolTip = "Удалить ярлык"
            };

            delBtn.Resources.Add(typeof(Border), new Style(typeof(Border))
            {
                Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(9)) }
            });

            delBtn.Click += (s, e) =>
            {
                e.Handled = true;
                if (_selectedGroup != null)
                {
                    _selectedGroup.Items.Remove(item);
                    if (item.TargetPath.StartsWith(ConfigService.Instance.SavedLinksDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        ConfigService.TryDeleteOldFile(item.TargetPath);
                    }
                    ConfigService.TryDeleteOldFile(item.CustomIconPath);
                    SaveConfig();
                    App.RefreshAllUI();
                }
            };

            grid.Children.Add(delBtn);
            card.Child = grid;

            // Context Menu on Card (ПКМ)
            var cardContextMenu = new ContextMenu();

            var changeIconItem = new MenuItem { Header = "🎨 Сменить иконку..." };
            changeIconItem.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Выберите новую иконку для элемента",
                    Filter = "Иконки и изображения (*.ico;*.png;*.exe;*.jpg;*.bmp)|*.ico;*.png;*.exe;*.jpg;*.jpeg;*.bmp|Все файлы (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    string newIco = IconService.ConvertAndAssignCustomIcon(ofd.FileName, item.Id, item.CustomIconPath);
                    item.CustomIconPath = newIco;
                    SaveConfig();
                    App.RefreshAllUI();
                }
            };
            cardContextMenu.Items.Add(changeIconItem);

            var renameItem = new MenuItem { Header = "✏️ Переименовать..." };
            renameItem.Click += (s, e) =>
            {
                var dlg = new InputDialog("Переименовать", "Введите новое название для ярлыка:", item.Name);
                if (dlg.ShowDialog() == true && dlg.ResponseText != null)
                {
                    item.Name = dlg.ResponseText;
                    SaveConfig();
                    App.RefreshAllUI();
                }
            };
            cardContextMenu.Items.Add(renameItem);

            cardContextMenu.Items.Add(new Separator());

            var deleteItem = new MenuItem { Header = "🗑️ Удалить из этой папки" };
            deleteItem.Click += (s, e) =>
            {
                if (_selectedGroup != null)
                {
                    _selectedGroup.Items.Remove(item);
                    if (item.TargetPath.StartsWith(ConfigService.Instance.SavedLinksDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        ConfigService.TryDeleteOldFile(item.TargetPath);
                    }
                    ConfigService.TryDeleteOldFile(item.CustomIconPath);
                    SaveConfig();
                    App.RefreshAllUI();
                }
            };
            cardContextMenu.Items.Add(deleteItem);

            card.ContextMenu = cardContextMenu;

            // Hover animation & delete button visibility
            card.MouseEnter += (s, e) =>
            {
                card.Background = (Brush)FindResource("CardBgHoverBrush");
                card.BorderBrush = (Brush)FindResource("GlassBorderStrongBrush");
                delBtn.Opacity = 1.0;
            };

            card.MouseLeave += (s, e) =>
            {
                card.Background = (Brush)FindResource("CardBgBrush");
                card.BorderBrush = (Brush)FindResource("GlassBorderBrush");
                delBtn.Opacity = 0.0;
            };

            // Click to run or test
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = item.TargetPath,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            };

            // Drag & Drop reorder (only when moved > 5px, ignoring clicks on delete button)
            card.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is DependencyObject dep && FindVisualAncestor<Button>(dep) != null)
                {
                    // Clicked on delete button, do not capture or initiate drag!
                    return;
                }
                _dragStartPoint = e.GetPosition(this);
                _draggedCard = card;
                _draggedItem = item;
            };

            card.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedCard == card && _draggedItem != null)
                {
                    Point currentPoint = e.GetPosition(this);
                    Vector diff = _dragStartPoint - currentPoint;
                    if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
                    {
                        DragDrop.DoDragDrop(card, new DataObject("FolderItem", _draggedItem), DragDropEffects.Move);
                        _draggedCard = null;
                        _draggedItem = null;
                    }
                }
            };

            card.PreviewMouseLeftButtonUp += (s, e) =>
            {
                _draggedCard = null;
                _draggedItem = null;
            };

            card.AllowDrop = true;
            card.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent("FolderItem") && _selectedGroup != null)
                {
                    var droppedItem = e.Data.GetData("FolderItem") as FolderItem;
                    if (droppedItem != null && droppedItem != item)
                    {
                        int oldIdx = _selectedGroup.Items.IndexOf(droppedItem);
                        int newIdx = _selectedGroup.Items.IndexOf(item);
                        if (oldIdx >= 0 && newIdx >= 0)
                        {
                            _selectedGroup.Items.RemoveAt(oldIdx);
                            _selectedGroup.Items.Insert(newIdx, droppedItem);
                            SaveConfig();
                            RefreshLiveCanvas();
                        }
                    }
                }
            };

            return card;
        }

        private void SaveConfig()
        {
            if (!_isUpdatingUi && _config != null)
            {
                ConfigService.Instance.SaveConfig(_config);
            }
        }

        private void UpdateSidebarSubtitle()
        {
            int idx = GroupsListBox.SelectedIndex;
            if (idx >= 0 && idx < _sidebarGroups.Count && _selectedGroup != null)
            {
                _sidebarGroups[idx] = new SidebarGroupViewModel(_selectedGroup);
                GroupsListBox.SelectedIndex = idx;
            }
        }

        // Actions
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new FolderGroup
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "",
                Type = FolderType.Virtual,
                Columns = 4,
                ShowLabels = true,
                Items = new()
            };

            // Use preset folder icon (copied to unique file)
            var presets = PresetIconsService.GetPresets();
            var preset = presets.FirstOrDefault();
            if (preset != null && File.Exists(preset.IconPath))
            {
                string newIco = ConfigService.Instance.GenerateUniqueIconPath(newGroup.Id);
                File.Copy(preset.IconPath, newIco, true);
                newGroup.CustomIconPath = newIco;
            }

            _config.Groups.Add(newGroup);
            ConfigService.Instance.SaveConfig(_config);

            var vm = new SidebarGroupViewModel(newGroup);
            _sidebarGroups.Add(vm);
            GroupsListBox.SelectedItem = vm;
        }

        private void DeleteGroupCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: SidebarGroupViewModel vm })
            {
                string displayName = string.IsNullOrWhiteSpace(vm.Name) ? "Без названия" : vm.Name;
                var res = MessageBox.Show(
                    $"Вы уверены, что хотите удалить папку «{displayName}»?",
                    "Удаление папки",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    ConfigService.Instance.DeleteOldShortcutsForGroup(vm.Group.Id);
                    ConfigService.TryDeleteOldFile(vm.Group.CustomIconPath);
                    _config.Groups.Remove(vm.Group);
                    ConfigService.Instance.SaveConfig(_config);

                    int prevIdx = GroupsListBox.SelectedIndex;
                    RefreshGroupsList();

                    if (_sidebarGroups.Count > 0)
                    {
                        GroupsListBox.SelectedIndex = Math.Clamp(prevIdx, 0, _sidebarGroups.Count - 1);
                    }
                    else
                    {
                        _selectedGroup = null;
                    }
                }
            }
        }

        private void ChangeGroupIcon_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            var picker = new IconPickerWindow(_selectedGroup.Id)
            {
                Owner = this
            };

            if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedIconPath))
            {
                string? oldIcon = _selectedGroup.CustomIconPath;
                string newIcon = picker.SelectedIconPath;
                _selectedGroup.CustomIconPath = newIcon;

                IconService.InvalidateIcon(oldIcon);
                IconService.InvalidateIcon(newIcon);
                IconService.InvalidateIcon(_selectedGroup.Id);
                ConfigService.TryDeleteOldFile(oldIcon);

                GroupIconPreview.Source = IconService.GetIcon(null, newIcon, 64);
                SaveConfig();

                int idx = GroupsListBox.SelectedIndex;
                if (idx >= 0 && idx < _sidebarGroups.Count)
                {
                    _sidebarGroups[idx].NotifyChanges();
                }

                var grp = _selectedGroup;
                var all = _config.Groups.ToList();
                System.Threading.Tasks.Task.Run(() =>
                {
                    ConfigService.Instance.CreateTaskbarShortcut(grp, all);
                    ConfigService.Instance.UpdateTaskbarPinnedShortcuts(grp.Id, newIcon);
                });

                App.RefreshAllUI();
            }
        }

        private void GroupNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GroupNamePlaceholder.Visibility = string.IsNullOrEmpty(GroupNameTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (_isUpdatingUi || _selectedGroup == null) return;

            // Update in-memory model and sidebar card text with 0 ms UI thread latency
            _selectedGroup.Name = GroupNameTextBox.Text;

            int idx = GroupsListBox.SelectedIndex;
            if (idx >= 0 && idx < _sidebarGroups.Count)
            {
                _sidebarGroups[idx].NotifyChanges();
            }

            // Debounce disk I/O and shortcut sync by 600 ms
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        private void GroupNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            DebounceSaveAndSync();
        }

        private void DebounceSaveAndSync()
        {
            _saveDebounceTimer.Stop();
            if (_selectedGroup == null || _config == null) return;

            var grp = _selectedGroup;
            var all = _config.Groups.ToList();
            var cfg = _config;

            System.Threading.Tasks.Task.Run(() =>
            {
                ConfigService.Instance.SaveConfig(cfg);
                ConfigService.Instance.CreateTaskbarShortcut(grp, all);
                ConfigService.Instance.UpdateTaskbarPinnedShortcuts(grp.Id, grp.CustomIconPath ?? "");
            });
        }

        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            try
            {
                string shortcutPath = ConfigService.Instance.CreateTaskbarShortcut(_selectedGroup, _config.Groups);

                // Highlight shortcut in Windows Explorer
                Process.Start("explorer.exe", $"/select,\"{shortcutPath}\"");

                // Show animated Toast Banner
                ShowToastNotification();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания ярлыка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowToastNotification()
        {
            ToastBanner.Visibility = Visibility.Visible;
            ToastBanner.Opacity = 0.0;

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200));
            ToastBanner.BeginAnimation(OpacityProperty, fadeIn);

            // Auto-hide after 4 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
                fadeOut.Completed += (s2, e2) => ToastBanner.Visibility = Visibility.Collapsed;
                ToastBanner.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private void TestFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;
            App.ShowFlyoutWindow(_selectedGroup.Id);
        }

        // Segmented controls handlers
        private void TypeRadio_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || _selectedGroup == null) return;

            _selectedGroup.Type = TypeSyncRadio.IsChecked == true ? FolderType.Synchronized : FolderType.Virtual;
            SyncPathCard.Visibility = _selectedGroup.Type == FolderType.Synchronized ? Visibility.Visible : Visibility.Collapsed;
            SaveConfig();
            UpdateSidebarSubtitle();
        }

        private void ColsRadio_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || _selectedGroup == null) return;

            if (Col3Radio.IsChecked == true) _selectedGroup.Columns = 3;
            else if (Col5Radio.IsChecked == true) _selectedGroup.Columns = 5;
            else _selectedGroup.Columns = 4;

            SaveConfig();
            UpdateSidebarSubtitle();
        }

        private void ViewRadio_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || _selectedGroup == null) return;

            _selectedGroup.ShowLabels = ViewLabelsRadio.IsChecked == true;
            SaveConfig();
            RefreshLiveCanvas();
        }

        private void SyncPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi || _selectedGroup == null) return;
            _selectedGroup.SyncPath = SyncPathTextBox.Text.Trim();
            SaveConfig();
        }

        private void BrowseSyncPath_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            var ofd = new OpenFolderDialog
            {
                Title = "Выберите папку для синхронизации",
                InitialDirectory = _selectedGroup.SyncPath ?? ""
            };

            if (ofd.ShowDialog() == true && !string.IsNullOrEmpty(ofd.FolderName))
            {
                SyncPathTextBox.Text = ofd.FolderName;
            }
        }

        // Add items actions
        private void AddItemFile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            var ofd = new OpenFileDialog
            {
                Title = "Выберите файл или программу для добавления",
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                foreach (var file in ofd.FileNames)
                {
                    AddFileItem(file);
                }
            }
        }

        private void AddItemUrl_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            var dlg = new InputDialog("Добавить веб-ссылку", "Введите URL адрес (например: https://github.com):", "https://")
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResponseText))
            {
                var item = ConfigService.Instance.CreateFolderItem(dlg.ResponseText);
                _selectedGroup.Items.Add(item);
                SaveConfig();
                RefreshLiveCanvas();
                UpdateSidebarSubtitle();
            }
        }

        private void DropZone_Click(object sender, MouseButtonEventArgs e)
        {
            AddItemFile_Click(sender, e);
        }

        private void DropZone_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DropZoneCard.BorderBrush = (Brush)FindResource("AccentBrush");
                DropZoneCard.Background = (Brush)FindResource("AccentSoftBrush");
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void DropZone_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void DropZone_Drop(object sender, System.Windows.DragEventArgs e)
        {
            DropZoneCard.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 99, 102, 241));
            DropZoneCard.Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && _selectedGroup != null)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        AddFileItem(f);
                    }
                }
            }
        }

        private void AddFileItem(string path)
        {
            if (_selectedGroup == null) return;

            var item = ConfigService.Instance.CreateFolderItem(path);
            _selectedGroup.Items.Add(item);

            SaveConfig();
            RefreshLiveCanvas();
            UpdateSidebarSubtitle();
        }

        private void AutoRunCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool enable = AutoRunCheckBox.IsChecked == true;
            AutoRunService.SetAutoRun(enable);
        }

        private static T? FindVisualAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
