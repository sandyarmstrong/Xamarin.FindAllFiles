using System.Collections.Generic;

namespace Xamarin.FindAllFiles
{
    public interface IFindResultFactory
    {
        IFindResultGroupViewModel CreateGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results);

        IFindResultViewModel CreateResultViewModel(string previewText, int line, int startColumn, int endColumn);
    }
}