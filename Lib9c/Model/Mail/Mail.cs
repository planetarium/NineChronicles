using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Model.Mail
{
    public enum MailType
    {
        Workshop = 1,
        Auction,
        System,
        Grinding,
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
                ["dailyRewardMail"] = d => new DailyRewardMail(d),
                ["monsterCollectionMail"] = d => new MonsterCollectionMail(d),
                [nameof(OrderExpirationMail)] = d => new OrderExpirationMail(d),
                [nameof(CancelOrderMail)] = d => new CancelOrderMail(d),
                [nameof(OrderBuyerMail)] = d => new OrderBuyerMail(d),
                [nameof(OrderSellerMail)] = d => new OrderSellerMail(d),
                [nameof(GrindingMail)] = d => new GrindingMail(d),
                [nameof(MaterialCraftMail)] = d => new MaterialCraftMail(d),
            };

        public Guid id;
        public bool New;
        public long blockIndex;
        public virtual MailType MailType => MailType.System;
        public long requiredBlockIndex;

        protected Mail(long blockIndex, Guid id, long requiredBlockIndex)
        {
            this.id = id;
            this.blockIndex = blockIndex;
            this.requiredBlockIndex = requiredBlockIndex;
        }

        protected Mail(Dictionary serialized) : this(
            serialized["blockIndex"].ToLong(),
            serialized["id"].ToGuid(),
            serialized["requiredBlockIndex"].ToLong()
        )
        {
        }

        public abstract void Read(IMail mail);

        protected abstract string TypeId { get; }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = id.Serialize(),
                [(Text)"typeId"] = TypeId.Serialize(),
                [(Text)"blockIndex"] = blockIndex.Serialize(),
                [(Text)"requiredBlockIndex"] = requiredBlockIndex.Serialize(),
            });

        public static Mail Deserialize(Dictionary serialized)
        {
            var typeId = serialized.GetString("typeId");
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
                Log.Error(e, "{0} was raised during deserialize: {1}", e.GetType().FullName, serialized);
                throw;
            }
        }
    }

    [Serializable]
    public class MailBox : IEnumerable<Mail>, IState
    {
        private List<Mail> _mails = new List<Mail>();

        public int Count => _mails.Count;

        public Mail this[int idx] => _mails[idx];

        public MailBox()
        {
        }

        public MailBox(List serialized) : this()
        {
            _mails = serialized.Select(
                d => Mail.Deserialize((Dictionary)d)
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

        public void CleanUp()
        {
            if (_mails.Count > 30)
            {
                _mails = _mails
                    .OrderByDescending(m => m.blockIndex)
                    .ThenBy(m => m.id)
                    .Take(30)
                    .ToList();
            }
        }

        [Obsolete("Use CleanUp")]
        public void CleanUp2()
        {
            if (_mails.Count > 30)
            {
                _mails = _mails.OrderByDescending(m => m.blockIndex).Take(30).ToList();
            }
        }

        [Obsolete("No longer in use.")]
        public void CleanUpTemp(long blockIndex)
        {
            _mails = _mails
                .Where(m => m.requiredBlockIndex >= blockIndex)
                .ToList();
        }

        public void Remove(Mail mail)
        {
            _mails.Remove(mail);
        }

        public IValue Serialize() => new List(_mails
            .OrderBy(i => i.id)
            .Select(m => m.Serialize()));
    }
}
