using System;
using ShellProgressBar;

namespace Updater
{
    public class DownloadProgress : IProgress<(long Downloaded, long Total)>, IDisposable
    {
        private const long Denominator = 1024L;

        private string _downloadUrl;

        private ProgressBar _progressBar;

        public DownloadProgress(string downloadUrl) 
        {
            _downloadUrl = downloadUrl;
            _progressBar = null;
        }

        private ProgressBar GetProgressBar(long totalBytes)
        {
            if (_progressBar is null)
            {
                _progressBar = new ProgressBar(
                    (int)(totalBytes / Denominator),
                    $"Downloading from {_downloadUrl}...",
                    new ProgressBarOptions
                    {
                        ProgressCharacter = '-',
                        BackgroundCharacter = '-',
                        CollapseWhenFinished = true,
                        ProgressBarOnBottom = true,
                        DisplayTimeInRealTime = false,
                    }
                );
            }

            return _progressBar;
        }

        public void Report((long Downloaded, long Total) value)
        {
            ProgressBar progressBar = GetProgressBar(value.Total);
            progressBar.Tick((int)(value.Downloaded / 1024L));
            progressBar.Message = $"Downloading from {_downloadUrl}... " +
                $"({(int)(value.Downloaded / 1024L)}KB/{(int)(value.Total / 1024L)}KB)";
        }

        public void Dispose()
        {
            if (_progressBar is ProgressBar)
            {
                _progressBar.Dispose();
            }
        }
    }
}
