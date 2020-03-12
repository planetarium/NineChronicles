using System;
using System.Collections.Generic;
using Nekoyume.Action;

namespace Nekoyume.BlockChain
{
    public class ActionUnrenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionUnrenderHandler Value = new ActionUnrenderHandler();
        }

        public static readonly ActionUnrenderHandler Instance = Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Start(ActionRenderer renderer)
        {
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
