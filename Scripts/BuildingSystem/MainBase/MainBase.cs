using Godot;
using RtsGame.Scripts;
using System;

public partial class MainBase : BuildingBase
{
	public override void _Ready()
	{
        GameManager.Instance.MainBaseList.Add(this);
	}

	public override void _Process(double delta)
	{
	}
}
