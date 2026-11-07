using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace TrayFolderFlyout.Launcher
{
    /// <summary>
    /// Ultra-light NativeAOT launcher for pinned taskbar shortcuts.
    /// Its only job is to turn a click into a single IPC message within a few milliseconds,
    /// instead of paying for a full WPF process start (measured: ~450 ms end to end).
    /// When no daemon is listening, the launch is handed over to the real application.
    /// </summary>
    internal static class Program
    {
        private const string PipeName = "TrayFolderFlyout_Pipe";
        private const string HostExeName = "TrayFolderFlyout.exe";

        // Short on purpose: a live daemon accepts in ~1 ms, a dead one must be detected fast.
        private const int ConnectTimeoutMs = 40;

        private static int Main(string[] args)
        {
            string? groupId = ParseGroupId(args);
            if (string.IsNullOrEmpty(groupId))
            {
                // Everything that is not a flyout request (manager, --exit, --daemon) stays with the app.
                return DelegateToHost(args);
            }

            long createdTick = GetProcessCreatedTick();
            long sentTick = Environment.TickCount64;
            GetCursorPos(out POINT pt);

            if (TrySend(BuildPayload(groupId!, pt.X, pt.Y, createdTick, sentTick)))
            {
                return 0;
            }

            return DelegateToHost(args);
        }

        /// <summary>Reads the group id from "--flyout &lt;id&gt;" / "-f &lt;id&gt;".</summary>
        private static string? ParseGroupId(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                bool isFlag = args[i].Equals("--flyout", StringComparison.OrdinalIgnoreCase)
                           || args[i].Equals("-f", StringComparison.OrdinalIgnoreCase);

                if (isFlag && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// Hand-built payload: no serializer, no reflection. The daemon still parses it as plain JSON,
        /// so old and new binaries can talk to each other.
        /// </summary>
        private static string BuildPayload(string groupId, int cursorX, int cursorY, long createdTick, long sentTick)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"Action\":\"ToggleFlyout\",\"GroupId\":\"").Append(EscapeJson(groupId))
              .Append("\",\"CursorX\":").Append(cursorX)
              .Append(",\"CursorY\":").Append(cursorY)
              .Append(",\"ClientCreatedTick\":").Append(createdTick)
              .Append(",\"ClientSentTick\":").Append(sentTick)
              .Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (value.IndexOf('"') < 0 && value.IndexOf('\\') < 0)
            {
                return value;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool TrySend(string payload)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(ConnectTimeoutMs);

                using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true)
                {
                    AutoFlush = true
                };
                writer.WriteLine(payload);
                writer.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Fallback: the daemon is not running (or not accepting). Start the real app with the same
        /// arguments — it will become the host and show the flyout itself.
        /// </summary>
        private static int DelegateToHost(string[] args)
        {
            try
            {
                string hostPath = Path.Combine(GetOwnDirectory(), HostExeName);
                if (!File.Exists(hostPath))
                {
                    return 1;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = hostPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (string arg in args)
                {
                    psi.ArgumentList.Add(arg);
                }

                Process.Start(psi);
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private static string GetOwnDirectory()
        {
            string? self = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(self))
            {
                string? dir = Path.GetDirectoryName(self);
                if (!string.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// Monotonic tick (ms since boot) at which the OS created this process.
        /// The daemon uses it to measure the full click -> visible frame latency.
        /// </summary>
        private static long GetProcessCreatedTick()
        {
            long now = Environment.TickCount64;
            try
            {
                using var proc = Process.GetCurrentProcess();
                double ageMs = (DateTime.Now - proc.StartTime).TotalMilliseconds;
                return Math.Max(0, now - (long)ageMs);
            }
            catch
            {
                return now;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);
    }
}
