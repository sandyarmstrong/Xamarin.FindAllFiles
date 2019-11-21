using System;

namespace Xamarin.FindAllFiles
{
    public interface IFileSearchEngine
    {
        bool IsSearching { get; }

        void SearchFiles(FindOptionsViewModel options, bool forceSearch, Action<Action> invokeOnMainThread);
    }
}
