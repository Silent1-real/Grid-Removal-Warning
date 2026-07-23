using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game;

namespace Grid_Removal_Warning
{
    public class GridScanner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Config config;

        private List<MyCubeGrid> allGridsSnapshot;
        private List<MyPlayer> pendingPlayers;
        private HashSet<long> processedGridIds;
        private HashSet<long> discoveredSubgridIds;
        private bool checkSubgrids;
        private List<GridInfo> currentCycleResults;
        private int playerIndex;

        private Stopwatch cycleStopwatch;

        public bool IsScanning { get; private set; }

        public GridScanner(Config config)
        {
            this.config = config;
        }

        // ---------------- Batched cycle (automatic scan / scan & warn commands) ----------------

        // Call once, when a new scan cycle begins.
        public void StartScan()
        {
            cycleStopwatch = Stopwatch.StartNew();

            var snapshotWatch = Stopwatch.StartNew();
            allGridsSnapshot = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();
            snapshotWatch.Stop();

            Log.Info($"[Perf] Entity snapshot ({allGridsSnapshot.Count} grids) took {snapshotWatch.ElapsedMilliseconds} ms.");

            pendingPlayers = MySession.Static.Players.GetOnlinePlayers().ToList();

            processedGridIds = new HashSet<long>();
            currentCycleResults = new List<GridInfo>();
            discoveredSubgridIds = new HashSet<long>();
            playerIndex = 0;

            checkSubgrids = config.IgnoreSubgridsForBlockCheck || config.IgnoreSubgridsForNameCheck;

            IsScanning = true;

            Log.Info($"Scan cycle started: {pendingPlayers.Count} online players, {allGridsSnapshot.Count} total grids.");
        }

        // Call once per tick. Returns true when the gathering phase is complete.
        public bool StepScan()
        {
            if (!IsScanning)
                return true;

            if (playerIndex >= pendingPlayers.Count)
            {
                FinishScan();
                return true;
            }

            var stepWatch = Stopwatch.StartNew();

            var player = pendingPlayers[playerIndex];
            long identityId = player.Identity.IdentityId;

            var ownedGrids = allGridsSnapshot
                .Where(g => g.BigOwners.Contains(identityId))
                .ToList();

            // Only checked if config actually cares - finds subgrids attached to
            // THIS player's grids. Offline-owned subgrids are filtered out for
            // free elsewhere: we only ever enumerate online players' owned grids,
            // so a subgrid owned by an offline player never enters currentCycleResults.
            if (checkSubgrids)
            {
                foreach (var grid in ownedGrids)
                {
                    foreach (var block in grid.GetFatBlocks<MyMechanicalConnectionBlockBase>())
                    {
                        if (block.TopGrid != null)
                            discoveredSubgridIds.Add(block.TopGrid.EntityId);
                    }
                }
            }
            
            int gridsThisStep = 0;

            foreach (var grid in ownedGrids)
            {
                // Dedupe: a grid with multiple big owners would
                // otherwise get processed once per owner.
                if (!processedGridIds.Add(grid.EntityId))
                    continue;

                if (grid.BlocksCount <= config.MinimumBlocks)
                    continue;

                currentCycleResults.Add(new GridInfo
                {
                    EntityId = grid.EntityId,
                    Name = grid.DisplayName,
                    BlockCount = grid.BlocksCount,
                    OwnerId = identityId,
                    OwnerName = player.DisplayName,
                    Grid = grid,
                    FoundBlocks = GetGridBlocks(grid),
                    IsSubgrid = discoveredSubgridIds.Contains(grid.EntityId)
                });

                gridsThisStep++;
            }

            stepWatch.Stop();

            Log.Info($"[Perf] Step {playerIndex + 1}/{pendingPlayers.Count} ({player.DisplayName}): " +
                     $"{gridsThisStep} grids processed in {stepWatch.ElapsedMilliseconds} ms.");

            playerIndex++;

            if (playerIndex >= pendingPlayers.Count)
            {
                FinishScan();
                return true;
            }

            return false;
        }

        private void FinishScan()
        {
            IsScanning = false;

            // Correction pass: a grid may have been added to results before we
            // discovered it was a subgrid (its owner's step ran before the step
            // that found the mechanical connection to it). This only touches the
            // already-collected results (a handful), not the whole world.
            if (checkSubgrids)
            {
                foreach (var info in currentCycleResults)
                {
                    if (!info.IsSubgrid && discoveredSubgridIds.Contains(info.EntityId))
                        info.IsSubgrid = true;
                }
            }

            cycleStopwatch.Stop();

            Log.Info($"[Perf] Full scan cycle completed in {cycleStopwatch.ElapsedMilliseconds} ms "
                     + $"may includes at least one tick duration. (spread across {playerIndex} ticks). "
                     + $"Grids collected: {currentCycleResults.Count}.");
        }

        public List<GridInfo> GetResults() => currentCycleResults;

        // ---------------- Single-player scan (check command) ----------------

        // Cheap, instant scan limited to one player's grids. No batching needed -
        // cost scales with one player's grid count, not the whole server.
        public List<GridInfo> ScanSinglePlayer(long identityId)
        {
            var results = new List<GridInfo>();

            var player = MySession.Static.Players.GetOnlinePlayers()
                .FirstOrDefault(p => p.Identity.IdentityId == identityId);

            if (player == null)
                return results;

            var ownedGrids = MyEntities.GetEntities()
                .OfType<MyCubeGrid>()
                .Where(g => g.BigOwners.Contains(identityId))
                .ToList();

            HashSet<long> subgrids = (config.IgnoreSubgridsForBlockCheck || config.IgnoreSubgridsForNameCheck)
                ? GetSubgridIds(ownedGrids)
                : new HashSet<long>();

            foreach (var grid in ownedGrids)
            {
                if (grid.BlocksCount <= config.MinimumBlocks)
                    continue;

                results.Add(new GridInfo
                {
                    EntityId = grid.EntityId,
                    Name = grid.DisplayName,
                    BlockCount = grid.BlocksCount,
                    OwnerId = identityId,
                    OwnerName = player.DisplayName,
                    Grid = grid,
                    FoundBlocks = GetGridBlocks(grid),
                    IsSubgrid = subgrids.Contains(grid.EntityId)
                });
            }

            return results;
        }

        // ---------------- Shared helpers ----------------

        private HashSet<long> GetSubgridIds(List<MyCubeGrid> gridsToCheck)
        {
            var ids = new HashSet<long>();

            foreach (var grid in gridsToCheck)
            {
                foreach (var block in grid.GetFatBlocks<MyMechanicalConnectionBlockBase>())
                {
                    if (block.TopGrid != null)
                        ids.Add(block.TopGrid.EntityId);
                }
            }

            return ids;
        }

        private HashSet<MyDefinitionId> GetGridBlocks(MyCubeGrid grid)
        {
            HashSet<MyDefinitionId> blocks = new HashSet<MyDefinitionId>();

            foreach (var block in grid.CubeBlocks)
            {
                blocks.Add(block.BlockDefinition.Id);
            }

            return blocks;
        }
    }
}
