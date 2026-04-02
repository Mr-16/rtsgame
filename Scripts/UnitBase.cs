using Godot;
using RtsGame.Scripts;
using System;

public partial class UnitBase : Node3D
{
    [Export] protected float MoveSpeed = 20.0f;
    [Export] protected float RotationSpeed = 10.0f; // 转向感，数值越大转得越快
    [Export] private MeshInstance3D _selctedMark;
    [Export] protected NavigationAgent3D NaviAgent;
    [Export] protected AnimationPlayer AnimPlayer;
    public Player OwnerPlayer;

    public override void _Ready()
    {
        GameManager.Instance.UnitList.Add(this);
        _selctedMark.Visible = false;

        
    }

    public override void _PhysicsProcess(double delta)
    {
        
    }

    public virtual void SetTarget(TargetType type, Vector3 pos)
    {
    }

    public void SetSelected(bool isSelected)
    {
        _selctedMark.Visible = isSelected;
    }
}