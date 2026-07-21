using NLog.Fluent;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{
    //    The GridScanner class is responsible for scanning the game world for grids and collecting information about them.
    public class GridScanner
    {
        private readonly Config config;

        public GridScanner(Config config)
        {
            this.config = config;
        }
        private bool HasOnlineOwner(MyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0)
                return false;


            foreach (var ownerId in grid.BigOwners)
            {
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (player.Identity.IdentityId == ownerId)
                        return true;
                }
            }


            return false;
        }

        // Scan the game world for grids and return a list of GridInfo objects containing information about each grid.
        public List<GridInfo> Scan()
        {
            List<GridInfo> scannedGrids = new List<GridInfo>();

            List<MyCubeGrid> grids = MyEntities.GetEntities()
                .OfType<MyCubeGrid>()
                .ToList();

            // Build once if either check needs subgrid info.
            HashSet<long> subgridIds = (config.IgnoreSubgridsForBlockCheck || config.IgnoreSubgridsForNameCheck)
                ? GetSubgridIds()
                : new HashSet<long>();

            // Filter out grids that do not meet the minimum block count requirement and collect information about the remaining grids.
            foreach (var grid in grids)
            {
                if (!HasOnlineOwner(grid))
                    continue;


                if (grid.BlocksCount <= config.MinimumBlocks)
                    continue;
                long ownerId = grid.BigOwners.FirstOrDefault();

                var identity = MySession.Static.Players.TryGetIdentity(ownerId);

                // Create a new GridInfo object and populate it with information about the grid.
                var info = new GridInfo
                {
                    EntityId = grid.EntityId,
                    Name = grid.DisplayName,
                    BlockCount = grid.BlocksCount,
                    OwnerId = grid.BigOwners[0],
                    OwnerName = identity?.DisplayName?? "Unknown", // Optional: Populate the owner's name
                    Grid = grid,
                    FoundBlocks = GetGridBlocks(grid),
                    IsSubgrid = subgridIds.Contains(grid.EntityId)
                };


                scannedGrids.Add(info);
            }


            return scannedGrids;
        }

        private HashSet<long> GetSubgridIds()
        {
            var subgridIds = new HashSet<long>();

            foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
            {
                foreach (var block in grid.GetFatBlocks<MyMechanicalConnectionBlockBase>())
                {
                    if (block.TopGrid != null)
                        subgridIds.Add(block.TopGrid.EntityId);
                }
            }

            return subgridIds;
        }

        // Get a list of unique block types present in the given grid.
        private List<MyDefinitionId> GetGridBlocks(MyCubeGrid grid)
        {
            List<MyDefinitionId> blocks = new List<MyDefinitionId>();

            foreach (var block in grid.CubeBlocks)
            {
                MyDefinitionId definitionId = block.BlockDefinition.Id;

                if (!blocks.Contains(definitionId))
                    blocks.Add(definitionId);
            }

            return blocks;
        }

    }
}