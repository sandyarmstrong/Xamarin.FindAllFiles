using System.Collections.Generic;

namespace Xamarin.FindAllFiles
{
    public interface IFindResultGroupViewModel
    {
        string FileName { get; }

        string RelativeFilePath { get; }

        IReadOnlyList<IFindResultViewModel> Results { get; }
    }
}