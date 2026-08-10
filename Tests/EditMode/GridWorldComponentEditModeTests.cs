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

        [Test]
        public void ResolveReturnsNullWhenOnlyAnUnrelatedWorldExists()
        {
            GameObject unrelatedOwner = new GameObject("Unrelated grid world");
            GameObject contextOwner = new GameObject("Grid world resolution context");

            try
            {
                unrelatedOwner.AddComponent<GridWorldComponent>();

                GridWorldComponent resolved = GridWorldComponentUtility.Resolve(
                    contextOwner.transform,
                    assignedComponent: null);

                Assert.IsNull(resolved);
            }
            finally
            {
                Object.DestroyImmediate(contextOwner);
                Object.DestroyImmediate(unrelatedOwner);
            }
        }

        [Test]
        public void ResolvePrefersAssignedThenLocalThenParentWorld()
        {
            GameObject parentOwner = new GameObject("Parent grid world");
            GameObject contextOwner = new GameObject("Local grid world");
            GameObject assignedOwner = new GameObject("Assigned grid world");
            contextOwner.transform.SetParent(parentOwner.transform);

            try
            {
                GridWorldComponent parentWorld = parentOwner.AddComponent<GridWorldComponent>();
                GridWorldComponent localWorld = contextOwner.AddComponent<GridWorldComponent>();
                GridWorldComponent assignedWorld = assignedOwner.AddComponent<GridWorldComponent>();

                Assert.AreSame(
                    assignedWorld,
                    GridWorldComponentUtility.Resolve(contextOwner.transform, assignedWorld));
                Assert.AreSame(
                    localWorld,
                    GridWorldComponentUtility.Resolve(contextOwner.transform, assignedComponent: null));

                Object.DestroyImmediate(localWorld);

                Assert.AreSame(
                    parentWorld,
                    GridWorldComponentUtility.Resolve(contextOwner.transform, assignedComponent: null));
            }
            finally
            {
                Object.DestroyImmediate(assignedOwner);
                Object.DestroyImmediate(contextOwner);
                Object.DestroyImmediate(parentOwner);
            }
        }
    }
}
