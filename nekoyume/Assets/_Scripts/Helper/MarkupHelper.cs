using System.Collections.Generic;

namespace Nekoyume.Helper
{
    public class MarkupHelper
    {
        private static readonly Dictionary<string, string> Markups = new Dictionary<string, string>
        {
            {"[comma]", ","},
            {"[newline]", "\n"}
        };
        
        public static void ReplaceMarkups(ref string value)
        {
            foreach (var markup in Markups)
            {
                if (value.Contains(markup.Key))
                {
                    value = value.Replace(markup.Key, markup.Value);
                }
            }
        }
    }
}
