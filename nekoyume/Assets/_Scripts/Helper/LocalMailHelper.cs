using System;
using System.Collections.Generic;
using Libplanet.Crypto;
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
            _localMailBox = new ReactiveProperty<MailBox>(States.Instance.CurrentAvatarState?.mailBox);
            Event.OnUpdateAddresses.AsObservable()
                .Subscribe(_ =>
                {
                    if (States.Instance.CurrentAvatarState?.address is { } addr)
                    {
                        Initialize(addr);
                    }
                });
            ReactiveAvatarState.MailBox.Subscribe(UpdateLocalMailBox);
        }

        private readonly Dictionary<Address, List<Mail>> _localMailDictionary;
        private readonly List<IDisposable> _disposables;
        private readonly ReactiveProperty<MailBox> _localMailBox;
        private static LocalMailHelper _instance;
        private MailBox _originalMailBox;

        public static LocalMailHelper Instance => _instance ??= new LocalMailHelper();
        public IObservable<MailBox> ObservableMailBox => _localMailBox.ObserveOnMainThread();
        public MailBox MailBox => _localMailBox.Value;

        public void Add(Address address, Mail mail)
        {
            Initialize(address);

            _localMailDictionary[address].Add(mail);
            UpdateLocalMailBox(_originalMailBox);
        }

        private void Initialize(Address address)
        {
            if (_localMailDictionary.ContainsKey(address))
            {
                return;
            }

            _localMailDictionary.Add(address, new List<Mail>());
            Event.OnUpdateAddresses.AsObservable()
                .First()
                .Subscribe(_ => CleanupAndDispose())
                .AddTo(_disposables);
        }

        private void CleanupAndDispose()
        {
            _localMailDictionary.Clear();
            _disposables.DisposeAllAndClear();
        }

        private void UpdateLocalMailBox(MailBox mailBox)
        {
            _originalMailBox = mailBox;
            var newMailBox = new MailBox();

            if (_originalMailBox is not null)
            {
                foreach (var mail in _originalMailBox)
                {
                    newMailBox.Add(mail);
                }
            }

            if (_localMailDictionary.TryGetValue(States.Instance.CurrentAvatarState?.address ?? new Address(),
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
