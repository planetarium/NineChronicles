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
    }
}
