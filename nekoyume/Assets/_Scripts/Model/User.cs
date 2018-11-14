using Nekoyume.Move;
using Nekoyume.Network.Agent;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Address;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model
{
    public class User
    {
        private readonly Agent agent;

        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public event EventHandler<Model.Avatar> DidSleep;

        public Avatar Avatar { get; private set; }

        public User(Agent agent)
        {
            this.agent = agent;
            this.agent.DidReceiveAction += OnDidReceiveAction;
        }

        private void OnDidReceiveAction(object sender, Move.Move move)
        {
            if (Avatar == null)
            {
                var moves = agent.Moves.Where(
                    m => m.UserAddress.SequenceEqual(agent.UserAddress)
                );
                Avatar = Avatar.FromMoves(moves);
                if (Avatar != null)
                {
                    DidAvatarLoaded?.Invoke(this, Avatar);
                }
            }
            var ctx = new Context();
            ctx.avatar = Avatar;
            var executed = move.Execute(ctx);
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
            var has = new HackAndSlash(details);

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

        private T ProcessMove<T>(T move, int tax, DateTime? timestamp) where T : Move.Move
        {
            move.Tax = tax;
            move.Timestamp = (timestamp) ?? DateTime.UtcNow;
            agent.Send(move);
            return move;
        }
    }
}
