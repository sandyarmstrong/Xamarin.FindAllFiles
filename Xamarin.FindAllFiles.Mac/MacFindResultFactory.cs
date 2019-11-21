using System.Collections.Generic;

namespace Xamarin.FindAllFiles.Mac
{
    public class MacFindResultFactory : IFindResultFactory
    {
        public IFindResultGroupViewModel CreateGroupViewModel(string fileName, string relativeFilePath, IReadOnlyList<IFindResultViewModel> results)
            => new MacFindResultGroupViewModel(fileName, relativeFilePath, results);

        public IFindResultViewModel CreateResultViewModel(string previewText, int line, int startColumn, int endColumn)
            => new MacFindResultViewModel(previewText, line, startColumn, endColumn);
    }
}
