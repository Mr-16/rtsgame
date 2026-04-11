using Godot;
using RtsGame.Scripts;
using System;

public partial class Flag : BuildingBase
{
    [Export] public float BuildingRange = 10;
    public float BuildingRangeSq;
    [Export] public MeshInstance3D RingMesh;
    private ShaderMaterial _ringMaterial;

    public override void _Ready()
	{
        base._Ready();
        GameManager.Instance.FlagList.Add(this);
        _ringMaterial = RingMesh.GetActiveMaterial(0) as ShaderMaterial;
        ShowBuildingRing(false);
        BuildingRangeSq = BuildingRange * BuildingRange;
    }

	public override void _Process(double delta)
	{
	}

    public void ShowBuildingRing(bool isShow)
    {
        if (isShow)
        {
            RingMesh.Visible = true;
            RingMesh.Scale = new Vector3(BuildingRange, 1.0f, BuildingRange);
            _ringMaterial.SetShaderParameter("main_color", new Color(0.0f, 0.7f, 1.0f, 0.1f));
            _ringMaterial.SetShaderParameter("segment_count", 0.0f);
        }
        else
        {
            RingMesh.Visible = false;
        }
    }

    public override void SetSelected(bool isSelected)
    {
        base.SetSelected(isSelected);
        ShowBuildingRing(isSelected);
    }
}
