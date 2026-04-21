using Godot;
using RtsGame.Scripts;
using System;

public partial class Flag : BuildingBase
{
    [Export] public float BuildRange = 10;
    public float BuildingRangeSq;
    [Export] public MeshInstance3D BuildRingMesh;

    public override void _Ready()
	{
        base._Ready();
        GameManager.Instance.FlagList.Add(this);
        ShowBuildingRing(false);
        BuildingRangeSq = BuildRange * BuildRange;
    }

	public override void _Process(double delta)
	{
	}

    public void ShowBuildingRing(bool isShow)
    {
        BuildRingMesh.Visible = isShow;
    }

    public override void SetSelected(bool isSelected)
    {
        base.SetSelected(isSelected);
        ShowBuildingRing(isSelected);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        GameManager.Instance.FlagList.Remove(this);
    }
}
