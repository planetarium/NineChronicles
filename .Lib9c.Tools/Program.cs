using System;
using Cocona;
using Lib9c.Tools.SubCommand;

namespace Lib9c.Tools
{
    using Action = Lib9c.Tools.SubCommand.Action;

    [HasSubCommands(typeof(Account), Description = "Query about accounts.")]
    [HasSubCommands(typeof(Market), Description = "Query about market.")]
    [HasSubCommands(typeof(State), Description = "Manage states.")]
    [HasSubCommands(typeof(Tx), Description = "Manage transactions.")]
    [HasSubCommands(typeof(Action), Description = "Get metadata of actions.")]
    class Program
    {
        static void Main(string[] args)
        {
            Console.Error.WriteLine(
                "`lib9c.Tools` is deprecated. " +
                "Please use `NineChronicles.Headless.Executable [command]` instead.");
            CoconaLiteApp.Run<Program>(args);
        }

        public void Help()
        {
            Main(new[] { "--help" });
        }
    }
}
