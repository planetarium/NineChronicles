using System;
using System.Collections.Generic;

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

        private ActionUnrenderHandler() : base()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
