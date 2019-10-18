using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// Avatar들을 관리한다.
    /// ToDo. 추후 복수의 아바타를 오갈 수 있도록 한다.
    /// </summary>
    public static class AvatarManager
    {
        public static Address CreateAvatarAddress()
        {
            var key = new PrivateKey();
            var privateKeyHex = ByteUtil.Hex(key.ByteArray);
            Debug.Log($"Avatar PrivateKey Created. {privateKeyHex}");

            return key.PublicKey.ToAddress();
        }

        public static bool DeleteAvatarPrivateKey(int index)
        {
            if (States.Instance.CurrentAvatarKey.Value == index)
            {
                ResetIndex();
            }

            Debug.Log($"Avatar PrivateKey Deleted. {index}");
            
            return true;
        }

        public static AvatarState SetIndex(int index)
        {
            if (!States.Instance.AvatarStates.ContainsKey(index))
            {
                return null;
            }
            
            States.Instance.CurrentAvatarKey.Value = index;
            return States.Instance.CurrentAvatarState.Value;
        }

        private static void ResetIndex()
        {
            States.Instance.CurrentAvatarKey.Value = -1;
        }
    }
}
