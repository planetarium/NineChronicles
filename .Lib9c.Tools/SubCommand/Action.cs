using System;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Cocona;
using Cocona.Help;
using Libplanet.Action;
using Nekoyume.Action;

namespace Lib9c.Tools.SubCommand
{
    public class Action
    {
        [Obsolete("This function is deprecated. Please use `NineChronicles.Headless.Executable action list` command instead.")]
        [Command(Description = "Lists all actions' type ids.")]
        public void List(
            [Option(
                Description = "If true, filter obsoleted actions since the --block-index option."
            )] bool excludeObsolete = false,
            [Option(
                Description = "The current block index to filter obsoleted actions."
            )] long blockIndex = 0
        )
        {
            Type baseType = typeof(Nekoyume.Action.ActionBase);

            bool IsTarget(Type type)
            {
                return baseType.IsAssignableFrom(type) &&
                    type.GetCustomAttribute<ActionTypeAttribute>() is { } &&
                    (
                        !excludeObsolete ||
                        !(type.GetCustomAttribute<ActionObsoleteAttribute>() is { } aoAttr) ||
                        aoAttr.ObsoleteIndex > blockIndex
                    );
            }

            var assembly = baseType.Assembly;
            var typeIds = assembly.GetTypes()
                .Where(IsTarget)
                .Select(type => type.GetCustomAttribute<ActionTypeAttribute>()?.TypeIdentifier)
                .OfType<Text>()
                .OrderBy(type => type);

            foreach (Text typeId in typeIds) {
                Console.WriteLine(typeId.Value);
            }
        }

        [PrimaryCommand]
        public void Help([FromService] ICoconaHelpMessageBuilder helpMessageBuilder)
        {
            Console.Error.WriteLine(helpMessageBuilder.BuildAndRenderForCurrentContext());
        }
    }
}
