using System;
using System.Collections.Generic;
using System.Diagnostics;

using AppKit;
using Foundation;

namespace Xamarin.FindAllFiles.Mac
{
    public partial class FindResultsViewController : NSViewController, IFindResultsView
    {
        // TODO: LOL
        public static IFindResultsView CurrentFindResultsView { get; private set; }

        // TODO: Maybe immutable (need internet)
        readonly List<MacFindResultGroupViewModel> findResultGroups = new List<MacFindResultGroupViewModel>();
        readonly Stopwatch uiWorkStopwatch = new Stopwatch();
        int totalResultCount;
        TimeSpan? totalSearchTime;

        #region Constructors

        // Called when created from unmanaged code
        public FindResultsViewController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public FindResultsViewController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public FindResultsViewController() : base("FindResultsView", NSBundle.MainBundle)
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
            CurrentFindResultsView = this;

            resultsOutlineView.Delegate = new FindResultsOutlineViewDelegate(this);
            resultsOutlineView.DataSource = new FindResultsOutlineViewDataSource(this);

            progressSpinner.Hidden = true;
        }

        bool IFindResultsView.PushResults(IReadOnlyList<IFindResultGroupViewModel> results)
        {
            if (totalResultCount == 0)
            {
                uiWorkStopwatch?.Restart();
            }

            foreach (MacFindResultGroupViewModel resultGroup in results)
            {
                findResultGroups.Add(resultGroup);
                totalResultCount += resultGroup.Results.Count;
                resultsOutlineView.InsertItems(new NSIndexSet(findResultGroups.Count - 1), null, NSTableViewAnimation.None);
                resultsOutlineView.ExpandItem(resultGroup, true);
            }

            RefreshSummaryLabel();

            // TODO: Return false if total results exceed some limit
            // (right now the engine is determining the limit. who should?)
            return true;
        }

        void IFindResultsView.Clear()
        {
            findResultGroups.Clear();
            resultsOutlineView.ReloadData();

            totalResultCount = 0;
            totalSearchTime = null;
            RefreshSummaryLabel();
        }

        void IFindResultsView.BeginSearch()
        {
            ((IFindResultsView)this).Clear();

            progressSpinner.StartAnimation(this);
            progressSpinner.Hidden = false;
        }

        void IFindResultsView.EndSearch(TimeSpan totalSearchTime, bool canceled)
        {
            this.totalSearchTime = totalSearchTime;
            uiWorkStopwatch.Stop();
            progressSpinner.StopAnimation(this);
            progressSpinner.Hidden = true;
            RefreshSummaryLabel(canceled);
        }

        void RefreshSummaryLabel(bool canceled = false)
        {
            if (findResultGroups.Count > 0 || totalSearchTime.HasValue)
            {
                var fileOrFiles = findResultGroups.Count == 1 ? "file" : "files";
                var summary = $"{totalResultCount} results in {findResultGroups.Count} {fileOrFiles}";
                if (totalSearchTime != null)
                    summary += $" (completed in {Math.Floor(totalSearchTime.Value.TotalMilliseconds)}ms, {uiWorkStopwatch.ElapsedMilliseconds}ms of UI work)";
                if (canceled)
                    summary += " (cancelled due to too many results)";
                resultsSummaryLabel.StringValue = summary;
            }
            else
            {
                resultsSummaryLabel.StringValue = string.Empty;
            }
        }

        class FindResultsOutlineViewDelegate : NSOutlineViewDelegate
        {
            readonly FindResultsViewController viewController;

            public FindResultsOutlineViewDelegate(FindResultsViewController viewController)
            {
                this.viewController = viewController ?? throw new ArgumentNullException(nameof(viewController));
            }

            public override NSView GetView(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
            {
                var oview = outlineView.MakeView(tableColumn.Identifier, this);
                var view = oview as NSTableCellView;

                if (view == null)
                    view = new NSTableCellView();

                if (item == null)
                    view.TextField.StringValue = "ROOT";
                else if (item is MacFindResultGroupViewModel groupViewModel)
                {
                    var attributedBuffer = new NSMutableAttributedString(groupViewModel.FileName);
                    attributedBuffer.BeginEditing();
                    if (!string.IsNullOrEmpty(groupViewModel.RelativeFilePath))
                    {
                        attributedBuffer.Append(new NSAttributedString(
                            $" {groupViewModel.RelativeFilePath}",
                            //font: slightly smaller
                            foregroundColor: NSColor.Gray));
                    }
                    attributedBuffer.EndEditing();
                    view.TextField.AttributedStringValue = attributedBuffer;
                }
                else if (item is MacFindResultViewModel resultViewModel)
                {
                    // TODO: Trim string, adjust offsets. Decide what to do if offsets are in leading/trailing space (probably don't trim at all?)

                    var attributedBuffer = new NSMutableAttributedString();
                    attributedBuffer.BeginEditing();

                    if (resultViewModel.StartColumn > 0)
                        attributedBuffer.Append(new NSAttributedString(
                            resultViewModel.PreviewText.Substring(0, resultViewModel.StartColumn)));

                    attributedBuffer.Append(new NSAttributedString(
                        resultViewModel.PreviewText.Substring(
                            resultViewModel.StartColumn,
                            resultViewModel.EndColumn - resultViewModel.StartColumn),
                        backgroundColor: NSColor.FromRgb(240, 193, 163)));//240	193	163	

                    if (resultViewModel.EndColumn < resultViewModel.PreviewText.Length)
                        attributedBuffer.Append(new NSAttributedString(
                            resultViewModel.PreviewText.Substring(resultViewModel.EndColumn)));

                    attributedBuffer.EndEditing();
                    view.TextField.AttributedStringValue = attributedBuffer;
                }

                return view;
            }
        }

        class FindResultsOutlineViewDataSource : NSOutlineViewDataSource
        {
            readonly FindResultsViewController viewController;

            public FindResultsOutlineViewDataSource(FindResultsViewController viewController)
            {
                this.viewController = viewController ?? throw new ArgumentNullException(nameof(viewController));
            }

            public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
            {
                if (item == null)
                    return viewController.findResultGroups.Count;
                else if (item is MacFindResultGroupViewModel groupViewModel)
                    return groupViewModel.Results.Count;

                return 0;
            }

            public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
            {
                var index = (int)childIndex;

                if (item == null)// && index >= 0 && index < viewController.findResultGroups.Count)
                    return viewController.findResultGroups[index] as MacFindResultGroupViewModel;
                else if (item is MacFindResultGroupViewModel groupViewModel)
                    return groupViewModel.Results[index] as MacFindResultViewModel;

                return null;
            }

            public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
            {
                return item is MacFindResultGroupViewModel;
            }
        }
    }
}
