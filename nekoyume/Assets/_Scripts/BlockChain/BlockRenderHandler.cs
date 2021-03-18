using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderer;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    public class BlockRenderHandler
    {
        private static class Singleton
        {
            internal static readonly BlockRenderHandler Value = new BlockRenderHandler();
        }

        public static BlockRenderHandler Instance => Singleton.Value;

        private BlockRenderer _blockRenderer;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private List<Guid> _mailRecords = new List<Guid>();

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            Stop();
            _blockRenderer = blockRenderer;

            Reorg();
            UpdateWeeklyArenaState();
            UpdateCurrentAvatarState();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void Reorg()
        {
            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.Notification.Push(Model.Mail.MailType.System, msg);
                })
                .AddTo(_disposables);
        }

        private void UpdateWeeklyArenaState()
        {
            _blockRenderer.EveryBlock()
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    var doNothing = true;
                    var agent = Game.Game.instance.Agent;
                    var gameConfigState = States.Instance.GameConfigState;
                    var challengeCountResetBlockIndex = States.Instance.WeeklyArenaState.ResetIndex;
                    var currentBlockIndex = agent.BlockIndex;
                    if (currentBlockIndex % gameConfigState.WeeklyArenaInterval == 0 &&
                        currentBlockIndex >= gameConfigState.WeeklyArenaInterval)
                    {
                        doNothing = false;
                    }

                    if (currentBlockIndex - challengeCountResetBlockIndex >=
                        gameConfigState.DailyArenaInterval)
                    {
                        doNothing = false;
                    }

                    if (doNothing)
                    {
                        return;
                    }

                    var weeklyArenaIndex =
                        (int) currentBlockIndex / gameConfigState.WeeklyArenaInterval;
                    var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(weeklyArenaIndex);
                    var weeklyArenaState =
                        new WeeklyArenaState(
                            (Bencodex.Types.Dictionary) agent.GetState(weeklyArenaAddress));
                    States.Instance.SetWeeklyArenaState(weeklyArenaState);
                })
                .AddTo(_disposables);
        }

        private void UpdateCurrentAvatarState()
        {
            _blockRenderer.EveryBlock()
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    IAgent agent = Game.Game.instance.Agent;
                    ShopState shop = States.Instance.ShopState;
                    AvatarState avatar = States.Instance.CurrentAvatarState;
                    List<ShopItem> shopItems = new List<ShopItem>();
                    bool replace = false;

                    if (!(avatar is null))
                    {
                        shopItems = shop.Products.Values.Where(r =>
                            r.SellerAvatarAddress == avatar.address && r.ExpiredBlockIndex != 0 &&
                            r.ExpiredBlockIndex <= agent.BlockIndex).ToList();
                    }

                    if (!shopItems.Any())
                    {
                        return;
                    }

                    var avatarState = new AvatarState((Dictionary)agent.GetState(avatar.address));
                    List<SellCancelMail> sellCancelMails = avatarState.mailBox.OfType<SellCancelMail>().ToList();
                    int prevCount = _mailRecords.Count;
                    foreach (var mail in shopItems.Select(shopItem => sellCancelMails.FirstOrDefault(m =>
                        ((SellCancellation.Result) m.attachment).shopItem.ProductId == shopItem.ProductId))
                        .Where(mail => !(mail is null) && !_mailRecords.Contains(mail.id)))
                    {
                        _mailRecords.Add(mail.id);
                        LocalLayerModifier.AddNewAttachmentMail(avatar.address, mail.id);
                    }

                    if (_mailRecords.Count > prevCount)
                    {
                        States.Instance.AddOrReplaceAvatarState(avatarState, States.Instance.CurrentAvatarKey);
                    }
                })
                .AddTo(_disposables);
        }
    }
}
