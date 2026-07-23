using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;

namespace Grid_Removal_Warning
{
    public class GridInfo
    {
        public long EntityId { get; set; } // Store the grid's entity ID for reference
        public string Name { get; set; } // Store the grid's display name for easier identification
        public int BlockCount { get; set; } // Store the number of blocks in the grid
        public long OwnerId { get; set; } // Store the owner's identity ID for reference
        public string OwnerName { get; set; } // Optional: Store the owner's name for easier identification
        public MyCubeGrid Grid { get; set; } // Store a reference to the actual MyCubeGrid object for further operations if needed
        public HashSet<MyDefinitionId> FoundBlocks { get; set; } = new HashSet<MyDefinitionId>(); // Store a list of unique block present in the grid for validation purposes
        public bool IsSubgrid { get; set; }// Optional: Store whether the grid is a subgrid or not for additional context
    }
}
