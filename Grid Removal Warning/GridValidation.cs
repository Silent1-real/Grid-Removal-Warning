using NLog;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{
    public class GridValidator
    {
        private Logger Log = LogManager.GetCurrentClassLogger();

        private readonly List<MyDefinitionId> requiredBlockDefinitions;

        private readonly Config config;
        public GridValidator(Config config, List<MyDefinitionId> requiredBlockDefinitions)
        {
            this.config = config;
            this.requiredBlockDefinitions = requiredBlockDefinitions;
        }
        // Validate a grid based on the configuration

        public GridValidationResult ValidateGrid(GridInfo info)
        {
            // Create a new validation result for the grid
            var result = new GridValidationResult
            {
                Grid = info
            };

            Log.Info($"Validating grid: {info.Name}");

            // Check for owner

            if (info.OwnerId == 0)
            {
                Log.Warn($"{info.Name} has no owner.");
            }
            // Check for required blocks

            if (config.EnableBlockCheck)
            {
                foreach (var required in requiredBlockDefinitions)
                {
                    if (!info.FoundBlocks.Contains(required))
                    {
                        result.Problems.Add($"Missing required block");
                    }
                }
            }

            if (config.EnableNameCheck)
            {
                CheckGridName(info, result);
            }
            return result;
        }
        // Check if the grid name starts with any of the generic names in the configuration
        private void CheckGridName(GridInfo info, GridValidationResult result)
        {
            foreach (var genericName in config.GenericGridNames)
            {
                if (info.Name.StartsWith(genericName))
                {
                    result.Problems.Add("Generic grid name");
                }
            }
        }
    }
}