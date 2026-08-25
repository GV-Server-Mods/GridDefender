using Sandbox.Game.Entities;
using VRage.Game;
using VRageMath;

namespace GVK.GridDefender.Utils
{
    /// <summary>
    /// Utility methods for grid queries, grouping checks, and speed evaluation.
    /// </summary>
    public static class GridUtils
    {
        /// <summary>
        /// Returns true if the grid is a large grid.
        /// </summary>
        public static bool IsLargeGrid(this MyCubeGrid grid)
        {
            return grid != null && grid.GridSizeEnum == MyCubeSize.Large;
        }

        /// <summary>
        /// Returns true if the grid is a small grid.
        /// </summary>
        public static bool IsSmallGrid(this MyCubeGrid grid)
        {
            return grid != null && grid.GridSizeEnum == MyCubeSize.Small;
        }

        /// <summary>
        /// Gets the current linear speed of the grid in m/s without heap allocations.
        /// </summary>
        public static float GetSpeed(this MyCubeGrid grid)
        {
            if (grid?.Physics == null) return 0f;
            return grid.Physics.LinearVelocity.Length();
        }

        /// <summary>
        /// Checks if two grids belong to the same mechanical group (e.g. connected via rotors, pistons, hinges, or suspension wheel attachments).
        /// </summary>
        public static bool AreInSameMechanicalGroup(MyCubeGrid gridA, MyCubeGrid gridB)
        {
            if (gridA == null || gridB == null) return false;
            if (ReferenceEquals(gridA, gridB)) return true;

            try
            {
                return MyCubeGridGroups.Static?.Mechanical?.HasSameGroup(gridA, gridB) ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if two grids belong to the same logical group (e.g. connected via connectors or landing gear).
        /// </summary>
        public static bool AreInSameLogicalGroup(MyCubeGrid gridA, MyCubeGrid gridB)
        {
            if (gridA == null || gridB == null) return false;
            if (ReferenceEquals(gridA, gridB)) return true;

            try
            {
                return MyCubeGridGroups.Static?.Logical?.HasSameGroup(gridA, gridB) ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}

