GridForge-Unity
==============

![GridForge Icon](https://raw.githubusercontent.com/mrdav30/GridForge/main/icon.png)

**A high-performance, deterministic voxel grid system for spatial partitioning, simulation, and game development in Unity.**

Lightweight and optimized for lockstep engines.

This package is a Unity-specific implementation of the [GridForge](https://github.com/mrdav30/GridForge) library

---

## 🚀 Key Features

- **Voxel-Based Spatial Partitioning** – Build efficient 3D **voxel grids** with fast access & updates.
- **Deterministic & Lockstep Ready** – Designed for **synchronized multiplayer** and physics-safe environments.
- **ScanCell Overlay System** – Accelerated **proximity and radius queries** using spatial hashing.
- **Dynamic Occupancy & Obstacle Tracking** – Manage **moving occupants, dynamic obstacles**, and voxel metadata.
- **Minimal Allocations & Fast Queries** – Built with **SwiftCollections** and **FixedMathSharp** for optimal performance.
- **Multi-Layered Grid System** – **Dynamic, hierarchical, and persistent grids**.

---

## 📦 Installation

### Via Unity Package Manager (UPM)

1. Open **Unity**.
2. Go to **Window** → **Package Manager**.
3. Click the **+** icon and select **"Add package from git URL..."**.
4. Enter:

https://github.com/mrdav30/GridForge-Unity.git

5. Click **Add**.

### Manual Installation

1. Download the .unitypackage file from the [latest release](https://github.com/mrdav30/SwiftCollections-Unity/releases).
2. Open Unity and import the package via **Assets → Import Package → Custom Package...**.
3. Select the downloaded file and import the contents.

---

## 🧩 Dependencies

GridForge-Unity depends on the following Unity packages:

- [FixedMathSharp-Unity](https://github.com/mrdav30/FixedMathSharp-Unity)
- [SwiftCollections-Unity](https://github.com/mrdav30/SwiftCollections-Unity)

These dependencies are automatically included when installing via UPM.

---

## 📖 Usage Examples

### **🔹 Creating a Grid**
```csharp
GridConfiguration config = new GridConfiguration(new Vector3d(-10, 0, -10), new Vector3d(10, 0, 10));
GlobalGridManager.TryAddGrid(config, out ushort gridIndex);
```

### **🔹 Querying a Grid for Nodes**
```csharp
Vector3d queryPosition = new Vector3d(5, 0, 5);
if (GlobalGridManager.TryGetGridAndVoxel(queryPosition, out VoxelGrid grid, out Voxel voxel))
	Console.WriteLine($"Voxel at {queryPosition} is {(voxel.IsOccupied ? "occupied" : "empty")}");
}
```

### **🔹 Adding a Blocker**
```csharp
BoundingArea blockArea = new BoundingArea(new Vector3d(3, 0, 3), new Vector3d(5, 0, 5));
Blocker blocker = new Blocker(blockArea);
blocker.ApplyBlockage();
```

### **🔹 Attaching a Partition to a Voxel**
```csharp
if (GlobalGridManager.TryGetGrid(queryPosition, out VoxelGrid grid, out Voxel voxel))
{
    PathPartition partition = new PathPartition();
    partition.Setup(voxel.GlobalVoxelIndex);
    voxel.AddPartition(partition);
}
```

### **🔹 Scanning for Nearby Occupants**
```csharp
Vector3d scanCenter = new Vector3d(0, 0, 0);
Fixed64 scanRadius = (Fixed64)5;
foreach (IVoxelOccupant occupant in ScanManager.ScanRadius(scanCenter, scanRadius))
{
    Console.WriteLine($"Found occupant at {occupant.WorldPosition}");
}
```

## 🎮 Unity Debugging Tools

GridForge includes **editor utilities** for debugging:

- **GridDebugger** – Visualizes **grids, voxels, and selected areas**.
- **GridTracer Debuging** – Helps debug **line-of-sight & navigation**.
- **Blocker Editor** – Allows **visual blocker placement** via Unity Inspector.

---

## 🔄 Compatibility

- **Unity 2020+**
- **Supports deterministic lockstep engines**
- **Compatible with AI navigation and procedural world systems**

---

## 📄 License

This project is licensed under the MIT License - see the `LICENSE` file for details.

---

## 👥 Contributors

- **David Oravsky** - Lead Developer
- **Contributions Welcome!** Open a PR or issue.

---

## 💬 Community & Support

For questions, discussions, or general support, join the official Discord community:

👉 **[Join the Discord Server](https://discord.gg/mhwK2QFNBA)**

For bug reports or feature requests, please open an issue in this repository.

We welcome feedback, contributors, and community discussion across all projects.
