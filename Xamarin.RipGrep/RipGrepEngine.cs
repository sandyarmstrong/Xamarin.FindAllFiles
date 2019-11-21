using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

//using Xamarin.ProcessControl;

namespace Xamarin.RipGrep
{
    public class RipGrepEngine
    {
        public Task SearchAsync(
            FindOptions options,
            Action<Message> messageHandler,
            CancellationToken cancellationToken)
        {
            // TODO: Nope, Exec doesn't support timeouts or cancellation/killing of any kind
            //await Exec.RunAsync(
            //    ExecFlags.RedirectStdout,
            //    HandleOutputSegment,
            //    "/usr/local/bin/rg",
            //    options.ToArgumentList().ToArray());

            //void HandleOutputSegment(ConsoleRedirection.Segment segment)
            //{
            //}

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/bin/rg",
                    WorkingDirectory = String.IsNullOrEmpty(options.WorkingDirectory) ? Environment.CurrentDirectory : options.WorkingDirectory,
                    Arguments = options.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };

            return Task.Run(() =>
            {
                cancellationToken.Register(() =>
                {
                    p.OutputDataReceived -= HandleOutputDataReceived;
                    p.Kill();

                });

                p.OutputDataReceived += HandleOutputDataReceived;
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                void HandleOutputDataReceived(object o, DataReceivedEventArgs e)
                {
                    // TODO: try/catch here, maybe elsewhere, maybe put exceptions on Task object
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var message = JsonSerializer.Deserialize<Message>(e.Data);
                        messageHandler(message);
                    }
                }
            }, cancellationToken);
        }
    }
}
