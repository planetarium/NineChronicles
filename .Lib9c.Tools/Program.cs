using Cocona;
using Lib9c.Tools.SubCommand;

namespace Lib9c.Tools
{
    [HasSubCommands(typeof(Account), Description = "Query about accounts.")]
    [HasSubCommands(typeof(Genesis), Description = "Manage genesis block.")]
    [HasSubCommands(typeof(Market), Description = "Query about market.")]
    [HasSubCommands(typeof(Tx), Description = "Manage transactions.")]
    class Program
    {
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        public void Help()
        {
            Main(new[] { "--help" });
        }
    }
}
