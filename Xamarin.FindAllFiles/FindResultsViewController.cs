using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Diagnostics;

namespace Xamarin.FindAllFiles
{
    public partial class FindResultsViewController : NSViewController, IFindResultsView
    {
        // TODO: LOL
        public static IFindResultsView CurrentFindResultsView { get; private set; }

        public static IFindResultFactory CurrentFindResultsFactory { get; } = new MacFindResultFactory();

        // TODO: Maybe immutable (need internet)
        readonly List<FindResultGroupViewModel> findResultGroups = new List<FindResultGroupViewModel>();
        int totalResultCount;
        int rowCount;
        TimeSpan? totalSearchTime;
        Stopwatch uiWorkStopwatch = new Stopwatch();

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
        }

        bool IFindResultsView.PushResults(IReadOnlyList<IFindResultGroupViewModel> results)
        {
            if (totalResultCount == 0)
            {
                uiWorkStopwatch?.Restart();
            }

            foreach (FindResultGroupViewModel resultGroup in results)
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
            rowCount = 0;
            totalSearchTime = null;
            RefreshSummaryLabel();
        }

        void IFindResultsView.EndSearch(TimeSpan totalSearchTime, bool canceled)
        {
            this.totalSearchTime = totalSearchTime;
            uiWorkStopwatch.Stop();
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
                else if (item is FindResultGroupViewModel groupViewModel)
                    view.TextField.StringValue = $"{groupViewModel.FileName} ({groupViewModel.RelativeFilePath})";
                else if (item is FindResultViewModel resultViewModel)
                    view.TextField.StringValue = resultViewModel.PreviewText;

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
                else if (item is FindResultGroupViewModel groupViewModel)
                    return groupViewModel.Results.Count;

                return 0;
            }

            public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
            {
                var index = (int)childIndex;

                // TODO: OMG STOP RECREATING
                // TODO: Using immutable here would help prevent sync issues (but realistically we shouldn't be creating stuff here at all)
                if (item == null)// && index >= 0 && index < viewController.findResultGroups.Count)
                    return viewController.findResultGroups[index] as FindResultGroupViewModel;
                else if (item is FindResultGroupViewModel groupViewModel)
                    return groupViewModel.Results[index] as FindResultViewModel;

                return null;
            }

            public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
            {
                return item is FindResultGroupViewModel;
            }
        }
    }

    public interface IFindResultGroupViewModel
    {
        string FileName { get; }

        string RelativeFilePath { get; }

        IReadOnlyList<IFindResultViewModel> Results { get; }
    }

    public interface IFindResultViewModel
    {
        string PreviewText { get; }

        int Line { get; }

        int Column { get; }
    }

    public class FindResultGroupViewModel : NSObject, IFindResultGroupViewModel
    {
        public string FileName { get; }

        public string RelativeFilePath { get; }

        public IReadOnlyList<IFindResultViewModel> Results { get; }

        // TODO: ImageId for icon?

        // TODO: Any state needed if user removes group from view? VScode lets you remove entire group and individual results

        public FindResultGroupViewModel(IntPtr handle) : base(handle) { }

        public FindResultGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("fileName must be set", nameof(fileName));
            }

            FileName = fileName;
            RelativeFilePath = relativeFilePath ?? throw new ArgumentException(nameof(relativeFilePath));
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }
    }

    public class FindResultViewModel : NSObject, IFindResultViewModel
    {
        public string PreviewText { get; }

        public int Line { get; }

        public int Column { get; }

        public FindResultViewModel(IntPtr handle) : base(handle) { }

        public FindResultViewModel(string previewText, int line, int column)
        {
            PreviewText = previewText ?? throw new ArgumentNullException(nameof(previewText));
            Line = line;
            Column = column;
        }
    }

    public class MacFindResultFactory : IFindResultFactory
    {
        public IFindResultGroupViewModel CreateGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results)
            => new FindResultGroupViewModel(fileName, relativeFilePath, results);

        public IFindResultViewModel CreateResultViewModel(string previewText, int line, int column)
            => new FindResultViewModel(previewText, line, column);
    }

    public interface IFindResultsView
    {
        bool PushResults(IReadOnlyList<IFindResultGroupViewModel> results);

        // TODO: Do we need a BeginSearch, or do we just count on results coming quickly enough that it doesn't matter?
        void EndSearch(TimeSpan totalSearchTime, bool canceled = false);

        void Clear();
    }

    public interface IFindResultFactory
    {
        IFindResultGroupViewModel CreateGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results);

        IFindResultViewModel CreateResultViewModel(string previewText, int line, int column);
    }
}
