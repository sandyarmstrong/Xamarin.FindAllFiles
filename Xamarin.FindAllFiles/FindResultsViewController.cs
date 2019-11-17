using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Xamarin.FindAllFiles
{
    public partial class FindResultsViewController : AppKit.NSViewController
    {
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
}
