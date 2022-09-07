using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "Notice info ScriptableObject",
        menuName = "Scriptable Object/Notice info ScriptableObject")]
    public class NoticeInfoScriptableObject : UnityEngine.ScriptableObject
    {
        public NoticePopup.NoticeInfo noticeInfo;
    }
}
