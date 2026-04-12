using Godot;
using System;

namespace RtsGame.Scripts
{
    public partial class BuildingBase : Node3D
    {
        [Export] public float ModelRadius = 2;
        [Export] public float MaxHp = 100;
        protected float _curHp;
        [Export] private MeshInstance3D HpBarMesh;
        private ShaderMaterial _hpMaterial;
        private Tween _bounceTween; // 保存当前的 Tween，防止多次点击导致动画冲突
        public override void _Ready()
        {
            GameManager.Instance.BuildingList.Add(this);
            _curHp = MaxHp;
            _hpMaterial = HpBarMesh.GetActiveMaterial(0).Duplicate() as ShaderMaterial;
            _hpMaterial.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpMaterial);
        }

        public virtual void SetSelected(bool isSelected)
        {
            if (isSelected)
            {
                PlayBounceAnimation();
            }
        }

        public void PlayBounceAnimation()
        {
            if (_bounceTween != null && _bounceTween.IsRunning())
            {
                _bounceTween.Kill();
            }
            Scale = Vector3.One;
            _bounceTween = GetTree().CreateTween();
            _bounceTween.SetTrans(Tween.TransitionType.Back);
            _bounceTween.SetEase(Tween.EaseType.Out);
            _bounceTween.TweenProperty(this, "scale", new Vector3(1.15f, 1.15f, 1.15f), 0.1f);
            _bounceTween.TweenProperty(this, "scale", Vector3.One, 0.2f);
        }

        public virtual void TakeDmg(float damage)
        {
            _curHp -= damage;
            if (_curHp < 0)
            {
                _curHp = 0;
                QueueFree();
            }
            _hpMaterial.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpMaterial);
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            GameManager.Instance.BuildingList.Remove(this);
            GameManager.Instance.BuildingGridMap.Remove(GlobalPosition);
        }
    }
}