namespace Xamarin.FindAllFiles
{
    public interface IFindResultViewModel
    {
        string PreviewText { get; }

        int Line { get; }

        int StartColumn { get; }

        int EndColumn { get; }
    }
}