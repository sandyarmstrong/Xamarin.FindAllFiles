using System;
using System.Collections.Generic;
using Foundation;

namespace Xamarin.FindAllFiles.Mac
{
    public class MacFindResultGroupViewModel : NSObject, IFindResultGroupViewModel
    {
        public string FileName { get; }

        public string RelativeFilePath { get; }

        public IReadOnlyList<IFindResultViewModel> Results { get; }

        // TODO: ImageId for icon?

        // TODO: Any state needed if user removes group from view? VScode lets you remove entire group and individual results

        public MacFindResultGroupViewModel(IntPtr handle) : base(handle) { }

        public MacFindResultGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results)
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
}
