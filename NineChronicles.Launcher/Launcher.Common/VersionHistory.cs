using System.Collections.Generic;

namespace Launcher.Common
{
    public struct VersionHistory
    {
        public string CurrentVersion { get; set; }

        public List<VersionDescriptor> Versions { get; set; }
    }
}
