using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using UnityEngine;

namespace Nekoyume.Serilog
{
    internal class UnityDebugSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Exception != null) 
            {
                UnityEngine.Debug.LogException(logEvent.Exception);
            }
            else 
            {
                UnityEngine.Debug.Log(logEvent.RenderMessage());
            }
        }
    }
}
