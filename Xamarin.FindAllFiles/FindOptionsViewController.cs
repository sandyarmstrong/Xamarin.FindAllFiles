using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

using AppKit;
using Foundation;

namespace Xamarin.FindAllFiles
{
    public partial class FindOptionsViewController : AppKit.NSViewController
    {
        #region Constructors

        // Called when created from unmanaged code
        public FindOptionsViewController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public FindOptionsViewController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public FindOptionsViewController() : base("FindOptionsView", NSBundle.MainBundle)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        public override void ViewDidLoad()
        {
            foreach (var control in new NSControl [] { searchField, workingDirectoryField, includeField, excludeField, matchCaseButton, matchWholeWordButton, regexButton, findButton })
            {
                control.Target = this;
                control.Action = new ObjCRuntime.Selector("searchRequested:");
            }

            workingDirectoryField.StringValue = "/Users/sandy/xam-git/monodevelop";
        }

        IFindResultsView findResultsView;
        IFindResultFactory findResultFactory;
        bool isSearching;
        FindOptionsViewModel lastFindOptions;

        // These are straight from vscode
        static long maxFileSize = (long)16 * 1024 * 1024 * 1024;
        static int maxResults = 10000;

        [Export("searchRequested:")]
        private void OnSearchRequested(NSObject sender)
        {
            if (isSearching)
                return;

            // TODO: Only trigger search if stuff has changed (looks like we need a viewmodel here!)

            var viewModel = new FindOptionsViewModel
            {
                Query = searchField.StringValue,
                WorkingDirectory = workingDirectoryField.StringValue,
                Include = includeField.StringValue,
                Exclude = includeField.StringValue,
                MatchCase = matchCaseButton.State == NSCellStateValue.On,
                MatchWholeWord = matchWholeWordButton.State == NSCellStateValue.On,
                IsRegex = regexButton.State == NSCellStateValue.On,
            };

            if (sender != findButton && viewModel == lastFindOptions)
                return;

            lastFindOptions = viewModel;

            var searchString = viewModel.Query;

            if (string.IsNullOrEmpty(searchString))
                return;

            isSearching = true;

            // TODO: Need cancellation and stuff
            findButton.Enabled = false;

            findResultFactory = FindResultsViewController.CurrentFindResultsFactory;
            findResultsView = FindResultsViewController.CurrentFindResultsView;
            findResultsView.Clear();

            // TODO: Multiline
            var matchCaseArgs = viewModel.MatchCase ? "--case-sensitive" : "--ignore-case";
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

            // TODO: Why do I (now) consistently get 335 results in 107 files for "monodevelop", but vscode gets 391 in 111? Clearly need to play with options
            //       My numbers at least match what I'm getting back from rg (can check summary if using --json)

            // TODO: When we have internet again, switch to use json output like vscode
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/bin/rg",
                    WorkingDirectory = String.IsNullOrEmpty(viewModel.WorkingDirectory) ? "/Users/sandy/xam-git/monodevelop" : viewModel.WorkingDirectory,
                    Arguments = $"\"{searchString}\" --json --max-filesize {maxFileSize} {matchCaseArgs}", // TODO: Real escaping, etc
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

                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                //new Utf8JsonReader(
                                var message = JsonSerializer.Deserialize<RipGrepMessage>(e.Data);
                                if (message.type == "match")
                                {
                                    filePath = message.data.path.GetText();
                                    data = message.data.lines.GetText().TrimStart();
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

                                BeginInvokeOnMainThread(() =>
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

                            if (data != string.Empty)
                            {
                                currentGroup.Add(findResultFactory.CreateResultViewModel(data, 0, 0));
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
                    isSearching = false;

                    Console.Error.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");

                    BeginInvokeOnMainThread(() =>
                    {
                        findResultsView.EndSearch(sw.Elapsed, canceled: killed);
                        findButton.Enabled = true;
                    });
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            });
        }
    }

    class FindOptionsViewModel : IEquatable<FindOptionsViewModel>
    {
        public bool MatchCase { get; set; }

        public bool MatchWholeWord { get; set; }

        public bool IsRegex { get; set; }

        public string Query { get; set; }

        public string WorkingDirectory { get; set; }

        public string Include { get; set; }

        public string Exclude { get; set; }

        public bool Equals(FindOptionsViewModel other)
        {
            return other != null &&
                MatchCase == other.MatchCase &&
                MatchWholeWord == other.MatchWholeWord &&
                IsRegex == other.IsRegex &&
                Query == other.Query &&
                WorkingDirectory == other.WorkingDirectory &&
                Include == other.Include &&
                Exclude == other.Exclude;
        }

        public override bool Equals(object obj)
        {
            return obj is FindOptionsViewModel other && this == other;
        }

        public override int GetHashCode()
        {
            var hash = 23;
            hash = hash * 31 + MatchCase.GetHashCode();
            hash = hash * 31 + MatchWholeWord.GetHashCode();
            hash = hash * 31 + IsRegex.GetHashCode();
            if (Query != null)
                hash = hash * 31 + Query.GetHashCode();
            if (WorkingDirectory != null)
                hash = hash * 31 + WorkingDirectory.GetHashCode();
            if (Include != null)
                hash = hash * 31 + Include.GetHashCode();
            if (Exclude != null)
                hash = hash * 31 + Exclude.GetHashCode();
            return hash;
        }

        public static bool operator ==(FindOptionsViewModel left, FindOptionsViewModel right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.MatchCase == right.MatchCase &&
                left.MatchWholeWord == right.MatchWholeWord &&
                left.IsRegex == right.IsRegex &&
                left.Query == right.Query &&
                left.WorkingDirectory == right.WorkingDirectory &&
                left.Include == right.Include &&
                left.Exclude == right.Exclude;
        }

        public static bool operator !=(FindOptionsViewModel left, FindOptionsViewModel right)
            => !(left == right);
    }

    class RipGrepMessage
    {
        public string type { get; set; }
        public RipGrepMatch data { get; set; }
    }

    class RipGrepMatch
    {
        public RipGrepBytesOrText path { get; set; }
        public RipGrepBytesOrText lines { get; set; }
        public int line_number { get; set; }
        public int absolute_offset { get; set; }
        public IList<RipGrepSubMatch> submatches { get; set; }
    }

    class RipGrepSubMatch
    {
        public RipGrepBytesOrText match { get; set; }
        public int start { get; set; }
        public int end { get; set; }
    }

    class RipGrepBytesOrText
    {
        public string bytes { get; set; }

        public string text { get; set; }

        public string GetText()
        {
            if (!string.IsNullOrEmpty(bytes))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes));
            }
            else
            {
                return text;
            }
        }
    }
}
