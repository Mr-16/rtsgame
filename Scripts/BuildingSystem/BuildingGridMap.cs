using Godot;
using System.Collections.Generic;

namespace RtsGame.Scripts
{
    /// <summary>
    /// RTS 建筑网格系统 - 彻底支持负世界坐标
    /// 内部网格始终使用非负下标，Remove 时无需宽高
    /// </summary>
    public partial class BuildingGridMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; } = 1.0f;

        // 网格在世界坐标中的左下角原点（非常重要！）
        public Vector3 Origin { get; set; } = new Vector3(-100f, 0f, -100f); // ← 根据你的地图范围调整

        private bool[,] _grid;

        // 记录每个建筑的占用区域（Key = 建筑左下角的网格坐标）
        private readonly Dictionary<Vector2I, HashSet<Vector2I>> _occupiedAreas = new();

        public BuildingGridMap(Vector3 origin, int width, int height, float cellSize = 1.0f)
        {
            Origin = origin;
            Width = width;
            Height = height;
            CellSize = cellSize;
            _grid = new bool[width, height];
        }

        // ====================== 坐标转换（核心修复） ======================
        public Vector2I WorldToGrid(Vector3 worldPos)
        {
            float relativeX = worldPos.X - Origin.X;
            float relativeZ = worldPos.Z - Origin.Z;

            // 使用 FloorToInt，保证负数也能正确映射到 >=0 的网格
            int gridX = Mathf.FloorToInt(relativeX / CellSize);
            int gridZ = Mathf.FloorToInt(relativeZ / CellSize);

            return new Vector2I(gridX, gridZ);
        }

        public Vector3 GridToWorld(Vector2I gridPos)
        {
            return new Vector3(
                Origin.X + gridPos.X * CellSize + CellSize * 0.5f,
                0.0f,
                Origin.Z + gridPos.Y * CellSize + CellSize * 0.5f
            );
        }

        // ====================== 核心功能 ======================
        // --- 修改后的 CanPlace ---
        public bool CanPlace(Vector3 centerWorldPos, int buildWidth, int buildHeight)
        {
            // 关键：从中心点推导出左下角的起始网格索引
            // 算法：中心点坐标 - (宽度/2 * CellSize) 得到左下角物理位置
            float startX = centerWorldPos.X - (buildWidth * CellSize * 0.5f);
            float startZ = centerWorldPos.Z - (buildHeight * CellSize * 0.5f);

            Vector2I start = WorldToGrid(new Vector3(startX, centerWorldPos.Y, startZ));

            for (int x = 0; x < buildWidth; x++)
            {
                for (int y = 0; y < buildHeight; y++)
                {
                    Vector2I cell = new Vector2I(start.X + x, start.Y + y);

                    if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height)
                        return false;

                    if (_grid[cell.X, cell.Y])
                        return false;
                }
            }
            return true;
        }

        // --- 修改后的 Place ---
        public bool Place(Vector3 centerWorldPos, int buildWidth, int buildHeight)
        {
            // 同样先找起点
            float startX = centerWorldPos.X - (buildWidth * CellSize * 0.5f);
            float startZ = centerWorldPos.Z - (buildHeight * CellSize * 0.5f);
            Vector2I start = WorldToGrid(new Vector3(startX, centerWorldPos.Y, startZ));

            if (!CanPlace(centerWorldPos, buildWidth, buildHeight))
                return false;

            var cells = new HashSet<Vector2I>();

            for (int x = 0; x < buildWidth; x++)
            {
                for (int y = 0; y < buildHeight; y++)
                {
                    Vector2I cell = new Vector2I(start.X + x, start.Y + y);
                    _grid[cell.X, cell.Y] = true;
                    cells.Add(cell);
                }
            }

            // 这里存的时候，建议依然存 start 索引，方便后续 Remove
            _occupiedAreas[start] = cells;
            return true;
        }

        public void Remove(Vector3 worldPos)
        {
            Vector2I start = WorldToGrid(worldPos);

            if (_occupiedAreas.TryGetValue(start, out var cells))
            {
                foreach (var cell in cells)
                {
                    if (cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height)
                        _grid[cell.X, cell.Y] = false;
                }
                _occupiedAreas.Remove(start);
            }
        }

        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            Vector2I gridPos = WorldToGrid(worldPos);
            return GridToWorld(gridPos);
        }

        // ====================== 辅助 ======================
        public bool IsOccupied(Vector3 worldPos)
        {
            Vector2I pos = WorldToGrid(worldPos);
            return IsOccupied(pos);
        }

        public bool IsOccupied(Vector2I gridPos)
        {
            if (gridPos.X < 0 || gridPos.X >= Width || gridPos.Y < 0 || gridPos.Y >= Height)
                return true;

            return _grid[gridPos.X, gridPos.Y];
        }

        public void ClearAll()
        {
            _grid = new bool[Width, Height];
            _occupiedAreas.Clear();
        }

        public void DebugPrint()
        {
            GD.PrintRich("[color=yellow]=== Grid Map Debug ===[/color]");
            GD.Print($"Origin = {Origin}, CellSize = {CellSize}");
            for (int y = 0; y < Mathf.Min(Height, 20); y++)
            {
                string line = "";
                for (int x = 0; x < Mathf.Min(Width, 30); x++)
                {
                    line += _grid[x, y] ? "■ " : "□ ";
                }
                GD.Print(line);
            }
        }
    }
}