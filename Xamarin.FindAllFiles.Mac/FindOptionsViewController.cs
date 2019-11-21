using System;

using AppKit;
using Foundation;

namespace Xamarin.FindAllFiles.Mac
{
    public partial class FindOptionsViewController : NSViewController, IFindOptionsView
    {
        // TODO: LOL
        public static IFindOptionsView CurrentFindOptionsView { get; private set; }

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
            foreach (var control in new NSControl [] {
                searchField,
                workingDirectoryField,
                includeField,
                excludeField,
                matchCaseButton,
                matchWholeWordButton,
                regexButton,
                useExcludeSettingsButton,
                findButton })
            {
                control.Target = this;
                control.Action = new ObjCRuntime.Selector("searchRequested:");
            }

            workingDirectoryField.StringValue = "/Users/sandy/xam-git/monodevelop";

            CurrentFindOptionsView = this;
        }

        readonly Lazy<IFileSearchEngine> engine = new Lazy<IFileSearchEngine>(() => {
            return new RipGrepFileSearchEngine(
                new MacFindResultFactory(),
                FindResultsViewController.CurrentFindResultsView,
                CurrentFindOptionsView);
        });

        [Export("searchRequested:")]
        private void OnSearchRequested(NSObject sender)
        {
            if (engine.Value.IsSearching)
                return;

            var viewModel = new FindOptionsViewModel
            {
                Query = searchField.StringValue,
                WorkingDirectory = workingDirectoryField.StringValue,
                Include = includeField.StringValue,
                Exclude = includeField.StringValue,
                MatchCase = matchCaseButton.State == NSCellStateValue.On, //cmd+opt+c
                MatchWholeWord = matchWholeWordButton.State == NSCellStateValue.On,//cmd+opt+w
                IsRegex = regexButton.State == NSCellStateValue.On,//cmd+opt+r
                UseExcludeSettingsAndIgnoreFiles = useExcludeSettingsButton.State == NSCellStateValue.On,
            };

            engine.Value.SearchFiles(viewModel, sender == findButton, BeginInvokeOnMainThread);
        }

        void IFindOptionsView.BeginSearch()
        {
            findButton.Enabled = false;
        }

        void IFindOptionsView.EndSearch()
        {
            findButton.Enabled = true;
        }
    }
}
