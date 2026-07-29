using NLog;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{

    public class GridValidator
    {
        private readonly Logger Log = LogManager.GetLogger(typeof(GridValidator).FullName);

        private readonly List<MyDefinitionId> requiredBlockDefinitions;

        private readonly Config config;

        public GridValidator(
            Config config,
            List<MyDefinitionId> requiredBlockDefinitions)
        {
            this.config = config;
            this.requiredBlockDefinitions = requiredBlockDefinitions;
        }

        public GridValidationResult ValidateGrid(GridInfo info)
        {
            var result = new GridValidationResult
            {
                Grid = info
            };

            var messages = GridMessages.Get(config);

            // Check for owner
            if (info.OwnerId == 0)
            {
                Log.Warn($"{info.Name} has no owner.");
            }

            // Check required blocks
            if (config.EnableBlockCheck && !(config.IgnoreSubgridsForBlockCheck && info.IsSubgrid))
            {

                 CheckRequiredBlocks(info, result, messages);
            }

            // Check generic grid name
            if (config.EnableNameCheck && !(config.IgnoreSubgridsForNameCheck && info.IsSubgrid))
            {

                CheckGridName(info, result, messages);
            }

            return result;
        }

        private void CheckRequiredBlocks(
            GridInfo info,
            GridValidationResult result, MessageSet messages)
        {
            // Group all definitions by TypeId.
            // Example:
            //
            // MyObjectBuilder_Beacon
            //     - LargeBlockBeacon
            //     - SmallBlockBeacon
            //     - LargeBlockBeaconReskin
            //     - SmallBlockBeaconReskin
            //
            // These are treated as ONE required category: Beacon.

            var requiredCategories = requiredBlockDefinitions
                .GroupBy(definition => definition.TypeId);

            foreach (var category in requiredCategories)
            {
                // Check if the grid contains ANY subtype
                // belonging to this category.
                bool hasRequiredBlock = info.FoundBlocks
                    .Any(foundBlock =>
                        foundBlock.TypeId == category.Key);

                if (!hasRequiredBlock)
                {
                    string categoryName = category.Key.ToString();

                    // Convert:
                    // MyObjectBuilder_Beacon
                    //
                    // Into:
                    // Beacon
                    if (categoryName.StartsWith("MyObjectBuilder_"))
                    {
                        categoryName = categoryName.Substring(
                            "MyObjectBuilder_".Length);
                    }

                    result.Problems.Add($"{messages.MissingPrefix} {categoryName}");
                }
            }
        }

        private void CheckGridName(GridInfo info, GridValidationResult result, MessageSet messages)
        {
            foreach (var genericName in config.GenericGridNames)
            {
                if (info.Name.StartsWith(genericName))
                {
                    result.Problems.Add(messages.GenericGridName);
                    break;
                }
            }
        }
    }
}