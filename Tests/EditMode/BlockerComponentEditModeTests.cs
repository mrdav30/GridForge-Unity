using FixedMathSharp;
using FixedMathSharp.Bounds;
using GridForge.Blockers;
using GridForge.Configuration;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class BlockerComponentEditModeTests
    {
        [Test]
        public void BoundsBlockerAppliesOverDenseRectangularGrid()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 0, 3), GridStorageKind.Dense),
                configuredVoxels: null,
                out _,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualFixedBoundArea(new FixedBoundArea(
                    new Vector3d(0, 0, 0),
                    new Vector3d(3, 0, 3)));

                blocker.Start();

                Assert.Greater(CountBlockedVoxels(grid), 0);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BoundsBlockerAppliesOverDenseHexGrid()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                HexConfig(new Vector3d(0, 0, 0), new Vector3d(12, 0, 12), GridStorageKind.Dense),
                configuredVoxels: null,
                out _,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualFixedBoundArea(new FixedBoundArea(
                    new Vector3d(0, 0, 0),
                    new Vector3d(12, 0, 12)));

                blocker.Start();

                Assert.Greater(CountBlockedVoxels(grid), 0);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BoundsBlockerAppliesOnlyConfiguredSparseRectangularVoxels()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 0, 3), GridStorageKind.Sparse),
                new[]
                {
                    new VoxelIndex(0, 0, 0),
                    new VoxelIndex(3, 0, 3)
                },
                out _,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualFixedBoundArea(new FixedBoundArea(
                    new Vector3d(0, 0, 0),
                    new Vector3d(3, 0, 3)));

                blocker.Start();

                Assert.AreEqual(2, CountBlockedVoxels(grid));
                Assert.IsFalse(grid.ContainsVoxel(new VoxelIndex(1, 0, 1)));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BoundsBlockerAppliesOnlyConfiguredSparseHexVoxels()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                HexConfig(new Vector3d(0, 0, 0), new Vector3d(12, 0, 12), GridStorageKind.Sparse),
                new[]
                {
                    new VoxelIndex(0, 0, 0),
                    new VoxelIndex(1, 0, 0)
                },
                out _,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualFixedBoundArea(new FixedBoundArea(
                    new Vector3d(0, 0, 0),
                    new Vector3d(12, 0, 12)));

                blocker.Start();

                Assert.AreEqual(2, CountBlockedVoxels(grid));
                Assert.IsFalse(grid.ContainsVoxel(new VoxelIndex(2, 0, 0)));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void XzLayerBlockerUsesVector2dBoundsAndDoesNotAffectOtherLayers()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 1, 3), GridStorageKind.Dense),
                configuredVoxels: null,
                out _,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualXzArea(new Vector2d(0, 0), new Vector2d(3, 3), layerY: Fixed64.One);

                blocker.Start();

                Assert.AreEqual(0, CountBlockedVoxelsOnLayer(grid, 0));
                Assert.Greater(CountBlockedVoxelsOnLayer(grid, 1), 0);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void CoveragePreviewCountsCellsWithoutApplyingBlockage()
        {
            GameObject owner = CreateBlockerOwnerWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 0, 3), GridStorageKind.Dense),
                configuredVoxels: null,
                out GridWorld world,
                out VoxelGrid grid);

            try
            {
                BlockerComponent blocker = owner.AddComponent<BlockerComponent>();
                blocker.ConfigureBoundsBlocker();
                blocker.ConfigureManualFixedBoundArea(new FixedBoundArea(
                    new Vector3d(0, 0, 0),
                    new Vector3d(3, 0, 3)));

                Assert.IsTrue(blocker.TryCountPreviewCoverage(world, out int gridCount, out int voxelCount));
                Assert.AreEqual(1, gridCount);
                Assert.Greater(voxelCount, 0);
                Assert.AreEqual(0, CountBlockedVoxels(grid));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static GameObject CreateBlockerOwnerWithGrid(
            GridConfiguration config,
            VoxelIndex[] configuredVoxels,
            out GridWorld world,
            out VoxelGrid grid)
        {
            GameObject owner = new("Blocker component edit mode test");
            GridWorldComponent worldComponent = owner.AddComponent<GridWorldComponent>();
            world = worldComponent.RebuildWorld(16);

            bool added = configuredVoxels == null
                ? world.TryAddGrid(config, out ushort gridIndex)
                : world.TryAddGrid(config, configuredVoxels, out gridIndex);

            Assert.IsTrue(added);
            grid = world.ActiveGrids[gridIndex];
            return owner;
        }

        private static GridConfiguration RectangularConfig(
            Vector3d min,
            Vector3d max,
            GridStorageKind storageKind) =>
            new(
                min,
                max,
                scanCellSize: 2,
                GridTopologyKind.RectangularPrism,
                GridTopologyMetrics.Rectangular(Fixed64.One, Fixed64.One, Fixed64.One),
                storageKind);

        private static GridConfiguration HexConfig(
            Vector3d min,
            Vector3d max,
            GridStorageKind storageKind) =>
            new(
                min,
                max,
                scanCellSize: 2,
                GridTopologyKind.HexPrism,
                GridTopologyMetrics.Hex(Fixed64.One, Fixed64.One, HexOrientation.PointyTop),
                storageKind);

        private static int CountBlockedVoxels(VoxelGrid grid)
        {
            int count = 0;
            foreach (Voxel voxel in grid.EnumerateVoxels())
            {
                if (voxel.IsBlocked)
                    count++;
            }

            return count;
        }

        private static int CountBlockedVoxelsOnLayer(VoxelGrid grid, int layerY)
        {
            int count = 0;
            foreach (Voxel voxel in grid.EnumerateVoxels())
            {
                if (voxel.Index.y == layerY && voxel.IsBlocked)
                    count++;
            }

            return count;
        }
    }
}
