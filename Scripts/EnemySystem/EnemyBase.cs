using Godot;
using RtsGame.Scripts.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts.EnemySystem
{
    public partial class EnemyBase : Node3D
    {
        [Export] public EnemeyType Type;

        public override void _Ready()
        {
            GameManager.Instance.EnemyList.Add(this);
        }

        public override void _PhysicsProcess(double delta)
        {

        }

        public virtual void TakeDmg(float damage)
        {
        }
    }
}
