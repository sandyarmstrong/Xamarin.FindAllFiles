using System.Collections.Generic;

namespace Xamarin.RipGrep
{
    public class FindOptions
    {
        public string Query { get; set; }

        public string WorkingDirectory { get; set; }

        public bool CaseSensitive { get; set; }

        public bool IgnoreCase { get; set; }

        public bool NoIgnore { get; set; }

        public bool NoIgnoreParent { get; set; }

        public bool NoConfig { get; set; }

        public long? MaxFileSize { get; set; }

        public bool CrlfSupport { get; set; }

        public IReadOnlyList<string> ToArgumentList()
        {
            var args = new List<string>
            {
                $"\"{Query}\"",
                "--json"
            };

            if (NoConfig)
                args.Add("--no-config");

            if (CrlfSupport)
                args.Add("--crlf");

            if (MaxFileSize.HasValue)
            {
                args.Add("--max-filesize");
                args.Add(MaxFileSize.Value.ToString());
            }

            if (CaseSensitive)
                args.Add("--case-sensitive");

            if (IgnoreCase)
                args.Add("--ignore-case");

            if (NoIgnore)
                args.Add("--no-ignore");

            if (NoIgnoreParent)
                args.Add("--no-ignore-parent");

            return args;
        }

        public override string ToString()
            => string.Join(' ', ToArgumentList());
    }
}
