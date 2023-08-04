using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
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
                Description = "If true, show obsoleted or will be obsoleted actions only (Actions having ActionObsolete attribute only)"
            )] bool obsoleteOnly = false,
            [Option(
                Description = "If true, make json file with results"
            )] string jsonPath = null,
            [Option(
                Description = "If true, filter obsoleted actions since the --block-index option."
            )] bool excludeObsolete = false,
            [Option(
                Description = "The current block index to filter obsoleted actions."
            )] long blockIndex = 0
        )
        {
            var baseType = typeof(ActionBase);

            bool IsTarget(Type type)
            {
                if (!baseType.IsAssignableFrom(type) ||
                    type.GetCustomAttribute<ActionTypeAttribute>() is null)
                {
                    return false;
                }

                var isObsoleteTarget = type.GetCustomAttribute<ActionObsoleteAttribute>() is not null;
                var isObsolete = type.GetCustomAttribute<ActionObsoleteAttribute>() is { } attr &&
                                 attr.ObsoleteIndex <= blockIndex;

                if (obsoleteOnly) return isObsoleteTarget;
                if (excludeObsolete) return !isObsolete;

                return true;
            }

            var assembly = baseType.Assembly;
            var typeIds = assembly.GetTypes()
                .Where(IsTarget)
                .Select(type => ((IValue typeId, long? obsoleteAt))(
                    type.GetCustomAttribute<ActionTypeAttribute>()?.TypeIdentifier,
                    type.GetCustomAttribute<ActionObsoleteAttribute>()?.ObsoleteIndex
                ))
                .Where(type => type.typeId is Text)
                .OrderBy(type => ((Text)type.typeId).Value);

            var jsonResult = new JsonArray();

            foreach (var (typeIdValue, obsoleteAt) in typeIds)
            {
                var typeId = (Text)typeIdValue;
                var json = new JsonObject { ["id"] = typeId.Value };
                if (obsoleteAt != null) json.Add("obsoleteAt", obsoleteAt);
                jsonResult.Add(json);

                Console.WriteLine(typeId.Value);
            }

            if (jsonPath != null)
            {
                using var stream = File.CreateText(jsonPath);
                stream.WriteLine(jsonResult.ToString());
            }
        }

        [PrimaryCommand]
        public void Help([FromService] ICoconaHelpMessageBuilder helpMessageBuilder)
        {
            Console.Error.WriteLine(helpMessageBuilder.BuildAndRenderForCurrentContext());
        }
    }
}
