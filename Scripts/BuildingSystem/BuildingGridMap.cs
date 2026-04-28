using Godot;
using System.Collections.Generic;

namespace RtsGame.Scripts
{
    /// <summary>
    /// RTS 建筑网格系统 - 完美支持正负坐标与奇偶对齐
    /// </summary>
    public partial class BuildingGridMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; }

        // 网格在世界坐标中的起始点（通常是地图的最左下角）
        public Vector3 Origin { get; set; }

        private bool[,] _grid;

        // Key: 建筑左下角的起始网格坐标, Value: 占用的所有网格集合
        private readonly Dictionary<Vector2I, HashSet<Vector2I>> _occupiedAreas = new();

        public BuildingGridMap(Vector3 origin, int width, int height, float cellSize = 1.0f)
        {
            Origin = origin;
            Width = width;
            Height = height;
            CellSize = cellSize;
            _grid = new bool[width, height];
        }

        // ====================== 核心坐标转换 ======================

        /// <summary>
        /// 将世界物理坐标转换为网格下标 (始终返回 Floor 值)
        /// </summary>
        public Vector2I WorldToGrid(Vector3 worldPos)
        {
            float relativeX = worldPos.X - Origin.X;
            float relativeZ = worldPos.Z - Origin.Z;

            return new Vector2I(
                Mathf.FloorToInt(relativeX / CellSize),
                Mathf.FloorToInt(relativeZ / CellSize)
            );
        }

        /// <summary>
        /// 将网格下标转换为格子的物理中心点坐标
        /// </summary>
        public Vector3 GridToWorld(Vector2I gridPos)
        {
            return new Vector3(
                Origin.X + gridPos.X * CellSize + CellSize * 0.5f,
                Origin.Y,
                Origin.Z + gridPos.Y * CellSize + CellSize * 0.5f
            );
        }

        /// <summary>
        /// 获取建筑左下角的起始网格索引（核心修复：处理奇偶偏移）
        /// </summary>
        private Vector2I GetStartGridIndex(Vector3 centerWorldPos, int buildWidth, int buildHeight)
        {
            // 计算建筑左下角的物理位置
            float startX = centerWorldPos.X - (buildWidth * CellSize * 0.5f);
            float startZ = centerWorldPos.Z - (buildHeight * CellSize * 0.5f);

            // 加上一个微小的偏移(0.01)防止浮点数精度导致的边界判定错误
            return WorldToGrid(new Vector3(startX + 0.01f, centerWorldPos.Y, startZ + 0.01f));
        }

        // ====================== 放置逻辑 ======================

        public bool CanPlace(Vector3 centerWorldPos, int buildWidth, int buildHeight)
        {
            Vector2I start = GetStartGridIndex(centerWorldPos, buildWidth, buildHeight);

            for (int x = 0; x < buildWidth; x++)
            {
                for (int y = 0; y < buildHeight; y++)
                {
                    Vector2I cell = new Vector2I(start.X + x, start.Y + y);

                    // 越界检查
                    if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height)
                        return false;

                    // 占用检查
                    if (_grid[cell.X, cell.Y])
                        return false;
                }
            }
            return true;
        }

        public bool Place(Vector3 centerWorldPos, int buildWidth, int buildHeight)
        {
            if (!CanPlace(centerWorldPos, buildWidth, buildHeight))
                return false;

            Vector2I start = GetStartGridIndex(centerWorldPos, buildWidth, buildHeight);
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

            // 使用建筑左下角索引作为唯一 ID 存入字典
            _occupiedAreas[start] = cells;
            return true;
        }

        /// <summary>
        /// 根据点击位置删除建筑
        /// </summary>
        public void Remove(Vector3 worldPos)
        {
            // 遍历所有已占用的区域，看当前点击点在哪个区域内
            Vector2I gridPos = WorldToGrid(worldPos);
            Vector2I? keyToRemove = null;

            foreach (var pair in _occupiedAreas)
            {
                if (pair.Value.Contains(gridPos))
                {
                    foreach (var cell in pair.Value)
                    {
                        _grid[cell.X, cell.Y] = false;
                    }
                    keyToRemove = pair.Key;
                    break;
                }
            }

            if (keyToRemove.HasValue)
                _occupiedAreas.Remove(keyToRemove.Value);
        }

        // ====================== 对齐逻辑 ======================

        /// <summary>
        /// 建筑预览时的吸附坐标计算
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos, int buildWidth, int buildHeight)
        {
            // 1. 先计算基础的网格坐标（如果是奇数，这正好是中心）
            Vector2I gridPos = WorldToGrid(worldPos);
            Vector3 snappedPos = GridToWorld(gridPos);

            // 2. 偶数尺寸偏移修正：
            // 如果宽度是偶数，中心点应该偏向格子边缘 0.5 个单位
            if (buildWidth % 2 == 0)
            {
                snappedPos.X -= CellSize * 0.5f;
            }
            if (buildHeight % 2 == 0)
            {
                snappedPos.Z -= CellSize * 0.5f;
            }

            return snappedPos;
        }

        // ====================== 辅助工具 ======================

        public bool IsOccupied(Vector3 worldPos)
        {
            Vector2I pos = WorldToGrid(worldPos);
            if (pos.X < 0 || pos.X >= Width || pos.Y < 0 || pos.Y >= Height) return true;
            return _grid[pos.X, pos.Y];
        }

        public void ClearAll()
        {
            _grid = new bool[Width, Height];
            _occupiedAreas.Clear();
        }
    }
}