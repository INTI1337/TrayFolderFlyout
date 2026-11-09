using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TrayFolderFlyout.Native;
using TrayFolderFlyout.Services;

namespace TrayFolderFlyout.Views
{
    public partial class PinnedItemsDialog : Window
    {
        /// <summary>One pinned shortcut found on disk, with the visual state of its checkbox.</summary>
        public class PinnedEntry : INotifyPropertyChanged
        {
            private bool _isSelected;

            public string Name { get; init; } = "";
            public string Path { get; init; } = "";
            public string Source { get; init; } = "";
            public ImageSource? Icon { get; init; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? propName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private readonly ObservableCollection<PinnedEntry> _entries = new();

        public List<string> SelectedPaths { get; } = new();

        public PinnedItemsDialog()
        {
            InitializeComponent();
            ItemsList.ItemsSource = _entries;
            LoadEntries();

            Loaded += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                Win32.ApplyWin11Style(hwnd, ThemeService.IsDarkTheme(), useAcrylic: true);
            };
        }

        private void LoadEntries()
        {
            foreach (var item in PinnedShortcutsService.Load())
            {
                _entries.Add(new PinnedEntry
                {
                    Name = item.Name,
                    Path = item.Path,
                    Source = item.Source,
                    Icon = IconService.GetIcon(item.Path, null, 24)
                });
            }

            EmptyText.Visibility = _entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedPaths.Clear();
            SelectedPaths.AddRange(_entries.Where(x => x.IsSelected).Select(x => x.Path));
            DialogResult = SelectedPaths.Count > 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
