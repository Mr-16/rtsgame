using Godot;
using RtsGame.Scripts;
using System;

public partial class MainBasePreview : BuildingPreviewBase
{
    [Export] MeshInstance3D meshInstance;
    private Color defaultColor;
    private StandardMaterial3D material;
    public override void _Ready()
	{
        if (meshInstance.Mesh != null && meshInstance.Mesh.GetSurfaceCount() > 0)
        {
            material = meshInstance.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
            if (material != null)
            {
                defaultColor = material.AlbedoColor;
            }
        }
    }

	public override void _Process(double delta)
	{
	}

    public override void SetCanPlace(bool canPlace)
    {
        if(canPlace)
        {
            material.AlbedoColor = defaultColor; // 红色
        }
        else
        {
            material.AlbedoColor = new Color(1.0f, 0.0f, 0.0f, 0.5f); // 红色
        }
    }
}
