using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Xamarin.FindAllFiles
{
    public partial class FindResultsViewController : NSViewController, IFindResultsView
    {
        // TODO: Maybe immutable (need internet)
        readonly List<FindResultGroupViewModel> findResultGroups = new List<FindResultGroupViewModel>();
        int totalResultCount;

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
            resultsOutlineView.Delegate = new FindResultsOutlineViewDelegate();
            resultsOutlineView.DataSource = new FindResultsOutlineViewDataSource();
        }

        bool IFindResultsView.PushResults(IReadOnlyList<FindResultGroupViewModel> results)
        {


            // TODO: Return false if total results exceed some limit
            return true;
        }

        void IFindResultsView.Clear()
        {
            totalResultCount = 0;
            findResultGroups.Clear();
            RefreshSummaryLabel();

            resultsOutlineView.NeedsDisplay = true;
        }

        void RefreshSummaryLabel()
        {
            resultsSummaryLabel.StringValue = $"{totalResultCount} results in {findResultGroups.Count} files";
        }

        class FindResultsOutlineViewDelegate : NSOutlineViewDelegate
        {
            public override NSView GetView(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
            {
                //var view = outlineView.MakeView(tableColumn.Identifier, this);
                return base.GetView(outlineView, tableColumn, item);
            }
        }

        class FindResultsOutlineViewDataSource : NSOutlineViewDataSource
        {
            public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
            {
                return base.GetChildrenCount(outlineView, item);
            }

            public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
            {
                return base.GetChild(outlineView, childIndex, item);
            }

            public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
            {
                return base.ItemExpandable(outlineView, item);
            }
        }
    }

    public class FindResultGroupViewModel
    {
        public string FileName { get; }

        public string RelativeFilePath { get; }

        public IReadOnlyList<FindResultViewModel> Results { get; }

        // TODO: ImageId for icon?

        // TODO: Any state needed if user removes group from view? VScode lets you remove entire group and individual results

        public FindResultGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<FindResultViewModel> results)
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

    public class FindResultViewModel
    {
        public string PreviewText { get; }

        public int Line { get; }

        public int Column { get; }

        public FindResultViewModel(string previewText, int line, int column)
        {
            PreviewText = previewText ?? throw new ArgumentNullException(nameof(previewText));
            Line = line;
            Column = column;
        }
    }

    public interface IFindResultsView
    {
        public bool PushResults(IReadOnlyList<FindResultGroupViewModel> results);

        public void Clear();
    }
}
