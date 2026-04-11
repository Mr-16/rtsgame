using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.EnemySystem;
using System;

public enum MagicTowerState
{
    Building,
    Idle,
    Atk,
    CD,
    Death,
}

public partial class MagicTower : BuildingBase
{
    //todo
    //选中后可以显示攻击范围
    [Export] public PackedScene BallPs;
    [Export] public float AtkRange = 20;
    [Export] public float Damage = 57;
    [Export] public float CdTime = 2f;
    private float _atkRangeSq;
    private EnemyBase _curTargetEnemy;
    private MagicTowerState _curState;


	public override void _Ready()
	{
        base._Ready();
        _atkRangeSq = AtkRange * AtkRange;
        _curState = MagicTowerState.Idle;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        switch (_curState)
        {
            case MagicTowerState.Building:
                UpdateBuilding((float)delta);
                break;
            case MagicTowerState.Idle:
                UpdateIdle((float)delta);
                break;
            case MagicTowerState.Atk:
                UpdateAtk((float)delta);
                break;
            case MagicTowerState.CD:
                UpdateCD((float)delta);
                break;
            case MagicTowerState.Death:
                UpdateDeath((float)delta);
                break;
        }

        
    }


    private void UpdateBuilding(float delta)
    {
        
    }
    private void UpdateIdle(float delta)
    {
        if(_curTargetEnemy == null || IsInstanceValid(_curTargetEnemy) == false)
            _curTargetEnemy = FindNearestEnemy();
        if (_curTargetEnemy == null || IsInstanceValid(_curTargetEnemy) == false)
            return;
        if (GlobalPosition.DistanceSquaredTo(_curTargetEnemy.GlobalPosition) <= _atkRangeSq)
        {
            _curState = MagicTowerState.Atk;
        }
    }
    private void UpdateAtk(float delta)
    {
        GD.Print("Atk!!!");
        MagicTowerBall ball = BallPs.Instantiate<MagicTowerBall>();
        GetTree().CurrentScene.AddChild(ball);
        ball.Position = GlobalPosition;
        ball.Init(_curTargetEnemy, Damage);
        _curState = MagicTowerState.CD;
    }

    
    private float CdTimer = 0;
    private void UpdateCD(float delta)
    {
        CdTimer += delta;
        if(CdTimer > CdTime)
        {
            CdTimer = 0;
            _curState = MagicTowerState.Idle;
        }
    }
    private void UpdateDeath(float delta)
    {
        
    }

    private EnemyBase FindNearestEnemy()
    {
        var enemys = GameManager.Instance.EnemyList;
        if (enemys == null || enemys.Count == 0) return null;

        EnemyBase nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemys)
        {
            if (!IsInstanceValid(enemy)) continue;

            float dist = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
