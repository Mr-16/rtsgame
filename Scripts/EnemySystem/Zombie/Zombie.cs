using Godot;

namespace RtsGame.Scripts.EnemySystem
{
    public enum ZombieState
    {
        Chase,
        Atk,
        Hurt,
        Death,
    }

    public partial class Zombie : EnemyBase
    {
        [Export] public float MoveSpeed = 4.0f;
        [Export] public float AtkRange = 1.0f;
        [Export] public float MaxHp = 100;
        [Export] private AnimationPlayer animPlayer;
        [Export] private MeshInstance3D HpBarMesh;
        private ShaderMaterial _hpMaterial;

        private float _atkRangeSq;
        private float _curHp;
        private ZombieState _curState;
        private BuildingBase _targetBuilding;

        public override void _Ready()
        {
            base._Ready();
            _curHp = MaxHp;
            _atkRangeSq = AtkRange * AtkRange;
            animPlayer.Play("Move");
            _curState = ZombieState.Chase;
            _hpMaterial = HpBarMesh.GetActiveMaterial(0).Duplicate() as ShaderMaterial;
            _hpMaterial.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpMaterial);
        }

        public override void _PhysicsProcess(double delta)
        {
            switch (_curState)
            {
                case ZombieState.Chase:
                    UpdateChase((float)delta);
                    break;
                case ZombieState.Atk:
                    UpdateAtk((float)delta);
                    break;
                case ZombieState.Hurt:
                    UpdateHurt((float)delta);
                    break;
                case ZombieState.Death:
                    UpdateDeath((float)delta);
                    break;
            }
            //GD.Print("_curState : " + _curState);
        }



        private void UpdateChase(float delta)
        {
            if (_curHp <= 0 && _curState != ZombieState.Death)
            {
                _curState = ZombieState.Death;
                QueueFree();
            }

            _targetBuilding = FindNearestBuilding();
            if (_targetBuilding == null || !IsInstanceValid(_targetBuilding))
                return;

            Vector3 targetPos = _targetBuilding.GlobalPosition;
            float distToTargetSq = GlobalPosition.DistanceSquaredTo(targetPos);

            float totalRange = AtkRange + _targetBuilding.ModelRadius;
            float totalRangeSq = totalRange * totalRange;
            if (distToTargetSq <= totalRangeSq)
            {
                animPlayer.Play("Atk");
                _curState = ZombieState.Atk;
                return;
            }

            Vector3 lookTarget = new Vector3(targetPos.X, GlobalPosition.Y, targetPos.Z);
            LookAt(lookTarget, Vector3.Up);
            Vector3 moveDir = (lookTarget - GlobalPosition).Normalized();
            GlobalPosition += moveDir * MoveSpeed * delta;
        }

        private void UpdateAtk(float delta)
        {
            if (_curHp <= 0 && _curState != ZombieState.Death)
            {
                _curState = ZombieState.Death;
                QueueFree();
            }

            //if (!IsInstanceValid(_targetBuilding))
            //{
            //    TransitionTo(ZombieState.Chase);
            //    return;
            //}

            //// 检查目标是否跑出了范围（可选）
            //float distToTarget = GlobalPosition.DistanceTo(_targetBuilding.GlobalPosition);
            //if (distToTarget > AtkRange + 0.5f) // 加一点缓冲防止状态抖动
            //{
            //    TransitionTo(ZombieState.Chase);
            //    return;
            //}

            // 这里编写攻击逻辑，例如扣除建筑血量
            // GD.Print("正在撕咬建筑...");
        }

        private void UpdateHurt(float delta)
        {
            if (_curHp <= 0 && _curState != ZombieState.Death)
            {
                _curState = ZombieState.Death;
                QueueFree();
            }

            //throw new NotImplementedException();
        }

        private void UpdateDeath(float delta)
        {
            
        }

        private BuildingBase FindNearestBuilding()
        {
            var buildings = GameManager.Instance.BuildingList;
            if (buildings == null || buildings.Count == 0) return null;

            BuildingBase nearest = null;
            float minDistance = float.MaxValue;

            foreach (var building in buildings)
            {
                if (!IsInstanceValid(building)) continue;

                float dist = GlobalPosition.DistanceSquaredTo(building.GlobalPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = building;
                }
            }
            return nearest;
        }

        public override void TakeDmg(float damage)
        {
            _curHp -= damage;
            if (_curHp < 0)
            {
                _curHp = 0;
            }
            _hpMaterial.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpMaterial);
        }
    }
}

