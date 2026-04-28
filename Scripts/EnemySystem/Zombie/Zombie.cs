using Godot;
using System;

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
        [Export] public float MoveSpeed = 2.0f;
        [Export] public float AtkRange = 1.0f;
        [Export] private AnimationPlayer animPlayer;
        [Export] private MeshInstance3D HpBarMesh;
        private ShaderMaterial _hpMaterial;

        private float _atkRangeSq;
        private ZombieState _curState;
        private BuildingBase _targetBuilding;

        private int _searchInterval = 1; // 每10帧搜寻一次
        private int _frameOffset;

        public override void _Ready()
        {
            base._Ready();
            _curHp = MaxHp;
            LogicCurHp = _curHp;
            _atkRangeSq = AtkRange * AtkRange;
            animPlayer.Play("Move");
            _curState = ZombieState.Chase;
            _hpMaterial = HpBarMesh.GetActiveMaterial(0).Duplicate() as ShaderMaterial;
            _hpMaterial.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpMaterial);
            _frameOffset = Math.Abs(GetHashCode()) % _searchInterval;
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

            if (Engine.GetFramesDrawn() % _searchInterval == _frameOffset)
            {
                _targetBuilding = FindNearestBuilding();
            }
            
            if (_targetBuilding == null || !IsInstanceValid(_targetBuilding))
                return;

            Vector3 targetPos = _targetBuilding.GlobalPosition;
            float distToTargetSq = GlobalPosition.DistanceSquaredTo(targetPos);

            float totalRange = AtkRange + _targetBuilding.ModelRadius;
            float totalRangeSq = totalRange * totalRange;
            if (distToTargetSq <= totalRangeSq)
            {
                animPlayer.Play("Atk");
                animPlayer.AnimationFinished += OnAtkAnimFinish;
                _targetBuilding.TakeDmg(23);
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

        private Tween _bufferTween;
        public override void TakeDmg(float damage)
        {
            _curHp -= damage;
            if (_curHp < 0)
            {
                _curHp = 0;
            }
            float healthRatio = _curHp / MaxHp;
            _hpMaterial.SetShaderParameter("health_value", healthRatio);
            if (_bufferTween != null && _bufferTween.IsRunning())
            {
                _bufferTween.Kill(); // 如果上次动画没播完，停掉它重新播
            }
            _bufferTween = CreateTween();
            _bufferTween.SetParallel(false);
            _bufferTween.TweenInterval(0.2f);
            _bufferTween.TweenProperty(_hpMaterial, "shader_parameter/buffer_value", healthRatio, 0.4f)
                        .SetTrans(Tween.TransitionType.Sine)
                        .SetEase(Tween.EaseType.Out);
        }

        private void OnAtkAnimFinish(StringName name)
        {
            _curState = ZombieState.Chase;
            animPlayer.AnimationFinished -= OnAtkAnimFinish;
        }
    }
}

