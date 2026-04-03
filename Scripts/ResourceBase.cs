using Godot;
using System;

namespace RtsGame.Scripts
{
    public partial class ResourceBase : Node3D
    {
        [Export] public int MaxCount = 10000;
        [Export] public int MinCaptureCount = 1;  // 每次最少采多少
        [Export] public int MaxCaptureCount = 100; // 每次最多采多少
        [Export] public Label3D CurCountLb;

        public int CurCount;
        private Random _random = new Random();

        public override void _Ready()
        {
            CurCount = MaxCount;
            CurCountLb.Text = $"{CurCount}";
            //GD.Print(CurCount);
        }

        public int GetRes()
        {
            if (CurCount <= 0) return 0;
            int amountToHarvest = _random.Next(MinCaptureCount, MaxCaptureCount + 1);// 计算随机采集量
            int actualHarvested = Math.Min(amountToHarvest, CurCount);

            
            CurCount -= actualHarvested;// 更新扣除
            CurCountLb.Text = $"{CurCount}";

            if (CurCount <= 0)
            {
                OnDepleted();
            }

            return actualHarvested;
        }

        private void OnDepleted()
        {
            GD.Print($"{Name} 资源已枯竭");
            QueueFree();
        }
    }
}