using Godot;
using RtsGame.Scripts.Global;
using System;

public partial class BuildingItemBase : Node3D
{
    [Export] public BuildingType Type;
    [Export] public Area3D Area;
    [Export] public Label3D PriceLb;
    [Export] public int Price = 100;

    private Tween tween;
    private Vector3 _originalScale = Vector3.One;
    private float _scale = 1.3f;// 可以自行调整放大倍数
    private Vector3 _hoverScale;

    public override void _Ready()
    {
        Area.MouseEntered += OnMouseEntered;
        Area.MouseExited += OnMouseExited;
        _hoverScale = new Vector3(_scale, _scale, _scale);
        _originalScale = Scale;  // 保存初始缩放
        PriceLb.Text = $"{Price}Gold";
        PriceLb.FontSize = 150;
    }

    private void OnMouseEntered()
    {
        GD.Print("OnMouseEntered");
        KillCurrentTween();
        tween = CreateTween();
        tween.TweenProperty(this, "scale", _hoverScale, 0.15f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    }

    private void OnMouseExited()
    {
        GD.Print("OnMouseExited");
        KillCurrentTween();
        tween = CreateTween();
        tween.TweenProperty(this, "scale", _originalScale, 0.18f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    }

    private void KillCurrentTween()
    {
        if (tween != null && tween.IsRunning())
        {
            tween.Kill();
        }
    }

    public void PlayBounceAnimation()
    {
        if (tween != null && tween.IsRunning())
        {
            tween.Kill();
        }
        Scale = Vector3.One;
        tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector3(1.15f, 1.15f, 1.15f), 0.1f);
        tween.TweenProperty(this, "scale", Vector3.One, 0.2f);
    }

    public override void _ExitTree()
    {
        KillCurrentTween();
    }
}
