using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

        public async Task SearchFilesAsync(
            FindOptionsViewModel viewModel,
            bool forceSearch,
            CancellationToken cancellationToken = default)
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

            var rgEngine = new RipGrepEngine();

            var rgOptions = new FindOptions
            {
                NoConfig = true,
                MaxFileSize = maxFileSize,
                CrlfSupport = true,
                NoIgnore = !viewModel.UseExcludeSettingsAndIgnoreFiles,
                NoIgnoreParent = viewModel.UseExcludeSettingsAndIgnoreFiles,
                CaseSensitive = viewModel.MatchCase,
                IgnoreCase = !viewModel.MatchCase,
                WorkingDirectory = viewModel.WorkingDirectory,
                Query = searchString,
            };

            // TODO: Why do I (now) consistently get 335 results in 107 files for "monodevelop", but vscode gets 391 in 111? Clearly need to play with options
            //       My numbers at least match what I'm getting back from rg (can check summary if using --json)

            var cancellationTokenSource = new CancellationTokenSource();
            var killed = false;
            var sw = new Stopwatch();
            sw.Start();

            var currentGroup = new List<IFindResultViewModel>();
            var currentGroupFilePath = string.Empty;
            var totalResults = 0;

            var mainSyncContext = SynchronizationContext.Current;

            await rgEngine.SearchAsync(rgOptions, HandleMessage, cancellationTokenSource.Token);

            sw.Stop();
            IsSearching = false;

            findResultsView.EndSearch(sw.Elapsed, canceled: killed);
            findOptionsView.EndSearch();

            void HandleMessage(Message message)
            {
                if (totalResults > maxResults)
                    return;

                var filePath = string.Empty;
                var data = string.Empty;
                RipGrep.Match match = null;

                if (message.Type == "match")
                {
                    match = message.Data;
                    filePath = match.Path.GetText();
                    data = match.Lines.GetText();
                }

                if (currentGroupFilePath != string.Empty && filePath != currentGroupFilePath)
                {
                    var resultsToPush = new[] {
                                    findResultFactory.CreateGroupViewModel(
                                        Path.GetFileName(currentGroupFilePath),
                                        Path.GetDirectoryName(currentGroupFilePath),
                                        currentGroup),
                                };

                    mainSyncContext.Post(_ =>
                    {
                        try
                        {
                            findResultsView.PushResults(resultsToPush);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                        }
                    }, null);

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
                        cancellationTokenSource.Cancel();
                    }
                }
            }
        }
    }
}
