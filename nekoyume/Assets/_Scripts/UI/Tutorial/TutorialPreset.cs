using System;

namespace Nekoyume.UI
{
    [Serializable]
    public class TutorialPreset
    {
        public Preset[] preset;
    }

    [Serializable]
    public class Preset
    {
        public int id;
        public string content;
        public bool isExistFadeInBackground;
        public bool isEnableMask;
        public bool isSkipArrowAnimation;
        public int commaId;
    }
}
