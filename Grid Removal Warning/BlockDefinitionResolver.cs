using NLog;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{
    public class BlockDefinitionResolver
    {
        private readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Resolves a list of block names to their corresponding MyDefinitionId objects.
        public List<MyDefinitionId> ResolveRequiredBlocks(List<string> blockNames)
        {
            List<MyDefinitionId> definitionIds = new List<MyDefinitionId>();

            var cubeBlockDefinitions =
                MyDefinitionManager.Static.GetDefinitionsOfType<MyCubeBlockDefinition>();

            foreach (var blockName in blockNames)
            {
                string expectedTypeName = "MyObjectBuilder_" + blockName;

                Log.Info(
                    $"Searching definitions for block category: {blockName} " +
                    $"(Expected TypeId: {expectedTypeName})"
                );

                int foundCount = 0;
                int totalDefinitions = 0;
                // Iterate through all cube block definitions and check if the TypeId matches the expected type name.
                foreach (var definition in cubeBlockDefinitions)
                {
                    totalDefinitions++;

                    if (string.Equals(
                        definition.Id.TypeId.ToString(),
                        expectedTypeName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        definitionIds.Add(definition.Id);
                        foundCount++;

                        Log.Info(
                            $"MATCHED {blockName}: {definition.Id}"
                        );
                    }
                }

                if (foundCount == 0)
                {
                    Log.Warn(
                        $"No block definitions found for category: {blockName}"
                    );
                }
                else
                {
                    Log.Info(
                        $"Found {foundCount} definition(s) for category: {blockName}"
                    );
                }
            }

            return definitionIds;
        }
    }
}