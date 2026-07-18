using NLog;
using Sandbox.Definitions;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{
    // The BlockDefinitionResolver class is responsible for resolving block names to their corresponding MyDefinitionId objects.
    public class BlockDefinitionResolver
    {
        private Logger Log = LogManager.GetCurrentClassLogger();
        // Resolve a list of block names to their corresponding MyDefinitionId objects.
        public List<MyDefinitionId> ResolveRequiredBlocks(List<string> blockNames)
        {
            List<MyDefinitionId> definitionIds = new List<MyDefinitionId>();

            foreach (var blockName in blockNames)
            {
                MyCubeBlockDefinition definition = FindBlockDefinition(blockName);

                if (definition == null)
                {
                    Log.Warn($"Block definition not found for: {blockName}");
                    continue;
                }

                    Log.Info($"Resolved block definition for {blockName}: {definition.Id}");

                definitionIds.Add(definition.Id);
            }

            return definitionIds;
        }

        private MyCubeBlockDefinition FindBlockDefinition(string blockName)
        {
            Log.Info("Starting definition search...");

            var definitions =
                MyDefinitionManager.Static.GetAllDefinitions<MyCubeBlockDefinition>();

            Log.Info($"Total block definitions found: {definitions.Count()}");

            foreach (var definition in definitions)
            {
                Log.Info(
                    $"Definition: Id={definition.Id} | DisplayNameText={definition.DisplayNameText}"
                );
            }

            return null;
        }
    }
}