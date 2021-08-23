using Cocona;
using Lib9c.Tools.SubCommand;

namespace Lib9c.Tools
{
    [HasSubCommands(typeof(Account), Description = "Manage accounts.")]
    [HasSubCommands(typeof(Genesis), Description = "Manage genesis block.")]
    [HasSubCommands(typeof(Tx), Description = "Manage transactions.")]
    [HasSubCommands(typeof(Store), Description = "Manage store.")]
    class Program
    {
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        public void Help()
        {
            Main(new[] { "--help" });
        }
    }
}
