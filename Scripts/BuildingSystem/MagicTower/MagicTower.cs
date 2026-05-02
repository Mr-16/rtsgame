using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.EnemySystem;
using System;

public enum MagicTowerState
{
    Building,
    Idle,
    Atk,
    Death,
}

public partial class MagicTower : BuildingBase
{
    [Export] public PackedScene BallPs;
    [Export] public float AtkRange = 25;
    [Export] public int Damage = 38;
    [Export] public float CdTime = 0.3f;
    private float idleRestTime = 0;
    private float _atkRangeSq;
    private EnemyBase _curTarget;
    private MagicTowerState _curState;
    [Export] public MeshInstance3D AtkRingMesh;

    private int _searchInterval = 1; // 每10帧搜寻一次
    private int _frameOffset;

    public override void _Ready()
	{
        base._Ready();
        _atkRangeSq = AtkRange * AtkRange;
        _curState = MagicTowerState.Idle;
        ShowRing(false);
        _frameOffset = Math.Abs(GetHashCode()) % _searchInterval;
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
        idleRestTime -= delta;
        if (idleRestTime > 0)
            return;

        if (Engine.GetFramesDrawn() % _searchInterval == _frameOffset)
        {
            _curTarget = FindTargetEnemy();
        }
        if (_curTarget != null && IsInstanceValid(_curTarget))
        {
            _curState = MagicTowerState.Atk;
            return;
        }
        
    }
    private void UpdateAtk(float delta)
    {
        if (_curTarget == null || IsInstanceValid(_curTarget) == false)
        {
            _curState = MagicTowerState.Idle;
            return;
        }

        //GD.Print("Atk!!!");
        if( _curTarget.LogicCurHp <= 0)
        {
            _curState = MagicTowerState.Idle;
            return;
        }
        _curTarget.LogicCurHp -= Damage;
        PlayBounceAnimation();

        MagicTowerBall ball = BallPs.Instantiate<MagicTowerBall>();
        GetTree().CurrentScene.AddChild(ball);
        Vector3 newPos = GlobalPosition;
        newPos.Y += 2.0f;
        ball.GlobalPosition = newPos;
        ball.Init(_curTarget, Damage);

        idleRestTime = CdTime;
        _curState = MagicTowerState.Idle;
    }

    private void UpdateDeath(float delta)
    {
        
    }

    private EnemyBase FindTargetEnemy()
    {
        var enemys = GameManager.Instance.EnemyList;
        if (enemys == null || enemys.Count == 0) return null;

        EnemyBase nearest = null;
        float minDistance = _atkRangeSq; // 初始值设为攻击距离平方，这样只有小于它的才会被选中

        foreach (var enemy in enemys)
        {
            if (!IsInstanceValid(enemy)) continue;
            if (enemy.LogicCurHp <= 0) continue;

            float dist = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (dist < minDistance) // 只有进入射程的才会被考虑
            {
                minDistance = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    public void ShowRing(bool isShow)
    {
        AtkRingMesh.Visible = isShow;
    }
    public override void SetSelected(bool isSelected)
    {
        base.SetSelected(isSelected);
        ShowRing(isSelected);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }
}
