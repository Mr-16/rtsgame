using Godot;
using System;
using System.Linq;
using RtsGame.Scripts;

public enum ZombieState
{
    Chase,
    Atk,
    Hurt,
    Death,
}

public partial class Zombie : Node3D
{
    [Export] public float MoveSpeed = 3.0f;
    [Export] public float AtkRange = 2.0f;
    [Export] public int MaxHp = 100;

    private float _atkRangeSq;
    private int _curHp;
    private ZombieState _curState = ZombieState.Chase;
    private BuildingBase _targetBuilding;

    public override void _Ready()
    {
        _curHp = MaxHp;
        _atkRangeSq = AtkRange * AtkRange;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 简单的死亡检查
        if (_curHp <= 0 && _curState != ZombieState.Death)
        {
            _curState = ZombieState.Death;
        }

        switch (_curState)
        {
            case ZombieState.Chase:
                UpdateChase((float)delta);
                break;
            case ZombieState.Atk:
                UpdateAtk((float)delta);
                break;
            case ZombieState.Hurt:
                UpdateHurt((float)delta);
                break;
            case ZombieState.Death:
                UpdateDeath((float)delta);
                break;
        }
    }



    private void UpdateChase(float delta)
    {
        _targetBuilding = FindNearestBuilding();
        //GD.Print("_targetBuilding : " + _targetBuilding);
        // 修正逻辑：如果目标无效，则返回
        if (_targetBuilding == null || !IsInstanceValid(_targetBuilding))
            return;

        Vector3 targetPos = _targetBuilding.GlobalPosition;
        float distToTargetSq = GlobalPosition.DistanceSquaredTo(targetPos);

        // 1. 检查是否进入攻击范围
        if (distToTargetSq <= _atkRangeSq)
        {
            _curState = ZombieState.Atk;
            return; // 进入攻击状态后不再执行移动和转向
        }

        Vector3 lookTarget = new Vector3(targetPos.X, GlobalPosition.Y, targetPos.Z);
        LookAt(lookTarget, Vector3.Up);
        Vector3 moveDir = (lookTarget - GlobalPosition).Normalized();
        GlobalPosition += moveDir * MoveSpeed * delta;
    }

    private void UpdateAtk(float delta)
    {
        //if (!IsInstanceValid(_targetBuilding))
        //{
        //    TransitionTo(ZombieState.Chase);
        //    return;
        //}

        //// 检查目标是否跑出了范围（可选）
        //float distToTarget = GlobalPosition.DistanceTo(_targetBuilding.GlobalPosition);
        //if (distToTarget > AtkRange + 0.5f) // 加一点缓冲防止状态抖动
        //{
        //    TransitionTo(ZombieState.Chase);
        //    return;
        //}

        // 这里编写攻击逻辑，例如扣除建筑血量
        // GD.Print("正在撕咬建筑...");
    }

    private void UpdateHurt(float delta)
    {
        //throw new NotImplementedException();
    }

    private void UpdateDeath(float delta)
    {
        //throw new NotImplementedException();
    }

    

    private BuildingBase FindNearestBuilding()
    {
        var buildings = GameManager.Instance.BuildingList;
        if (buildings == null || buildings.Count == 0) return null;

        BuildingBase nearest = null;
        float minDistance = float.MaxValue;

        foreach (var building in buildings)
        {
            if (!IsInstanceValid(building)) continue;

            float dist = GlobalPosition.DistanceSquaredTo(building.GlobalPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = building;
            }
        }
        return nearest;
    }

    //private void TransitionTo(ZombieState newState)
    //{
    //    if (_curState == newState) return;
    //    _curState = newState;

    //    // 可以在这里触发动画切换
    //    // _animPlayer.Play(newState.ToString());
    //}
}