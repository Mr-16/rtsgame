using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts
{
    public partial class BuildingPreviewBase : Node3D
    {
        [Export] public int Width = 3;
        [Export] public int Height = 3;

        public override void _Ready()
        {
        }

        public override void _Process(double delta)
        {
        }

        public virtual void SetCanPlace(bool canPlace)
        {

        }
    }
}
