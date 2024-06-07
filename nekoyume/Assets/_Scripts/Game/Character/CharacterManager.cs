using Cysharp.Threading.Tasks;
using Spine.Unity;

namespace Nekoyume.Game.Character
{
    // TODO: 에셋 로드만을 위해서라면 CharacterManager를 사용하는 것이 맞는지 생각해봐야함
    // ResourceManager에는 컨텐츠와 관련된 코드를 넣기 꺼려져서 별도의 클래스로 분리했음
    public class CharacterManager
    {
        private static class Singleton
        {
            internal static readonly CharacterManager Value = new();
        }

        public static CharacterManager Instance => Singleton.Value;

        public async UniTask LoadCharacterAssetAsync()
        {
            await ResourceManager.Instance.LoadAllAsync<SkeletonDataAsset>(ResourceManager.CharacterLabel, true);
        }
    }
}
