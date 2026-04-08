using Godot;
using RtsGame.Scripts;
using System;

public partial class MainBase : BuildingBase
{
    [Export] public float FlagRange = 10;
    public float FlagRangeSq;
    [Export] public MeshInstance3D RingMesh;
    private ShaderMaterial _ringMaterial;
    public override void _Ready()
	{
        GameManager.Instance.MainBaseList.Add(this);
        _ringMaterial = RingMesh.GetActiveMaterial(0) as ShaderMaterial;
        ShowFlagRing(false);
        FlagRangeSq = FlagRange * FlagRange;
    }

	public override void _Process(double delta)
	{
	}

    public void ShowFlagRing(bool isShow)
    {
        if (isShow)
        {
            RingMesh.Visible = true;
            RingMesh.Scale = new Vector3(FlagRange, 1.0f, FlagRange);
            _ringMaterial.SetShaderParameter("main_color", new Color(0.6f, 0.3f, 0.3f, 0.1f));
            _ringMaterial.SetShaderParameter("segment_count", 0.0f);
        }
        else
        {
            RingMesh.Visible = false;
        }
    }
}
