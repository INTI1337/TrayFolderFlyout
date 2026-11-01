using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TrayFolderFlyout.Services
{
    public class IpcMessage
    {
        public string Action { get; set; } = "ToggleFlyout"; // "ToggleFlyout", "OpenFlyout", "CloseFlyout", "OpenManager"
        public string? GroupId { get; set; }
        public int CursorX { get; set; }
        public int CursorY { get; set; }
    }

    public class IpcService
    {
        public const string MutexName = @"Global\TrayFolderFlyout_SingleInstance_Mutex";
        public const string PipeName = "TrayFolderFlyout_Pipe";

        private CancellationTokenSource? _cts;

        public static bool SendMessage(IpcMessage message, int timeoutMs = 800)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(timeoutMs);

                using var writer = new StreamWriter(client) { AutoFlush = true };
                string json = JsonSerializer.Serialize(message);
                writer.WriteLine(json);
                writer.Flush();
                try { client.WaitForPipeDrain(); } catch { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartServer(Action<IpcMessage> onMessageReceived)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(
                            PipeName,
                            PipeDirection.In,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous);

                        await server.WaitForConnectionAsync(token);

                        using var reader = new StreamReader(server);
                        string? line = await reader.ReadLineAsync(token);

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                var message = JsonSerializer.Deserialize<IpcMessage>(line);
                                if (message != null)
                                {
                                    onMessageReceived(message);
                                }
                            }
                            catch { }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Short delay before retrying pipe creation in case of transient errors
                        await Task.Delay(200, token);
                    }
                }
            }, token);
        }

        public void StopServer()
        {
            _cts?.Cancel();
        }
    }
}
