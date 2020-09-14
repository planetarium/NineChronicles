using System;
using Cocona;
using Lib9c.Tools.SubCommend;

namespace Lib9c.Tools
{
    // FIXME more detailed description need 
    [HasSubCommands(typeof(Create), Description = "Create commands")]
    class Program
    {
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);
    }
}
