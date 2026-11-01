using System;
using System.Linq;
using System.Windows;

namespace TrayFolderFlyout.Setup
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                                a.Equals("-u", StringComparison.OrdinalIgnoreCase)))
            {
                bool silent = e.Args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                                              a.Equals("-s", StringComparison.OrdinalIgnoreCase));
                InstallerLogic.Uninstall(silent);
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
