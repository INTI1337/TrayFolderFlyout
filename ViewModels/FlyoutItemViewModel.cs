using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        public FlyoutItemViewModel(FolderItem item)
        {
            SourceItem = item;
            _name = item.Name;
            _targetPath = item.TargetPath;
            _isDirectory = item.IsDirectory;
            IsUrl = item.IsUrl;
            _icon = IconService.GetIcon(item.TargetPath, item.CustomIconPath, 48);
        }

        public void ReloadIcon()
        {
            Icon = IconService.GetIcon(SourceItem.TargetPath, SourceItem.CustomIconPath, 48);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
