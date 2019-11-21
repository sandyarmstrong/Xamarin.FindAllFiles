using System;

namespace Xamarin.FindAllFiles
{
    public class FindOptionsViewModel : IEquatable<FindOptionsViewModel>
    {
        public bool MatchCase { get; set; }

        public bool MatchWholeWord { get; set; }

        public bool IsRegex { get; set; }

        // TODO: May need to split this into global vs local like vscode
        public bool UseExcludeSettingsAndIgnoreFiles { get; set; }

        public string Query { get; set; }

        public string WorkingDirectory { get; set; }

        public string Include { get; set; }

        public string Exclude { get; set; }

        public bool Equals(FindOptionsViewModel other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is FindOptionsViewModel other && this == other;
        }

        public override int GetHashCode()
        {
            var hash = 23;
            hash = hash * 31 + MatchCase.GetHashCode();
            hash = hash * 31 + MatchWholeWord.GetHashCode();
            hash = hash * 31 + IsRegex.GetHashCode();
            hash = hash * 31 + UseExcludeSettingsAndIgnoreFiles.GetHashCode();
            if (Query != null)
                hash = hash * 31 + Query.GetHashCode();
            if (WorkingDirectory != null)
                hash = hash * 31 + WorkingDirectory.GetHashCode();
            if (Include != null)
                hash = hash * 31 + Include.GetHashCode();
            if (Exclude != null)
                hash = hash * 31 + Exclude.GetHashCode();
            return hash;
        }

        public static bool operator ==(FindOptionsViewModel left, FindOptionsViewModel right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.MatchCase == right.MatchCase &&
                left.MatchWholeWord == right.MatchWholeWord &&
                left.IsRegex == right.IsRegex &&
                left.UseExcludeSettingsAndIgnoreFiles == right.UseExcludeSettingsAndIgnoreFiles &&
                left.Query == right.Query &&
                left.WorkingDirectory == right.WorkingDirectory &&
                left.Include == right.Include &&
                left.Exclude == right.Exclude;
        }

        public static bool operator !=(FindOptionsViewModel left, FindOptionsViewModel right)
            => !(left == right);
    }
}
