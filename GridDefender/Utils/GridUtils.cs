using Sandbox.Game.Entities;
using VRage.Game;
using VRageMath;

namespace GVK.GridDefender.Utils
{
    public static class GridUtils
    {
        public static bool IsLargeGrid(this MyCubeGrid grid)
        {
            return grid != null && grid.GridSizeEnum == MyCubeSize.Large;
        }

        public static bool IsSmallGrid(this MyCubeGrid grid)
        {
            return grid != null && grid.GridSizeEnum == MyCubeSize.Small;
        }

        public static float GetSpeed(this MyCubeGrid grid)
        {
            if (grid?.Physics == null) return 0f;
            return grid.Physics.LinearVelocity.Length();
        }

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

