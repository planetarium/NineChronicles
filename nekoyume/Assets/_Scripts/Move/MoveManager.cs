using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Move
{
    internal class ByteArrayComparator : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] bs)
        {
            var h = bs.Aggregate<byte, uint>(0, (current, b) => ((current << 23) | (current >> 9)) ^ b);
            return unchecked((int) h);
        }
    }

    [Serializable]
    internal class SaveData
    {
        public Avatar Avatar;
        public long? LastBlockId;
    }

    public class MoveManager : MonoBehaviour
    {
        public static MoveManager Instance { get; private set; }

        public string ServerUrl;

        public event EventHandler<Avatar> DidAvatarLoaded;
        public event EventHandler<Avatar> DidSleep;
        public event EventHandler CreateAvatarRequired;
        public Avatar Avatar { get; private set; }

        private Agent agent;
        private long? lastBlockId;
        private const string LastBlockIdKey = "last_block_id";
        private HashSet<byte[]> _processedMoveIds;

        private string _saveFilePath;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;

            PrivateKey privateKey = null;
            var privateKeyHex = PlayerPrefs.GetString("private_key", "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = PrivateKey.Generate();
                PlayerPrefs.SetString("private_key", privateKey.Bytes.Hex());
            }
            else
            {
                privateKey = PrivateKey.FromBytes(privateKeyHex.ParseHex());
            }

            _saveFilePath = Path.Combine(Application.persistentDataPath, "avatar.dat");
            LoadStatus();

            this.agent = new Agent(ServerUrl, privateKey, lastBlockOffset: lastBlockId);
            this.agent.DidReceiveAction += OnDidReceiveMove;

            Debug.Log($"User Address: 0x{agent.UserAddress.Hex()}");

            if (PlayerPrefs.HasKey(LastBlockIdKey))
            {
                lastBlockId = long.Parse(PlayerPrefs.GetString(LastBlockIdKey));
            }

            _processedMoveIds = new HashSet<byte[]>(new ByteArrayComparator());
        }

        public void StartSync()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            StartCoroutine(agent.FetchMove(delegate(IEnumerable<MoveBase> moves)
            {
                if (moves.FirstOrDefault() == null && Avatar == null)
                {
                    CreateAvatarRequired?.Invoke(this, null);
                }

                StartCoroutine(agent.Sync());
            }));
        }

        private void OnDidReceiveMove(object sender, MoveBase move)
        {
            if (Avatar == null)
            {
                var moves = agent.Moves.Where(
                    m => m.UserAddress.SequenceEqual(agent.UserAddress)
                );
                Avatar = Avatar.FromMoves(moves);
                if (Avatar != null)
                {
                    SaveStatus();
                    DidAvatarLoaded?.Invoke(this, Avatar);
                }
            }

            if (ShouldExecute(move))
            {
                ExecuteMove(move);
            }

            if (_processedMoveIds.Contains(move.Id))
            {
                _processedMoveIds.Remove(move.Id);
            }

            PlayerPrefs.SetString(LastBlockIdKey, move.BlockId.ToString());
        }

        private bool ShouldExecute(MoveBase move)
        {
            return !_processedMoveIds.Contains(move.Id);
        }

        private void ExecuteMove(MoveBase move)
        {
            var ctx = new Context {Avatar = Avatar};
            Context executed = move.Execute(ctx);
            if (executed.Status != ContextStatus.Success) return;

            Avatar = executed.Avatar;
            SaveStatus();

            if (ShouldNotify(move))
            {
                Notify(move);
            }
        }

        private bool ShouldNotify(MoveBase move)
        {
            return !move.Confirmed || lastBlockId.GetValueOrDefault(0) < move.BlockId;
        }

        private void Notify(MoveBase move)
        {
            if (move is Sleep)
            {
                DidSleep?.Invoke(this, Avatar);
            }
        }

        public HackAndSlash HackAndSlash(Player player, int stage,
            DateTime? timestamp = null)
        {
            var details = new Dictionary<string, string>
            {
                ["hp"] = player.HP.ToString(),
                ["zone"] = stage.ToString(),
                ["dead"] = player.IsDead().ToString(),
                ["exp"] = player.EXP.ToString(),
                ["level"] = player.Level.ToString(),
                ["items"] = player.SerializeItems(),
                ["weapon"] = player.SerializeWeapon()
            };

            var has = new HackAndSlash
            {
                Details = details
            };

            return ProcessMove(has, 0, timestamp);
        }

        public IEnumerator Sync()
        {
            return agent.Sync();
        }

        public Sleep Sleep(Stats statsData , DateTime? timestamp = null)
        {
            var actions = new Action.Sleep();
            var sleep = new Sleep
            {
                Actions = new[] {actions},
                Details = actions.ToDetails()
            };
            return ProcessMove(sleep, 0, timestamp);
        }


        public CreateNovice CreateNovice(string nickName, DateTime? timestamp = null)
        {
            var actions = new Action.CreateNovice(nickName);
            var createNovice = new CreateNovice
            {
                Actions = new[] {actions},
                Details = actions.ToDetails()
            };
            return ProcessMove(createNovice, 0, timestamp);
        }

        public MoveZone MoveZone(int stage, DateTime? timestamp = null)
        {
            var action = new Action.MoveZone(stage);
            var moveZone = new MoveZone
            {
                Actions = new[] {action},
                Details = action.ToDetails()
            };
            return ProcessMove(moveZone, 0, timestamp);
        }

        private T ProcessMove<T>(T move, int tax, DateTime? timestamp) where T : MoveBase
        {
            move.Tax = tax;
            move.Timestamp = (timestamp) ?? DateTime.UtcNow;
            agent.Send(move);
            if (typeof(T).GetCustomAttribute<Preprocess>() != null)
            {
                ExecuteMove(move);
                _processedMoveIds.Add(move.Id);
            }

            return move;
        }

        private void LoadStatus()
        {
            if (!File.Exists(_saveFilePath)) return;

            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.Open))
            {
                var data = (SaveData) formatter.Deserialize(stream);
                Avatar = data.Avatar;
                lastBlockId = data.LastBlockId;
            }
        }

        private void SaveStatus()
        {
            var data = new SaveData
            {
                Avatar = Avatar,
                LastBlockId = lastBlockId
            };
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.OpenOrCreate))
            {
                formatter.Serialize(stream, data);
            }
        }

        public void UpdateItems(string items)
        {
            Avatar.Items = items;
            SaveStatus();
        }
    }
}
