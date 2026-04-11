using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts
{
    public partial class BuildingBase : Node3D
    {
        [Export] public float ModelRadius = 2;
        public override void _Ready()
        {
            GameManager.Instance.BuildingList.Add(this);
        }

        public override void _Process(double delta)
        {
        }

        public virtual void SetSelected(bool isSelected)
        {

        }
    }
}
