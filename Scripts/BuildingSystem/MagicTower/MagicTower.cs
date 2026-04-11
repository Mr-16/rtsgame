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
    [Export] public MeshInstance3D RingMesh;
    private ShaderMaterial _ringMaterial;

    public override void _Ready()
	{
        base._Ready();
        _atkRangeSq = AtkRange * AtkRange;
        _curState = MagicTowerState.Idle;
        _ringMaterial = RingMesh.GetActiveMaterial(0) as ShaderMaterial;
        ShowRing(false);
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
        PlayBounceAnimation();
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

    public void ShowRing(bool isShow)
    {
        if (isShow)
        {
            RingMesh.Visible = true;
            RingMesh.Scale = new Vector3(AtkRange, 1.0f, AtkRange);
            _ringMaterial.SetShaderParameter("main_color", new Color(0.5f, 1f, 0.3f, 0.1f));
            _ringMaterial.SetShaderParameter("segment_count", 10f);
        }
        else
        {
            RingMesh.Visible = false;
        }
    }
    public override void SetSelected(bool isSelected)
    {
        base.SetSelected(isSelected);
        ShowRing(isSelected);
    }
}
