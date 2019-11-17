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
            resultsOutlineView.Delegate = new FindResultsOutlineViewDelegate(this);
            resultsOutlineView.DataSource = new FindResultsOutlineViewDataSource(this);

            // TODO: Delete this
            ((IFindResultsView)this).PushResults(
                new List<FindResultGroupViewModel> {
                    new FindResultGroupViewModel("blah.txt", "", new List<FindResultViewModel> {
                        new FindResultViewModel("some preview text", 0, 0),
                        new FindResultViewModel("some other preview text", 0, 0),
                    }),
                    new FindResultGroupViewModel("stuff.cs", "", new List<FindResultViewModel> {
                        new FindResultViewModel("somebody loves me", 0, 0),
                        new FindResultViewModel("I would like some hot dogs please", 0, 0),
                    }),
                });
        }

        bool IFindResultsView.PushResults(IReadOnlyList<FindResultGroupViewModel> results)
        {
            foreach (var resultGroup in results)
            {
                findResultGroups.Add(resultGroup);
                totalResultCount += resultGroup.Results.Count;
            }

            RefreshSummaryLabel();

            // TODO: Is this problematic with long lists?
            resultsOutlineView.ReloadData();
            resultsOutlineView.ExpandItem(null, true);

            // TODO: Return false if total results exceed some limit
            return true;
        }

        void IFindResultsView.Clear()
        {
            totalResultCount = 0;
            findResultGroups.Clear();
            RefreshSummaryLabel();

            resultsOutlineView.ReloadData();
        }

        void RefreshSummaryLabel()
        {
            if (findResultGroups.Count > 0)
            {
                var fileOrFiles = findResultGroups.Count > 1 ? "files" : "file";
                resultsSummaryLabel.StringValue = $"{totalResultCount} results in {findResultGroups.Count} {fileOrFiles}";
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
                else if (item is GroupWrapper groupWrapper)
                    view.TextField.StringValue = groupWrapper.ViewModel.FileName;
                else if (item is ResultWrapper resultWrapper)
                    view.TextField.StringValue = resultWrapper.ViewModel.PreviewText;

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
                else if (item is GroupWrapper groupWrapper)
                    return groupWrapper.ViewModel.Results.Count;

                return 0;
            }

            public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
            {
                var index = (int)childIndex;

                // TODO: OMG STOP RECREATING
                // TODO: Using immutable here would help prevent sync issues (but realistically we shouldn't be creating stuff here at all)
                if (item == null)// && index >= 0 && index < viewController.findResultGroups.Count)
                    return new GroupWrapper { ViewModel = viewController.findResultGroups[index] };
                else if (item is GroupWrapper groupWrapper)
                    return new ResultWrapper { ViewModel = groupWrapper.ViewModel.Results[index] };

                return null;
            }

            public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
            {
                return item is GroupWrapper;
            }
        }
    }

    // TODO: Should these implement the interface instead? And engine generates them directly via factory? Then no dupes.
    class GroupWrapper : NSObject
    {
        public FindResultGroupViewModel ViewModel { get; set; }
    }

    class ResultWrapper : NSObject
    {
        public FindResultViewModel ViewModel { get; set; }
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
        bool PushResults(IReadOnlyList<FindResultGroupViewModel> results);

        void Clear();
    }
}
