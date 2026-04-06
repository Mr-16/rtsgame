using Godot;
using RtsGame.Scripts.Global;
using System;

public partial class BuildingItemBase : Node3D
{
    [Export] public BuildingType Type;

    [Export] public Area3D Area;

    private Tween _hoverTween;
    private Vector3 _originalScale = Vector3.One;
    private float _scale = 1.3f;// 可以自行调整放大倍数
    private Vector3 _hoverScale;

    public override void _Ready()
    {
        Area.MouseEntered += OnMouseEntered;
        Area.MouseExited += OnMouseExited;
        _hoverScale = new Vector3(_scale, _scale, _scale);
        _originalScale = Scale;  // 保存初始缩放
    }

    private void OnMouseEntered()
    {
        GD.Print("OnMouseEntered");

        KillCurrentTween();

        _hoverTween = CreateTween();

        // 鼠标进入：快速弹一下放大，并保持放大状态
        _hoverTween
            .TweenProperty(this, "scale", _hoverScale, 0.15f)
            .SetTrans(Tween.TransitionType.Elastic)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnMouseExited()
    {
        GD.Print("OnMouseExited");

        KillCurrentTween();

        _hoverTween = CreateTween();

        // 鼠标移出：快速弹一下恢复原始大小
        _hoverTween
            .TweenProperty(this, "scale", _originalScale, 0.18f)
            .SetTrans(Tween.TransitionType.Elastic)
            .SetEase(Tween.EaseType.Out);
    }

    private void KillCurrentTween()
    {
        if (_hoverTween != null && _hoverTween.IsRunning())
        {
            _hoverTween.Kill();
        }
    }

    // 可选：节点被销毁时清理 Tween
    public override void _ExitTree()
    {
        KillCurrentTween();
    }
}
