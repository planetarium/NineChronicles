using Cysharp.Threading.Tasks;
using Nekoyume.Game.Scenes;
using UnityEngine;

namespace Nekoyume.BattleTest
{
    /// <summary>
    /// 씬 전환 테스트용으로 작성한 스크립트
    /// 씬 이동 테스트 이후 삭제예정
    /// </summary>
    public class EnterBattleTestCommander : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown("]"))
            {
                NcSceneManager.Instance.LoadScene(SceneType.Loading).Forget();
            }
        }
    }
}
