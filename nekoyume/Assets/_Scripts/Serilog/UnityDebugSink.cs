using Serilog.Core;
using Serilog.Events;

namespace Nekoyume.Serilog
{
    internal class UnityDebugSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            UnityEngine.Debug.Log($"{logEvent.Timestamp}: {logEvent.RenderMessage()}");
        }
    }
}
