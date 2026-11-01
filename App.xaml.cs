using System;
using System.IO;
using System.Threading;
using System.Windows;
using TrayFolderFlyout.Native;
using TrayFolderFlyout.Services;
using TrayFolderFlyout.Views;

namespace TrayFolderFlyout
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        private static IpcService? _ipcService;
        private static TrayService? _trayService;

        private static FlyoutWindow? _flyoutWindow;
        private static MainWindow? _mainWindow;

        public static void Log(string msg)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TrayFolderFlyout");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "run.log");
                File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log($"OnStartup: args count={e.Args.Length}");

            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                Log("FATAL UnhandledException: " + ev.ExceptionObject);
            };

            this.DispatcherUnhandledException += (s, ev) =>
            {
                Log("FATAL DispatcherUnhandledException: " + ev.Exception);
                ev.Handled = true;
            };

            // Parse incoming command line arguments
            string? flyoutGroupId = null;
            string? configGroupId = null;
            bool isDaemon = false;
            bool isExitCommand = false;

            string[] args = e.Args;
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Equals("--flyout", StringComparison.OrdinalIgnoreCase) ||
                     args[i].Equals("-f", StringComparison.OrdinalIgnoreCase)) &&
                    i + 1 < args.Length)
                {
                    flyoutGroupId = args[i + 1];
                    i++;
                }
                else if ((args[i].Equals("--config", StringComparison.OrdinalIgnoreCase) ||
                          args[i].Equals("-c", StringComparison.OrdinalIgnoreCase) ||
                          args[i].Equals("--manager", StringComparison.OrdinalIgnoreCase)) &&
                         i + 1 < args.Length)
                {
                    configGroupId = args[i + 1];
                    i++;
                }
                else if (args[i].Equals("--daemon", StringComparison.OrdinalIgnoreCase) ||
                         args[i].Equals("-d", StringComparison.OrdinalIgnoreCase))
                {
                    isDaemon = true;
                }
                else if (args[i].Equals("--exit", StringComparison.OrdinalIgnoreCase) ||
                         args[i].Equals("-k", StringComparison.OrdinalIgnoreCase) ||
                         args[i].Equals("--kill", StringComparison.OrdinalIgnoreCase))
                {
                    isExitCommand = true;
                }
            }

            // 1. SINGLE-INSTANCE HOST / CLIENT CHECK
            _singleInstanceMutex = new Mutex(true, IpcService.MutexName, out bool createdNew);
            Log($"Mutex check: MutexName={IpcService.MutexName}, createdNew={createdNew}");
            bool isHost = createdNew;

            if (!createdNew)
            {
                if (isExitCommand)
                {
                    IpcService.SendMessage(new IpcMessage { Action = "ExitDaemon" }, timeoutMs: 800);
                    Environment.Exit(0);
                    return;
                }

                Win32.GetCursorPos(out POINT pt);
                var msg = new IpcMessage
                {
                    Action = !string.IsNullOrEmpty(flyoutGroupId) ? "ToggleFlyout" : "OpenManager",
                    GroupId = flyoutGroupId ?? configGroupId,
                    CursorX = pt.X,
                    CursorY = pt.Y
                };

                Log($"Client instance: sending IPC command to host (action={msg.Action}, group={msg.GroupId ?? "null"})...");
                bool sent = IpcService.SendMessage(msg, timeoutMs: 500);
                if (sent)
                {
                    Log("Client instance: IPC delivered in <1ms, exiting immediately.");
                    Environment.Exit(0);
                    return;
                }

                // If host didn't respond to pipe, it might have terminated abnormally.
                Log("Host did not respond on pipe. Checking if mutex is abandoned...");
                try
                {
                    if (_singleInstanceMutex.WaitOne(TimeSpan.FromMilliseconds(200), false))
                    {
                        isHost = true;
                    }
                }
                catch (AbandonedMutexException)
                {
                    isHost = true;
                }

                if (!isHost)
                {
                    Log("Could not acquire mutex or communicate with host, exiting.");
                    Environment.Exit(0);
                    return;
                }
            }

            if (isExitCommand)
            {
                Environment.Exit(0);
                return;
            }

            // 2. PRIMARY DAEMON HOST INITIALIZATION
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Log("Daemon host starting up...");

            // Initialize built-in icon presets and app brand icon
            PresetIconsService.EnsureInitialized();
            AppIconService.EnsureAppIcon(Path.Combine(ConfigService.Instance.IconsDirectory, "app.ico"));

            // Warm up configuration and perform autonomous repair on startup
            ConfigService.Instance.LoadConfig();

            // Setup Named Pipe server to handle instant requests
            _ipcService = new IpcService();
            _ipcService.StartServer(OnIpcMessageReceived);

            // Initialize System Tray Icon
            try
            {
                _trayService = new TrayService(
                    openManagerAction: () => Dispatcher.InvokeAsync(() => ShowManagerWindow()),
                    exitAction: () => Dispatcher.InvokeAsync(() => ExitApplication())
                );
                Log("TrayService initialized successfully");
            }
            catch (Exception ex)
            {
                Log("TrayService error: " + ex);
            }

            // Create pre-warmed flyout window instance in memory
            _flyoutWindow = new FlyoutWindow();

            // Handle initial startup mode
            if (!string.IsNullOrEmpty(flyoutGroupId))
            {
                Win32.GetCursorPos(out POINT pt);
                _flyoutWindow.ShowFlyout(flyoutGroupId, pt.X, pt.Y);
            }
            else if (!isDaemon)
            {
                ShowManagerWindow(configGroupId);
            }
        }

        private void OnIpcMessageReceived(IpcMessage msg)
        {
            Log($"OnIpcMessageReceived: Action={msg.Action}, GroupId={msg.GroupId}");
            Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (msg.Action.Equals("ToggleFlyout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(msg.GroupId))
                    {
                        _flyoutWindow ??= new FlyoutWindow();

                        // Toggle logic: if already visible for this group (and open > 200ms), smoothly close
                        if (_flyoutWindow.IsVisible && _flyoutWindow.CurrentGroupId == msg.GroupId)
                        {
                            long elapsed = Environment.TickCount64 - _flyoutWindow.OpenedTimestamp;
                            if (elapsed > 200)
                            {
                                Log("ToggleFlyout: hiding existing open flyout");
                                _flyoutWindow.HideFlyout();
                                return;
                            }
                        }
                        else if (Environment.TickCount64 - _flyoutWindow.ClosedTimestamp < 350 && _flyoutWindow.CurrentGroupId == msg.GroupId)
                        {
                            Log("ToggleFlyout: suppressed reopen because flyout was just closed by taskbar click deactivation");
                            return;
                        }

                        Log("ToggleFlyout: showing flyout");
                        _flyoutWindow.ShowFlyout(msg.GroupId, msg.CursorX, msg.CursorY);
                    }
                    else if (msg.Action.Equals("OpenFlyout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(msg.GroupId))
                    {
                        _flyoutWindow ??= new FlyoutWindow();
                        _flyoutWindow.ShowFlyout(msg.GroupId, msg.CursorX, msg.CursorY);
                    }
                    else if (msg.Action.Equals("CloseFlyout", StringComparison.OrdinalIgnoreCase))
                    {
                        _flyoutWindow?.HideFlyout();
                    }
                    else if (msg.Action.Equals("OpenManager", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowManagerWindow(msg.GroupId);
                    }
                    else if (msg.Action.Equals("ExitDaemon", StringComparison.OrdinalIgnoreCase))
                    {
                        ExitApplication();
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception in OnIpcMessageReceived handler: " + ex);
                }
            });
        }

        public static void ShowFlyoutWindow(string groupId, int cursorX = 0, int cursorY = 0)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _flyoutWindow ??= new FlyoutWindow();
                _flyoutWindow.ShowFlyout(groupId, cursorX, cursorY);
            });
        }

        public static void ShowManagerWindow(string? groupId = null)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_mainWindow == null || !_mainWindow.IsLoaded)
                {
                    _mainWindow = new MainWindow(groupId);
                }

                _mainWindow.Show();
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Activate();
                _mainWindow.Focus();
                var hwnd = new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle;
                Win32.SetForegroundWindow(hwnd);
            });
        }

        public static void RefreshAllUI()
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.RefreshGroupsList();
                    _mainWindow.RefreshLiveCanvas();
                }
                if (_flyoutWindow != null && _flyoutWindow.IsLoaded)
                {
                    _flyoutWindow.ReloadCurrentViewFromConfig();
                }
            });
        }

        public static void ExitApplication()
        {
            Log("ExitApplication called!");
            _trayService?.Dispose();
            _ipcService?.StopServer();

            try
            {
                _flyoutWindow?.Close();
                _mainWindow?.Close();
            }
            catch { }

            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();

            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log($"OnExit triggered: exitCode={e.ApplicationExitCode}");
            _trayService?.Dispose();
            _ipcService?.StopServer();
            base.OnExit(e);
        }
    }
}
