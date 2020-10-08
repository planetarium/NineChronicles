using Cocona;
using Lib9c.Tools.SubCommand;

namespace Lib9c.Tools
{
    [HasSubCommands(typeof(Genesis), Description = "Manage genesis block.")]
    class Program
    {
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        public void Help()
        {
            Main(new[] { "--help" });
        }
    }
}
