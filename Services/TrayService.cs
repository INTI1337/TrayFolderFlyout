using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace TrayFolderFlyout.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly Action _openManagerAction;
        private readonly Action _exitAction;

        public TrayService(Action openManagerAction, Action exitAction)
        {
            _openManagerAction = openManagerAction;
            _exitAction = exitAction;
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "TrayFolderFlyout — Папки панели задач"
            };

            // 1. Assign Icon first
            try
            {
                var sri = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
                if (sri?.Stream != null)
                {
                    using var s = sri.Stream;
                    _notifyIcon.Icon = new Icon(s);
                }
                else
                {
                    string appIco = Path.Combine(ConfigService.Instance.IconsDirectory, "app.ico");
                    if (File.Exists(appIco))
                    {
                        _notifyIcon.Icon = new Icon(appIco);
                    }
                    else
                    {
                        string? exePath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application;
                        }
                        else
                        {
                            _notifyIcon.Icon = SystemIcons.Application;
                        }
                    }
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // 2. Make visible after icon is assigned
            _notifyIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();

            var openManagerItem = new ToolStripMenuItem("⚙️ Открыть менеджер папок", null, (s, e) =>
            {
                _openManagerAction();
            })
            {
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
            };

            var autoRunItem = new ToolStripMenuItem("🚀 Автозапуск с Windows", null, (s, e) =>
            {
                if (s is ToolStripMenuItem item)
                {
                    bool current = AutoRunService.IsAutoRunEnabled();
                    AutoRunService.SetAutoRun(!current);
                    item.Checked = !current;
                }
            })
            {
                Checked = AutoRunService.IsAutoRunEnabled()
            };

            var exitItem = new ToolStripMenuItem("❌ Выход", null, (s, e) =>
            {
                _exitAction();
            });

            contextMenu.Items.Add(openManagerItem);
            contextMenu.Items.Add(autoRunItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _openManagerAction();
            };
        }

        public void UpdateIcon(string icoPath)
        {
            try
            {
                if (File.Exists(icoPath) && _notifyIcon != null)
                {
                    _notifyIcon.Icon = new Icon(icoPath);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
