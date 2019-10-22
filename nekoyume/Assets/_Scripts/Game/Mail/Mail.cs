using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Game.Mail
{
    public enum MailType
    {
        Forge = 1,
        Auction,
        System
    }

    [Serializable]
    public abstract class Mail : IState
    {
        private static readonly Dictionary<string, Func<Dictionary, Mail>> Deserializers =
            new Dictionary<string, Func<Dictionary, Mail>>
            {
                ["buyerMail"] = d => new BuyerMail(d),
                ["combinationMail"] = d => new CombinationMail(d),
                ["sellCancel"] = d => new SellCancelMail(d),
                ["seller"] = d => new SellerMail(d),
                ["itemEnhance"] = d => new ItemEnhanceMail(d),
            };

        public bool New;
        public long blockIndex;
        public virtual MailType MailType => MailType.System;

        protected Mail(long blockIndex)
        {
            New = true;
            this.blockIndex = blockIndex;
        }

        protected Mail(Bencodex.Types.Dictionary serialized)
            : this((long) ((Integer) serialized[(Bencodex.Types.Text) "blockIndex"]).Value)
        {
            New = ((Bencodex.Types.Boolean) serialized[(Bencodex.Types.Text) "new"]).Value;
        }

        public abstract string ToInfo();

        public abstract void Read(IMail mail);

        protected abstract string TypeId { get; }

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Bencodex.Types.Text) "typeId"] = (Bencodex.Types.Text) TypeId,
                [(Bencodex.Types.Text) "new"] = new Bencodex.Types.Boolean(New),
                [(Bencodex.Types.Text) "blockIndex"] = (Integer) blockIndex,
            });

        public static Mail Deserialize(Bencodex.Types.Dictionary serialized)
        {
            string typeId = ((Text) serialized[(Text) "typeId"]).Value;
            Func<Dictionary, Mail> deserializer;
            try
            {
                deserializer = Deserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    Deserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("{0} was raised during deserialize: {1}", e.GetType().FullName, serialized);
                throw;
            }
        }
    }

    [Serializable]
    public class MailBox : IEnumerable<Mail>, IState
    {
        private readonly List<Mail> _mails = new List<Mail>();

        public int Count => _mails.Count;

        public Mail this[int idx] => _mails[idx];

        public MailBox()
        {
        }

        public MailBox(Bencodex.Types.List serialized) : this()
        {
            _mails = serialized.Select(
                d => Mail.Deserialize((Bencodex.Types.Dictionary) d)
            ).ToList();
        }

        public IEnumerator<Mail> GetEnumerator()
        {
            return _mails.OrderByDescending(i => i.blockIndex).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Mail mail)
        {
            _mails.Add(mail);
        }

        public IValue Serialize() =>
            new Bencodex.Types.List(this.Select(m => m.Serialize()));
    }
}
