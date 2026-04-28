using Godot;
using RtsGame.Scripts;
using System;
using System.Collections.Generic;

public partial class Level_1 : Node3D
{
    //第一关的出兵逻辑 : 
    [Export] private EnemySpawner Spawner1;
    [Export] private EnemySpawner Spawner2;
    [Export] private EnemySpawner Spawner3;
    [Export] private EnemySpawner Spawner4;

    [Export] public double Time1 = 60;
    [Export] public double Time2 = 120;
    [Export] public double Time3 = 180;
    [Export] public double Time4 = 240;
    [Export] public double Time5 = 300;
    [Export] public double Time6 = 360;
    [Export] public double Time7 = 420;
    [Export] public double Time8 = 480;
    [Export] public double Time9 = 540;
    [Export] public double Time10 = 600;
    [Export] public Node3D BuildGridMesh;

    private double _curTimer = 0;

    public override void _Ready()
	{
        GameManager.Instance.Level1 = this;
        BuildGridMesh.Visible = false;
    }

	public override void _Process(double delta)
	{
	}

    public override void _PhysicsProcess(double delta)
    {
        _curTimer += delta;

        TimeSpan time = TimeSpan.FromSeconds(_curTimer);
        GameManager.Instance.Player.SetTimeLb(time.ToString(@"mm\:ss"));

        float targetCd = 100;

        // 倒序判断：从最晚的时间点开始
        //if (_curTimer > Time9) { targetCd = 0.1f; }
        //else if (_curTimer > Time6) { targetCd = 0.3f; }
        //else if (_curTimer > Time6) { targetCd = 0.5f; }
        //else if (_curTimer > Time6) { targetCd = 0.7f; }
        //else if (_curTimer > Time5) { targetCd = 0.9f; }
        //else if (_curTimer > Time4) { targetCd = 1.5f; }
        //else if (_curTimer > Time3) { targetCd = 3.0f; }
        //else if (_curTimer > Time2) { targetCd = 7.0f; }
        //else if (_curTimer > Time1) { targetCd = 10.0f; }
        //else { targetCd = 10.0f; } // 0 到 Time1 之间

        // 统一赋值
        Spawner1.CdTime = targetCd;
        Spawner2.CdTime = targetCd;
        Spawner3.CdTime = targetCd;
        Spawner4.CdTime = targetCd;
    }


}
