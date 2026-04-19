using Godot;
using RtsGame.Scripts;
using System;

public partial class MainBase : BuildingBase
{
    [Export] public float BuildRange = 10;
    public float FlagRangeSq;
    [Export] public MeshInstance3D BuildRingMesh;
    private ShaderMaterial _ringMaterial;
    public override void _Ready()
	{
        base._Ready();
        GameManager.Instance.MainBaseList.Add(this);
        _ringMaterial = BuildRingMesh.GetActiveMaterial(0) as ShaderMaterial;
        ShowBuildRing(false);
        FlagRangeSq = BuildRange * BuildRange;
    }

	public override void _Process(double delta)
	{
	}

    public void ShowBuildRing(bool isShow)
    {
        BuildRingMesh.Visible = isShow;
    }

    public override void SetSelected(bool isSelected)
    {
        base.SetSelected(isSelected);
        ShowBuildRing(isSelected);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        GameManager.Instance.MainBaseList.Remove(this);
    }
}
