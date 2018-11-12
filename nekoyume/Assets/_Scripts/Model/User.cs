using Nekoyume.Move;
using Nekoyume.Network.Agent;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Address;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model
{
    public class User
    {
        public PublicKey PublicKey
        {
            get
            {
                return privateKey.PublicKey;
            }
        }
        private readonly PrivateKey privateKey;

        private readonly Agent agent;

        public byte[] Address
        {
            get
            {
                return PublicKey.ToAddress();
            }
        }

        public User(PrivateKey privateKey, Agent agent)
        {
            this.privateKey = privateKey;
            this.agent = agent;
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

        public Sleep Sleep(DateTime? timestamp = null)
        {
            var sleep = new Sleep();

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
            move.Sign(privateKey);
            agent.Send(move);

            return move;
        }

        public Avatar Avatar
        {
            get
            {
                // FIXME
                if (agent.Moves == null)
                {
                    throw new System.Exception();
                }
                var associatedMoves = agent.Moves.Where(m => m.UserAddress.SequenceEqual(Address));
                associatedMoves = associatedMoves.SkipWhile(m => !(m is CreateNovice));
                var createNovice = associatedMoves.FirstOrDefault() as CreateNovice;
                if (createNovice == null)
                {
                    return null;
                }

                var avatar = createNovice.Execute(null).Item1;

                foreach (var move in associatedMoves.Skip(1))
                {
                    avatar = move.Execute(avatar).Item1;
                }

                return avatar;
            }
        }
    }
}
