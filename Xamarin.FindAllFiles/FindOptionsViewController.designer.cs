// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.FindAllFiles
{
	[Register ("FindOptionsViewController")]
	partial class FindOptionsViewController
	{
		[Outlet]
		AppKit.NSTextField excludeField { get; set; }

		[Outlet]
		AppKit.NSButton findButton { get; set; }

		[Outlet]
		AppKit.NSTextField includeField { get; set; }

		[Outlet]
		AppKit.NSSearchField searchField { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (findButton != null) {
				findButton.Dispose ();
				findButton = null;
			}

			if (searchField != null) {
				searchField.Dispose ();
				searchField = null;
			}

			if (includeField != null) {
				includeField.Dispose ();
				includeField = null;
			}

			if (excludeField != null) {
				excludeField.Dispose ();
				excludeField = null;
			}
		}
	}
}
