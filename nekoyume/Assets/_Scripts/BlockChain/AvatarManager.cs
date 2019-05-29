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
    public class AvatarManager
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
        
//        /// <summary>
//        /// FixMe. 모든 액션에 대한 랜더 단계에서 아바타 주소의 상태를 얻어 오고 있음.
//        /// 모든 액션 생성 단계에서 각각의 변경점을 업데이트 하는 방향으로 수정해볼 필요성 있음.
//        /// CreateNovice와 HackAndSlash 액션의 처리를 개선해서 테스트해 볼 예정.
//        /// 시작 전에 양님에게 문의!
//        /// </summary>
//        public static void SubscribeAvatarUpdates()
//        {
//            if (States.CurrentAvatarState.Value != null)
//            {
//                DidAvatarStateLoaded(States.CurrentAvatarState.Value);
//            }
//
//            if (!ReferenceEquals(_disposableForEveryRender, null))
//            {
//                return;
//            }
//            _disposableForEveryRender = ActionBase.EveryRender(States.CurrentAvatarState.Value.address).ObserveOnMainThread().Subscribe(eval =>
//            {
//                var avatarState = (AvatarState) eval.OutputStates.GetState(States.CurrentAvatarState.Value.address);
//                if (avatarState is null)
//                {
//                    return;
//                }
//                PostActionRender(avatarState);
//            });
//        }
//        
//        private static void PostActionRender(AvatarState avatarState)
//        {
//            var avatarLoaded = States.CurrentAvatarState.Value == null;
//            States.CurrentAvatarState.Value = avatarState;
//            if (avatarLoaded)
//            {
//                DidAvatarStateLoaded(States.CurrentAvatarState.Value);
//            }
//        }
    }
}
