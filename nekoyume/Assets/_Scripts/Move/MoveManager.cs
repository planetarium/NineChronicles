using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Move
{
    public class MoveManager : MonoBehaviour
    {
        private Agent agent;
        public static MoveManager Instance { get; private set; }

        public string ServerUrl;

        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public event EventHandler<Model.Avatar> DidSleep;

        public Model.Avatar Avatar { get; private set; }
        public event EventHandler CreateAvatarRequried;

        private long? lastBlockId;

        private static string LAST_BLOCK_ID_KEY = "last_block_id";

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

            this.agent = new Agent(ServerUrl, privateKey);
            this.agent.DidReceiveAction += OnDidReceiveAction;

            Debug.Log(string.Format("User Adress: 0x{0}", agent.UserAddress.Hex()));

            if (PlayerPrefs.HasKey(LAST_BLOCK_ID_KEY))
            {
                lastBlockId = long.Parse(PlayerPrefs.GetString(LAST_BLOCK_ID_KEY));
            }
        }

        public void StartSync()
        {
            StartCoroutine(agent.FetchMove(delegate (IEnumerable<Move> moves)
            {
                if (moves.FirstOrDefault() == null)
                {
                    CreateAvatarRequried?.Invoke(this, null);
                }
                StartCoroutine(agent.Sync());
            }));
        }

        private void OnDidReceiveAction(object sender, Move move)
        {
            if (Avatar == null)
            {
                var moves = agent.Moves.Where(
                    m => m.UserAddress.SequenceEqual(agent.UserAddress)
                );
                Avatar = Model.Avatar.FromMoves(moves);
                if (Avatar != null)
                {
                    DidAvatarLoaded?.Invoke(this, Avatar);
                }
            }

            if (lastBlockId.HasValue && move.BlockId > lastBlockId)
            {
                var ctx = new Context();
                ctx.avatar = Avatar;
                Context executed = move.Execute(ctx);
                Avatar = executed.avatar;

                if (move is Sleep)
                {
                    var result = executed.result;
                    if (result["result"] == "success")
                    {
                        DidSleep?.Invoke(this, Avatar);
                    }
                }
            }

            PlayerPrefs.SetString(LAST_BLOCK_ID_KEY, move.BlockId.ToString());
        }

        public HackAndSlash HackAndSlash(string weapon = null, string armor = null, string food = null, DateTime? timestamp = null)
        {
            var details = new Dictionary<string, string>();
            if (weapon != null)
            {
                details["weapon"] = weapon;
            }
            if (armor != null)
            {
                details["armor"] = armor;
            }
            if (food != null)
            {
                details["food"] = food;
            }
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

        public Sleep Sleep(DateTime? timestamp = null)
        {
            var sleep = new Sleep
            {
                // TODO bencodex
                Details = new Dictionary<string, string>
                { }
            };
            return ProcessMove(sleep, 0, timestamp);
        }

        public FirstClass FirstClass(string class_, DateTime? timestamp = null)
        {
            var firstClass = new FirstClass
            {
                Details = new Dictionary<string, string>
                {
                    { "class", class_ }
                }
            };

            return ProcessMove(firstClass, 0, timestamp);
        }

        public CreateNovice CreateNovice(Dictionary<string, string> details, DateTime? timestamp = null)
        {
            var createNovice = new CreateNovice
            {
                Details = details
            };
            return ProcessMove(createNovice, 0, timestamp);
        }

        private T ProcessMove<T>(T move, int tax, DateTime? timestamp) where T : Move
        {
            move.Tax = tax;
            move.Timestamp = (timestamp) ?? DateTime.UtcNow;
            agent.Send(move);
            return move;
        }
    }
}
