using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using TrayFolderFlyout.Models;
using TrayFolderFlyout.Services;

namespace TrayFolderFlyout.ViewModels
{
    public class FlyoutItemViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private ImageSource? _icon;
        private string _targetPath = "";
        private bool _isDirectory;
        private bool _showLabel = true;

        public FolderItem SourceItem { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string TargetPath
        {
            get => _targetPath;
            set
            {
                _targetPath = value;
                OnPropertyChanged();
            }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set
            {
                _isDirectory = value;
                OnPropertyChanged();
            }
        }

        public bool IsUrl { get; set; }

        public bool ShowLabel
        {
            get => _showLabel;
            set
            {
                if (_showLabel != value)
                {
                    _showLabel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsIconOnly));
                    OnPropertyChanged(nameof(IconSize));
                    OnPropertyChanged(nameof(CardWidth));
                    OnPropertyChanged(nameof(CardHeight));
                    OnPropertyChanged(nameof(LabelVisibility));
                }
            }
        }

        public bool IsIconOnly => !_showLabel;
        public Visibility LabelVisibility => _showLabel ? Visibility.Visible : Visibility.Collapsed;
        public double CardWidth => _showLabel ? 80 : 64;
        public double CardHeight => _showLabel ? 80 : 64;
        public double IconSize => _showLabel ? 32 : 44;

        public FlyoutItemViewModel(FolderItem item, bool showLabel = true)
        {
            SourceItem = item;
            _name = item.Name;
            _targetPath = item.TargetPath;
            _isDirectory = item.IsDirectory;
            IsUrl = item.IsUrl;
            _showLabel = showLabel;
            _icon = IconService.GetIcon(item.TargetPath, item.CustomIconPath, showLabel ? 32 : 44);
        }

        public void ReloadIcon()
        {
            Icon = IconService.GetIcon(SourceItem.TargetPath, SourceItem.CustomIconPath, _showLabel ? 32 : 44);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
