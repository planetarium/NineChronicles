using System;

namespace Nekoyume.L10n
{
    [Serializable]
    public class L10nCsvModel
    {
        public string Key { get; set; }

        public string English { get; set; }

        public string Korean { get; set; }

        public string Portuguese { get; set; }

        public string Japanese { get; set; }

        public string Spanish { get; set; }

        public string Thai { get; set; }

        public string Indonesian { get; set; }

        public string Russian { get; set; }

        public string ChineseSimplified { get; set; }

        public string ChineseTraditional { get; set; }

        public string Tagalog { get; set; }
    }
}
