using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume
{
    /// <summary>
    /// Avatar들을 관리한다.
    /// ToDo. 추후 복수의 아바타를 오갈 수 있도록 한다.
    /// </summary>
    public class AvatarManager
    {
        [Serializable]
        internal class SaveData
        {
            public Model.Avatar Avatar;
        }
        
        public const string PrivateKeyFormat = "private_key_{0}";
        public const string AvatarFileFormat = "avatar_{0}.dat";
        
        public static event EventHandler<Model.Avatar> DidAvatarLoaded;
        
        private static int _currentAvatarIndex = -1;
        private static PrivateKey _avatarPrivateKey;
        private static string _saveFilePath;
        
        private static IDisposable _disposableForEveryRender;
        
        public static List<Model.Avatar> Avatars
        {
            get
            {
                return Enumerable.Range(0, 3).Select(index => string.Format(AvatarFileFormat, index))
                    .Select(fileName => Path.Combine(Application.persistentDataPath, fileName))
                    .Select(LoadAvatar).ToList();
            }
        }

        public static AvatarState AvatarState => States.Avatar.Value;
        public static Model.Avatar Avatar => AvatarState?.avatar;
        public static BattleLog BattleLog
        {
            get => AvatarState?.battleLog;
            set
            {
                if (ReferenceEquals(AvatarState, null))
                {
                    return;
                }
                
                AvatarState.battleLog = value;   
            }
        }

        public static bool InitAvatarPrivateKeyAndFilePath(int index)
        {
            if (_currentAvatarIndex == index)
            {
                return false;
            }
            
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

            AddressBook.Avatar.Value = _avatarPrivateKey.PublicKey.ToAddress();   
            
            var fileName = string.Format(AvatarFileFormat, index);
            _saveFilePath = Path.Combine(Application.persistentDataPath, fileName);

            return true;
        }
        
        public static void InitAvatarState(int index)
        {
            if (!InitAvatarPrivateKeyAndFilePath(index))
            {
                return;
            }
            
            var avatar = LoadAvatar(_saveFilePath);
            if (ReferenceEquals(avatar, null))
            {
                throw new NullReferenceException("LoadAvatar() returns null.");
            }
            
            States.Avatar.Value = new AvatarState(avatar, AddressBook.Avatar.Value);
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
        
        /// <summary>
        /// FixMe. 모든 액션에 대한 랜더 단계에서 아바타 주소의 상태를 얻어 오고 있음.
        /// 모든 액션 생성 단계에서 각각의 변경점을 업데이트 하는 방향으로 수정해볼 필요성 있음.
        /// CreateNovice와 HackAndSlash 액션의 처리를 개선해서 테스트해 볼 예정.
        /// 시작 전에 양님에게 문의!
        /// </summary>
        public static void SubscribeAvatarUpdates()
        {
            if (AvatarState?.avatar != null)
            {
                DidAvatarLoaded?.Invoke(null, AvatarState.avatar);
            }

            if (!ReferenceEquals(_disposableForEveryRender, null))
            {
                return;
            }
            _disposableForEveryRender = ActionBase.EveryRender(AddressBook.Avatar.Value).ObserveOnMainThread().Subscribe(eval =>
            {
                var avatarState = (AvatarState) eval.OutputStates.GetState(AddressBook.Avatar.Value);
                if (!(avatarState?.avatar is null))
                {
                    PostActionRender(avatarState);
                }
            });
        }
        
        private static void PostActionRender(AvatarState avatarState)
        {
            var avatarLoaded = AvatarState == null;
            States.Avatar.Value = avatarState;
            // ToDo. 모든 랜더에 대해서 아바타를 저장하는 비용에 대해서 생각해볼 필요 있음.
            SaveStatus();
            if (avatarLoaded)
            {
                DidAvatarLoaded?.Invoke(null, AvatarState.avatar);
            }
        }
        private static void SaveStatus()
        {
            var data = new SaveData
            {
                Avatar = AvatarState.avatar,
            };
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.OpenOrCreate))
            {
                formatter.Serialize(stream, data);
            }
        }
        
        private static Model.Avatar LoadAvatar(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                var data = (SaveData) formatter.Deserialize(stream);
                return data.Avatar;
            }
        }
    }
}
