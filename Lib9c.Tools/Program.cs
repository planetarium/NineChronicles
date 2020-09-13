using System;
using Cocona;
using Lib9c.Tools.SubCommend;

namespace Lib9c.Tools
{
    [HasSubCommands(typeof(Create), Description = "Create commands")]
    class Program
    {
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        public void Info() => Console.WriteLine("Show information");
    }
}
