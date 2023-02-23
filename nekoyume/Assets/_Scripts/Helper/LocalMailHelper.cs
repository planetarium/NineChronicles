using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Mail;
using Nekoyume.State;

namespace Nekoyume.Helper
{
    using UniRx;

    public class LocalMailHelper
    {
        private LocalMailHelper()
        {
            _localMailDictionary = new Dictionary<Address, List<Mail>>();
            _disposables = new List<IDisposable>();
        }

        private readonly Dictionary<Address, List<Mail>> _localMailDictionary;
        private readonly List<IDisposable> _disposables;
        private static LocalMailHelper _instance;

        public static LocalMailHelper Instance => _instance ??= new LocalMailHelper();

        public void Initialize()
        {
            if (_disposables.Any())
            {
                return;
            }

            ReactiveAvatarState.MailBox.Subscribe(mailBox =>
            {
                if (_localMailDictionary.TryGetValue(States.Instance.CurrentAvatarState.address,
                        out var mails))
                {
                    foreach (var mail in mails.Where(mail => !mailBox.Contains(mail)))
                    {
                        // It works like `States.Instance.CurrentAvatarState.mailBox.Add(...)`
                        mailBox.Add(mail);
                    }
                }
            }).AddTo(_disposables);
        }

        public void Add(Address address, Mail mail, bool notifyUpdate = false)
        {
            if (!_localMailDictionary.ContainsKey(address))
            {
                _localMailDictionary.Add(address, new List<Mail>());
            }

            _localMailDictionary[address].Add(mail);
            if (notifyUpdate)
            {
                ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
            }
        }

        public void CleanupAndDispose(Address address)
        {
            _localMailDictionary[address].Clear();
            _disposables.DisposeAllAndClear();
        }

        public bool TryGetAllLocalMail(Address address, out List<Mail> localMails) =>
            _localMailDictionary.TryGetValue(address, out localMails);
    }
}
