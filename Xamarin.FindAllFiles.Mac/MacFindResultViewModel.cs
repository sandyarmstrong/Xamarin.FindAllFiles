using System;
using Foundation;

namespace Xamarin.FindAllFiles.Mac
{
    public class MacFindResultViewModel : NSObject, IFindResultViewModel
    {
        public string PreviewText { get; }

        public int Line { get; }

        public int StartColumn { get; }

        public int EndColumn { get; }

        public MacFindResultViewModel(IntPtr handle) : base(handle) { }

        public MacFindResultViewModel(string previewText, int line, int startColumn, int endColumn)
        {
            PreviewText = previewText ?? throw new ArgumentNullException(nameof(previewText));
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }
    }
}
