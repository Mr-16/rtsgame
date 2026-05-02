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
        [Export] public int MaxHp = 100;
        [Export] public float MoveSpeed = 2.0f;
        [Export] public float AtkRange = 1.0f;
        [Export] public Node3D ModelNode;
        [Export] public float AtkCdTime = 1.5f;
        [Export] public int Damage = 8;
        [Export] public MeshInstance3D HpBarMesh;
        private ShaderMaterial _hpBarMat;

        protected int _curHp;
        public int LogicCurHp;
        private Tween _hurtTween;
        private List<MeshInstance3D> _meshParts = new List<MeshInstance3D>();

        public override void _Ready()
        {
            GameManager.Instance.EnemyList.Add(this);
            _curHp = MaxHp;
            LogicCurHp = _curHp;
            //HpBarMesh.Visible = false;
            FindMeshesRecursive(ModelNode);
            _hpBarMat = HpBarMesh.GetActiveMaterial(0).Duplicate() as ShaderMaterial;
            _hpBarMat.SetShaderParameter("health_value", _curHp / MaxHp);
            HpBarMesh.SetSurfaceOverrideMaterial(0, _hpBarMat);
        }

        private Tween _bufferTween;
        public virtual void TakeDmg(int damage)
        {
            HpBarMesh.Visible = true;
            float healthRatio = (float)_curHp / (float)MaxHp;
            _hpBarMat.SetShaderParameter("health_value", healthRatio);
            if (_bufferTween != null && _bufferTween.IsRunning())
            {
                _bufferTween.Kill(); // 如果上次动画没播完，停掉它重新播
            }
            _bufferTween = CreateTween();
            _bufferTween.SetParallel(false);
            _bufferTween.TweenInterval(0.2f);
            _bufferTween.TweenProperty(_hpBarMat, "shader_parameter/buffer_value", healthRatio, 0.4f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            PlayHitFlash();
        }


        private void FindMeshesRecursive(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                _meshParts.Add(mesh);
            }

            foreach (Node child in node.GetChildren())
            {
                FindMeshesRecursive(child);
            }
        }
        private void PlayHitFlash()
        {
            if (_hurtTween != null && _hurtTween.IsRunning())
            {
                _hurtTween.Kill();
            }
            _hurtTween = CreateTween();
            SetAllMeshesFlash(1.0f);
            _hurtTween.TweenMethod(Callable.From<float>(SetAllMeshesFlash), 1.0f, 0.0f, 0.3f)
                 .SetTrans(Tween.TransitionType.Quint)
                 .SetEase(Tween.EaseType.Out);
        }

        private void SetAllMeshesFlash(float value)
        {
            foreach (var mesh in _meshParts)
            {
                mesh.SetInstanceShaderParameter("flash_modifier", value);
            }
        }
    }
}
