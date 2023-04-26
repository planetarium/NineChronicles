using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game;
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
            _localMailBox = new ReactiveProperty<MailBox>();
        }

        private readonly Dictionary<Address, List<Mail>> _localMailDictionary;
        private readonly List<IDisposable> _disposables;
        private readonly ReactiveProperty<MailBox> _localMailBox;
        private static LocalMailHelper _instance;

        public static LocalMailHelper Instance => _instance ??= new LocalMailHelper();
        public IObservable<MailBox> ObservableMailBox => _localMailBox.ObserveOnMainThread();
        public MailBox MailBox => _localMailBox.Value ?? States.Instance.CurrentAvatarState?.mailBox;

        public void Initialize(Address address)
        {
            if (_localMailDictionary.ContainsKey(address))
            {
                return;
            }

            ReactiveAvatarState.MailBox.Subscribe(UpdateLocalMailBox).AddTo(_disposables);
            Event.OnUpdateAddresses.AsObservable().First().Subscribe(_ => CleanupAndDispose());
        }

        public void Add(Address address, Mail mail)
        {
            if (!_localMailDictionary.ContainsKey(address))
            {
                _localMailDictionary.Add(address, new List<Mail>());
            }

            _localMailDictionary[address].Add(mail);
            UpdateLocalMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        private void CleanupAndDispose()
        {
            _localMailDictionary.Clear();
            _disposables.DisposeAllAndClear();
        }

        private void UpdateLocalMailBox(MailBox mailBox)
        {
            var newMailBox = new MailBox();
            foreach (var mail in mailBox)
            {
                newMailBox.Add(mail);
            }

            if (_localMailDictionary.TryGetValue(States.Instance.CurrentAvatarState.address,
                    out var localMailList))
            {
                foreach (var mail in localMailList)
                {
                    newMailBox.Add(mail);
                }
            }

            _localMailBox.SetValueAndForceNotify(newMailBox);
        }
    }
}
