using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.FindAllFiles
{
    public interface IFileSearchEngine
    {
        bool IsSearching { get; }

        Task SearchFilesAsync(
            FindOptionsViewModel options,
            bool forceSearch,
            CancellationToken cancellationToken = default);
    }
}
