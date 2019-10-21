using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Game.Mail
{
    public enum MailType
    {
        Forge = 1,
        Auction,
        System
    }

    [Serializable]
    public abstract class Mail
    {
        public bool New;
        public long blockIndex;
        public virtual MailType MailType { get => MailType.System; }

        protected Mail(long blockIndex)
        {
            New = true;
            this.blockIndex = blockIndex;
        }

        public abstract string ToInfo();

        public abstract void Read(IMail mail);
    }

    [Serializable]
    public class MailBox : IEnumerable<Mail>
    {
        private readonly List<Mail> _mails = new List<Mail>();

        public int Count => _mails.Count;

        public Mail this[int idx] => _mails[idx];

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
    }
}
