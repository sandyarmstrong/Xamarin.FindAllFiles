using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

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

        public override void ViewDidLoad()
        {
            // TODO: no events plz
            // TODO: Probably don't need a button anyway, can do same behavior as vscode. ViewModel shouldn't care either way
            findButton.Activated += FindButton_Activated;
        }

        private void FindButton_Activated(object sender, EventArgs e)
        {
            Console.WriteLine("clicked");
        }

        #endregion

        //strongly typed view accessor
        public new FindOptionsView View
        {
            get
            {
                return (FindOptionsView)base.View;
            }
        }
    }
}
