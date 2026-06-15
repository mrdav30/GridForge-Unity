using GridForge.Grids;
using GridForge.Unity;
using NUnit.Framework;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridWorldComponentEditModeTests
    {
        [Test]
        public void RebuildWorldCreatesActiveWorldWithConfiguredSpatialGridCellSize()
        {
            GameObject owner = new GameObject("GridWorldComponent edit mode test");

            try
            {
                GridWorldComponent component = owner.AddComponent<GridWorldComponent>();

                GridWorld world = component.RebuildWorld(7);

                Assert.NotNull(world);
                Assert.AreSame(world, component.World);
                Assert.IsTrue(world.IsActive);
                Assert.AreEqual(7, component.SpatialGridCellSize);
                Assert.AreEqual(7, world.SpatialGridCellSize);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void RebuildWorldClampsInvalidSpatialGridCellSize()
        {
            GameObject owner = new GameObject("GridWorldComponent edit mode test");

            try
            {
                GridWorldComponent component = owner.AddComponent<GridWorldComponent>();

                GridWorld world = component.RebuildWorld(0);

                Assert.NotNull(world);
                Assert.AreEqual(1, component.SpatialGridCellSize);
                Assert.AreEqual(1, world.SpatialGridCellSize);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
