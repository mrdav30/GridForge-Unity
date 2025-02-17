GridForge-Unity
==============

![GridForge Icon](https://raw.githubusercontent.com/mrdav30/GridForge/main/icon.png)

**A high-performance, deterministic spatial grid management system for Unity, optimized for lockstep simulations, AI navigation, and world partitioning.**   

This package is a Unity-specific implementation of the [GridForge](https://github.com/mrdav30/GridForge) library

---

## 🚀 Key Features

- **Deterministic Execution** – Supports **lockstep simulation** and **fixed-point** arithmetic.
- **Optimized Grid Management** – **Low memory allocations, spatial partitioning, and fast queries**.
- **Multi-Layered Grid System** – **Dynamic, hierarchical, and persistent  grids**.
- **Efficient Object Queries** – Retrieve **occupants, obstacles, and partitions** with minimal overhead.

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
if (GlobalGridManager.TryGetGrid(queryPosition, out Grid grid))
{
    if (grid.TryGetNode(queryPosition, out Node node))
    {
        Console.WriteLine($"Node at {queryPosition} is {(node.IsOccupied ? "occupied" : "empty")}");
    }
}
```

### **🔹 Adding a Blocker**
```csharp
BoundingArea blockArea = new BoundingArea(new Vector3d(3, 0, 3), new Vector3d(5, 0, 5));
Blocker blocker = new Blocker(blockArea);
blocker.ApplyBlockage();
```

### **🔹 Attaching a Partition to a Node**
```csharp
if (GlobalGridManager.TryGetGrid(queryPosition, out Grid grid) && grid.TryGetNode(queryPosition, out Node node))
{
    PathPartition partition = new PathPartition();
    partition.Setup(node.GlobalCoordinates);
    node.AddPartition(partition);
}
```

### **🔹 Scanning for Nearby Occupants**
```csharp
Vector3d scanCenter = new Vector3d(0, 0, 0);
Fixed64 scanRadius = (Fixed64)5;
foreach (INodeOccupant occupant in ScanManager.ScanRadius(scanCenter, scanRadius))
{
    Console.WriteLine($"Found occupant at {occupant.WorldPosition}");
}
```

## 🎮 Unity Debugging Tools

GridForge includes **editor utilities** for debugging:

- **GridDebugger** – Visualizes **grids, nodes, and selected areas**.
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

## 📧 Contact

For questions or support, open an issue on GitHub.
