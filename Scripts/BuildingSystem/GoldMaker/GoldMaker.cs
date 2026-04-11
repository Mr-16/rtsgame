using Godot;
using RtsGame.Scripts;
using System;

public partial class GoldMaker : Node3D
{
    [Export] public Label3D goldLb;
    [Export] public float MakeGoldDuration = 5.0f;     // 生产周期
    [Export] public int GoldPerTimeMin = 1;
    [Export] public int GoldPerTimeMax = 10;

    // 新增：可调节的动画参数（推荐默认值）
    [Export] public float FloatUpDistance = 2.5f;      // 向上飘多高
    [Export] public float PopDuration = 1.4f;          // 整个动画持续时间
    [Export] public Color StartColor = new Color(1.0f, 0.9f, 0.3f); // 亮金色

    private float _makeGoldTimer = 0;

    public override void _Ready()
    {
        base._Ready();
        if (goldLb != null)
        {
            goldLb.Visible = false;
            // 确保初始状态干净（可选）
            goldLb.Modulate = Colors.White;
            goldLb.Scale = Vector3.One;
        }
    }

    public override void _Process(double delta)
    {
        _makeGoldTimer += (float)delta;

        if (_makeGoldTimer >= MakeGoldDuration)
        {
            GenerateGold();
            _makeGoldTimer = 0;
        }
    }

    private void GenerateGold()
    {
        int gainedGold = GD.RandRange(GoldPerTimeMin, GoldPerTimeMax);

        if (goldLb != null)
        {
            goldLb.Text = $"+{gainedGold} Gold!";
            ShowFloatingText(gainedGold);
        }

        GameManager.Instance.Player.TakeGoldCount(gainedGold);
        //GD.Print($"金矿机产出了 {gainedGold} 金币");
    }

    private async void ShowFloatingText(int gainedGold)
    {
        if (goldLb == null) return;

        // 1. 动态实例化一个新的 Label3D
        var floatingLabel = (Label3D)goldLb.Duplicate();
        floatingLabel.Text = $"+{gainedGold}Gold!";
        floatingLabel.Visible = true;

        // 把新 Label 添加到场景中（建议加到金矿自己身上，或者加到世界根节点）
        AddChild(floatingLabel);                    // 加到当前 GoldMaker 上
                                                    // 或者： GetTree().CurrentScene.AddChild(floatingLabel);  // 加到世界根节点

        // 初始状态
        floatingLabel.Scale = new Vector3(0.2f, 0.2f, 0.2f);
        floatingLabel.Modulate = StartColor;
        floatingLabel.Position = new Vector3(0, 4f, 0);   // 根据你的模型调整起始高度

        var tween = CreateTween();
        tween.SetParallel();

        Vector3 targetPos = floatingLabel.Position + new Vector3(0, FloatUpDistance, 0);

        tween.TweenProperty(floatingLabel, "scale", new Vector3(1.8f, 1.8f, 1.8f), 0.25f)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        tween.TweenProperty(floatingLabel, "scale", Vector3.One, 0.4f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out)
             .SetDelay(0.15f);

        tween.TweenProperty(floatingLabel, "position", targetPos, PopDuration)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        tween.TweenProperty(floatingLabel, "modulate", new Color(1.0f, 0.85f, 0.4f), 0.6f)
             .SetDelay(0.1f);

        tween.TweenProperty(floatingLabel, "modulate:a", 0.0f, 0.5f)
             .SetDelay(PopDuration - 0.6f);

        // 动画结束后自动销毁
        await ToSignal(tween, "finished");
        floatingLabel.QueueFree();
    }
}