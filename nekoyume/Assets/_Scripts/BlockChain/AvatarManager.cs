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
        public const string PrivateKeyFormat = "private_key_{0}";
        
        private static PrivateKey _privateKey;
        
        public static PrivateKey GetOrCreateAvatarPrivateKey(int index)
        {
            var key = string.Format(PrivateKeyFormat, index);
            
            if (PlayerPrefs.HasKey(key))
            {
                var privateKeyHex = PlayerPrefs.GetString(key);
                _privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
                
                Debug.Log($"Avatar PrivateKey Loaded '{key}': {privateKeyHex}");
            }
            else
            {
                _privateKey = new PrivateKey();
                var privateKeyHex = ByteUtil.Hex(_privateKey.ByteArray);
                PlayerPrefs.SetString(key, privateKeyHex);
                
                Debug.Log($"Avatar PrivateKey Created '{key}': {privateKeyHex}");
            }

            return _privateKey;
        }
        
        public static Address GetOrCreateAvatarAddress(int index)
        {
            return GetOrCreateAvatarPrivateKey(index).PublicKey.ToAddress();
        }

        public static bool DeleteAvatarPrivateKey(int index)
        {
            var key = string.Format(PrivateKeyFormat, index);
            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }
            
            if (States.Instance.currentAvatarKey.Value == index)
            {
                ResetIndex();
            }

            var privateKeyHex = PlayerPrefs.GetString(key);
            PlayerPrefs.DeleteKey(key);
            
            Debug.Log($"Avatar PrivateKey Deleted '{key}': {privateKeyHex}");
            
            return true;
        }
        
        public static AvatarState SetIndex(int index)
        {
            if (!States.Instance.avatarStates.ContainsKey(index))
            {
                return null;
            }
            
            States.Instance.currentAvatarKey.Value = index;
            return States.Instance.currentAvatarState.Value;
        }

        private static void ResetIndex()
        {
            States.Instance.currentAvatarKey.Value = -1;
        }

        public static Transaction<PolymorphicAction<ActionBase>> MakeTransaction(
            IEnumerable<PolymorphicAction<ActionBase>> actions,
            BlockChain<PolymorphicAction<ActionBase>> chain
        )
        {
            return chain.MakeTransaction(AgentController.Agent.PrivateKey, actions);
        }
    }
}
