namespace Lib9c.Tests.Util
{
    using System;
    using System.Linq;

    public static class CsvUtil
    {
        public static string CsvLinqWhere(string csv, Func<string, bool> where)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(where);
            return string.Join('\n', after);
        }

        public static string CsvLinqSelect(string csv, Func<string, string> select)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(select);
            return string.Join('\n', after);
        }
    }
}
