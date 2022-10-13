using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Mail;
using Nekoyume.State;

namespace Nekoyume.Helper
{
    using UniRx;

    public static class LocalMailHelper
    {
        private static readonly Dictionary<Address, List<Mail>> LocalMailDictionary = new();
        private static readonly List<IDisposable> Disposables = new();

        public static void Initialize()
        {
            ReactiveAvatarState.MailBox.Subscribe(mailBox =>
            {
                if (LocalMailDictionary.TryGetValue(States.Instance.CurrentAvatarState.address,
                        out var mails))
                {
                    foreach (var mail in mails.Where(mail => !mailBox.Contains(mail)))
                    {
                        // It works like `States.Instance.CurrentAvatarState.mailBox.Add(...)`
                        mailBox.Add(mail);
                    }
                }
            }).AddTo(Disposables);
        }

        public static void Add(Address address, Mail mail, bool notifyUpdate = false)
        {
            if (!LocalMailDictionary.ContainsKey(address))
            {
                LocalMailDictionary.Add(address, new List<Mail>());
            }

            LocalMailDictionary[address].Add(mail);
            if (notifyUpdate)
            {
                ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
            }
        }

        public static void CleanupDisposables()
        {
            Disposables.DisposeAllAndClear();
        }
    }
}
