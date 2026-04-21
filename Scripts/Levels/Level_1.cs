using Godot;
using System;
using System.Collections.Generic;

public partial class Level_1 : Node
{
    //第一关的出兵逻辑 : 
    [Export] private EnemySpawner Spawner1;
    [Export] private EnemySpawner Spawner2;
    [Export] private EnemySpawner Spawner3;
    [Export] private EnemySpawner Spawner4;

    [Export] public double Time1 = 30;
    [Export] public double Time2 = 60;
    [Export] public double Time3 = 100;
    [Export] public double Time4 = 120;
    [Export] public double Time5 = 140;
    [Export] public double Time6 = 160;
    [Export] public double Time7 = 200;
    [Export] public double Time8 = 300;
    [Export] public double Time9 = 400;
    [Export] public double Time10 = 500;
    [Export] public double Time11 = 600;

    private double _curTimer = 0;

    public override void _Ready()
	{
        
    }

	public override void _Process(double delta)
	{
	}

    public override void _PhysicsProcess(double delta)
    {
        _curTimer += delta;
        if(_curTimer > 0)
        {
            //游戏开始
            Spawner1.CdTime = 10;
            Spawner2.CdTime = 10;
            Spawner3.CdTime = 10;
            Spawner4.CdTime = 10;
        }
        else if(_curTimer > Time1)
        {
            //第1波
            Spawner1.CdTime = 9;
            Spawner2.CdTime = 9;
            Spawner3.CdTime = 9;
            Spawner4.CdTime = 9;
        }
        else if (_curTimer > Time1)
        {
            //第2波
            Spawner1.CdTime = 8;
            Spawner2.CdTime = 8;
            Spawner3.CdTime = 8;
            Spawner4.CdTime = 8;
        }
        else if (_curTimer > Time1)
        {
            //第3波
            Spawner1.CdTime = 6;
            Spawner2.CdTime = 6;
            Spawner3.CdTime = 6;
            Spawner4.CdTime = 6;
        }
        else if (_curTimer > Time1)
        {
            //第4波
            Spawner1.CdTime = 3;
            Spawner2.CdTime = 3;
            Spawner3.CdTime = 3;
            Spawner4.CdTime = 3;
        }
        else if (_curTimer > Time1)
        {
            //第5波
            Spawner1.CdTime = 1f;
            Spawner2.CdTime = 1f;
            Spawner3.CdTime = 1f;
            Spawner4.CdTime = 1f;
        }
        else if (_curTimer > Time1)
        {
            //第6波
            Spawner1.CdTime = 0.5f;
            Spawner2.CdTime = 0.5f;
            Spawner3.CdTime = 0.5f;
            Spawner4.CdTime = 0.5f;
        }
        else if (_curTimer > Time1)
        {
            //第7波
            Spawner1.CdTime = 0.1f;
            Spawner2.CdTime = 0.1f;
            Spawner3.CdTime = 0.1f;
            Spawner4.CdTime = 0.1f;

        }
        else if (_curTimer > Time1)
        {
            //第8波

        }
        else if (_curTimer > Time1)
        {
            //第9波

        }
        else if (_curTimer > Time1)
        {
            //第10波

        }
    }

    
}
