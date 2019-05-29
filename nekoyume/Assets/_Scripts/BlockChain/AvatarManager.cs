using System;
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
        public const string AvatarFileFormat = "avatar_{0}.dat";
        
//        public static event Action<AvatarState> DidAvatarStateLoaded = delegate { };
        
        private static int _currentAvatarIndex = -1;
        private static PrivateKey _avatarPrivateKey;
        private static string _saveFilePath;
        
        private static IDisposable _disposableForEveryRender;
        
        public static Address GetOrCreateAvatarAddress(int index)
        {
            _currentAvatarIndex = index;

            var key = string.Format(PrivateKeyFormat, index);
            var privateKeyHex = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                _avatarPrivateKey = new PrivateKey();
                PlayerPrefs.SetString(key, ByteUtil.Hex(_avatarPrivateKey.ByteArray));
            }
            else
            {
                _avatarPrivateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }

            return _avatarPrivateKey.PublicKey.ToAddress();
        }
        
        public static AvatarState InitAvatarState(int index)
        {
            if (!States.AvatarStates.ContainsKey(index))
            {
                return null;
            }
            
            States.CurrentAvatarState.Value = States.AvatarStates[index];
            return States.CurrentAvatarState.Value;
        }

        public static Transaction<PolymorphicAction<ActionBase>> MakeTransaction(
            IEnumerable<PolymorphicAction<ActionBase>> actions,
            BlockChain<PolymorphicAction<ActionBase>> chain
        )
        {
            return Transaction<PolymorphicAction<ActionBase>>.Create(
                chain.GetNonce(_avatarPrivateKey.PublicKey.ToAddress()),
                _avatarPrivateKey,
                actions,
                timestamp: DateTime.UtcNow
            );
        }
    }
}
