using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Nekoyume
{
    public static class StoreUtils
    {
        public const string BackupNameFormat = "{0}_{1:yyyyMMdd_HHmmss}";
        public const string BackupNamePattern = "{0}_*";

        public static void ResetStore(string storePath)
        {
            if (storePath is null)
            {
                throw new ArgumentNullException(nameof(storePath));
            }
            else if (string.IsNullOrEmpty(storePath))
            {
                throw new ArgumentException("The path is empty.", nameof(storePath));
            }

            // 백업 이름을 정한다. 예:
            //   foo -> foo_20200410_170601
            //   foo.bar -> foo_20200410_170601.bar
            string backupName = string.Format(
                CultureInfo.InvariantCulture,
                BackupNameFormat,
                Path.GetFileNameWithoutExtension(storePath),
                DateTimeOffset.UtcNow
            ) + Path.GetExtension(storePath);

            string backupPath = Path.Combine(Path.GetDirectoryName(storePath), backupName);

            if (Directory.Exists(storePath))
            {
                // 저장소가 디렉터리인 경우
                Directory.Move(storePath, backupPath);
            }
            else if (File.Exists(storePath))
            {
                // 저장소가 파일인 경우
                File.Move(storePath, backupPath);
            }
        }

        public static void ClearBackupStores(string storePath)
        {
            if (storePath is null)
            {
                throw new ArgumentNullException(nameof(storePath));
            }
            else if (string.IsNullOrEmpty(storePath))
            {
                throw new ArgumentException("The path is empty.", nameof(storePath));
            }

            string dirPath = Path.GetDirectoryName(storePath);
            string pattern = string.Format(
                CultureInfo.InvariantCulture,
                BackupNamePattern,
                Path.GetFileNameWithoutExtension(storePath)
            ) + Path.GetExtension(storePath);
            string fullStorePath = Path.GetFullPath(storePath);

            foreach (string f in Directory.GetFileSystemEntries(dirPath, pattern))
            {
                if (f is null || f.Equals(".") || f.Equals("..") || Path.GetFullPath(f).Equals(fullStorePath))
                {
                    continue;
                }

                if (Directory.Exists(f))
                {
                    Directory.Delete(f, true);
                }
                else if (File.Exists(f))
                {
                    File.Delete(f);
                }
            }
        }
    }
}
