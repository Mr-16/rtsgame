using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }
        public BuildingGridMap BuildingGridMap = new BuildingGridMap(new Vector3(-300, 0, -300), 300, 300, 2);
        public List<UnitBase> UnitList = new List<UnitBase>();
        public List<MainBase> MainBaseList = new List<MainBase>();
        public uint TERRAIN_MASK = 1 << 0;     // Layer 1
        public uint INTERACTABLE_MASK = 1 << 1; // Layer 2
        public uint UI3D_MASK = 1 << 2; // Layer 3
        public Player Player;

        public override void _Ready()
        {
            // 初始化单例
            Instance = this;

            // 设置为持久化节点，确保切换场景时不会被销毁
            ProcessMode = ProcessModeEnum.Always;
        }
        public override void _Process(double delta)
        {
            //GD.Print("UnitList.Count : " + UnitList.Count);
        }

    }
}
