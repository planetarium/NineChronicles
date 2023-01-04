using System;
using System.IO;
using Nekoyume.Helper;
using Sentry;
using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SentryOptionsConfiguration.cs", menuName = "Sentry/SentryOptionsConfiguration", order = 999)]
public class SentryOptionsConfiguration : ScriptableOptionsConfiguration
{
    private static readonly string CommandLineOptionsJsonPath =
        Path.Combine(Application.streamingAssetsPath, "clo.json");
    // This method gets called when you instantiated the scriptable object and added it to the configuration window
    public override void Configure(SentryUnityOptions options)
    {
        // NOTE: Native support is already initialized by the time this method runs, so Unity bugs are captured.
        // That means changes done to the 'options' here will only affect events from C# scripts.

        var _options = CommandLineOptions.Load(
            CommandLineOptionsJsonPath
        );
        options.SampleRate = 0.01f;
        options.TracesSampleRate = _options.SentrySampleRate;
        options.AddExceptionFilterForType<NullReferenceException>();
    }
}
