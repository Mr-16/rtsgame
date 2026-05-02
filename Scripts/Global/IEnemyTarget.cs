using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts.Global
{
    public interface IEnemyTarget
    {
        public float GetModelRadius();
        public Vector3 GetPos();

        public void TakeDmg(int damage);

        public bool IsValid();
    }
}
