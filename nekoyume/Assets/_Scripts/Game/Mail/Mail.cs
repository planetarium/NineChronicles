using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public abstract class Mail
    {
        public bool New;
        public ActionResult attachment;
        public long blockIndex;

        protected Mail(ActionResult actionResult, long blockIndex)
        {
            New = true;
            attachment = actionResult;
            this.blockIndex = blockIndex;
        }

        public abstract string ToInfo();
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
