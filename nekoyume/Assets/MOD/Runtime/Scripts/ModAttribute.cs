using System;

namespace NineChronicles.MOD
{
    public class ModAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public Version Version { get; set; }

        public ModAttribute(string name, string description, string version)
        {
            Name = name;
            Description = description;
            Version = new Version(version);
        }
    }
}
