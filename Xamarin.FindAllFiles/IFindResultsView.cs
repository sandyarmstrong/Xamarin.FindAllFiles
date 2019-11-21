

using System.Collections.Generic;
using System;

namespace Xamarin.FindAllFiles
{
    public interface IFindResultsView
    {
        bool PushResults(IReadOnlyList<IFindResultGroupViewModel> results);

        void BeginSearch();

        void EndSearch(TimeSpan totalSearchTime, bool canceled = false);

        void Clear();
    }
}