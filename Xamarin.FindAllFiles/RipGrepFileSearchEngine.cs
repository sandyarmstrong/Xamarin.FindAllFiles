using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.RipGrep;

namespace Xamarin.FindAllFiles
{
    public class RipGrepFileSearchEngine : IFileSearchEngine
    {
        readonly IFindResultFactory findResultFactory;
        readonly IFindResultsView findResultsView;
        readonly IFindOptionsView findOptionsView;

        FindOptionsViewModel lastFindOptions;

        // These are straight from vscode
        static long maxFileSize = (long)16 * 1024 * 1024 * 1024;
        static int maxResults = 10000;

        public bool IsSearching { get; private set; }

        public RipGrepFileSearchEngine(IFindResultFactory findResultFactory, IFindResultsView findResultsView, IFindOptionsView findOptionsView)
        {
            this.findResultFactory = findResultFactory ?? throw new ArgumentNullException(nameof(findResultFactory));
            this.findResultsView = findResultsView ?? throw new ArgumentNullException(nameof(findResultsView));
            this.findOptionsView = findOptionsView ?? throw new ArgumentNullException(nameof(findOptionsView));
        }

        public void SearchFiles(FindOptionsViewModel viewModel, bool forceSearch, Action<Action> invokeOnMainThread)
        {
            if (IsSearching)
                return;

            if (!forceSearch && viewModel == lastFindOptions)
                return;

            lastFindOptions = viewModel;

            var searchString = viewModel.Query;

            if (string.IsNullOrEmpty(searchString))
                return;

            IsSearching = true;

            // TODO: Need cancellation and stuff
            findOptionsView.BeginSearch();
            findResultsView.BeginSearch();

            var args = $"--no-config --json --max-filesize {maxFileSize} --crlf";

            // TODO: global vs local? need to untangle vscode a bit more
            if (viewModel.UseExcludeSettingsAndIgnoreFiles)
                args += " --no-ignore-parent";
            else
                args += " --no-ignore";

            // TODO: Multiline
            if (viewModel.MatchCase)
                args += " --case-sensitive";
            else
                args += " --ignore-case";

            var isRegex = viewModel.IsRegex;

            if (!isRegex)
            {
                searchString = EscapeRegExpCharacters(searchString);

                string EscapeRegExpCharacters(string val)
                {
                    return Regex.Replace(val, "[\\{}*+?|^$.[]()]", "\\$&");
                }
            }

            if (viewModel.MatchWholeWord)
            {
                if (!Regex.IsMatch(searchString.Substring(0, 1), "\\B"))
                {
                    searchString = "\\b" + searchString;
                }

                if (!Regex.IsMatch(searchString.Substring(searchString.Length - 1, 1), "\\B"))
                {
                    searchString += "\\b";
                }
            }

            // TODO: Real escaping, etc
            args = $"\"{searchString}\" {args}";

            // TODO: Why do I (now) consistently get 335 results in 107 files for "monodevelop", but vscode gets 391 in 111? Clearly need to play with options
            //       My numbers at least match what I'm getting back from rg (can check summary if using --json)

            // TODO: When we have internet again, switch to use json output like vscode
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/bin/rg",
                    WorkingDirectory = String.IsNullOrEmpty(viewModel.WorkingDirectory) ? "/Users/sandy/xam-git/monodevelop" : viewModel.WorkingDirectory,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };

            var killed = false;
            var sw = new Stopwatch();
            sw.Start();

            Task.Run(() =>
            {
                try
                {
                    p.Start();
                    p.BeginOutputReadLine();

                    var currentGroup = new List<IFindResultViewModel>();
                    var currentGroupFilePath = string.Empty;
                    var totalResults = 0;

                    // TODO: Check if we guarantee to get full lines here. Actually just use mirepoix when you have internet
                    //       Looks like we get a line at a time.
                    p.OutputDataReceived += HandleOutputDataReceived;

                    void HandleOutputDataReceived(object o, DataReceivedEventArgs e)
                    {
                        if (totalResults > maxResults)
                            return;

                        try
                        {
                            var filePath = string.Empty;
                            var data = string.Empty;
                            RipGrep.Match match = null;

                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                //new Utf8JsonReader(
                                var message = JsonSerializer.Deserialize<Message>(e.Data);
                                if (message.Type == "match")
                                {
                                    match = message.Data;
                                    filePath = match.Path.GetText();
                                    data = match.Lines.GetText();
                                }
                            }

                            if (currentGroupFilePath != string.Empty && filePath != currentGroupFilePath)
                            {
                                var resultsToPush = new[] {
                                    findResultFactory.CreateGroupViewModel(
                                        Path.GetFileName(currentGroupFilePath),
                                        Path.GetDirectoryName(currentGroupFilePath),
                                        currentGroup),
                                };

                                invokeOnMainThread(() =>
                                {
                                    try
                                    {
                                        findResultsView.PushResults(resultsToPush);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine(ex);
                                    }
                                });

                                currentGroup = new List<IFindResultViewModel>();
                                currentGroupFilePath = filePath;
                            }
                            else if (currentGroupFilePath == string.Empty)
                            {
                                currentGroupFilePath = filePath;
                            }

                            if (data != string.Empty && match != null)
                            {
                                var submatch = match.Submatches.FirstOrDefault();

                                currentGroup.Add(findResultFactory.CreateResultViewModel(
                                    data,
                                    match.LineNumber,
                                    submatch?.Start ?? 0,
                                    submatch?.End ?? data.Length));

                                totalResults++;

                                if (totalResults > maxResults)
                                {
                                    killed = true;
                                    p.Kill();
                                    p.OutputDataReceived -= HandleOutputDataReceived;
                                    Console.Error.WriteLine("Exceeded max allowed results; killing search");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                        }
                    };

                    p.WaitForExit();
                    sw.Stop();
                    IsSearching = false;

                    Console.Error.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");

                    invokeOnMainThread(() =>
                    {
                        findResultsView.EndSearch(sw.Elapsed, canceled: killed);
                        findOptionsView.EndSearch();
                    });
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            });
        }
    }
}
