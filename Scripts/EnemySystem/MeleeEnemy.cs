using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.EnemySystem;
using RtsGame.Scripts.Global;
using System;
using System.Collections.Generic;

public enum MeleeEnemyState
{
    Idle,
    Chase,
    Atk,
    Hurt,
    Death,
}

public partial class MeleeEnemy : EnemyBase
{
    //黄毛模板
    //全部都综合

    

    private float _idleRestTime = 0;
    private float _atkRangeSq;
    private MeleeEnemyState _curState;
    private IEnemyTarget _curTarget;
    private Tween _activeTween;
    
    

    public override void _Ready()
	{
		base._Ready();
        Type = EnemeyType.Enemy1;
        _curState = MeleeEnemyState.Idle;
        PlayTween();
        
    }

    public override void _PhysicsProcess(double delta)
    {
        //GD.Print(_curState);
        switch (_curState)
        {
            case MeleeEnemyState.Idle:
                UpdateIdle((float)delta);
                break;
            case MeleeEnemyState.Chase:
                UpdateChase((float)delta);
                break;
            case MeleeEnemyState.Atk:
                UpdateAtk((float)delta);
                break;
            case MeleeEnemyState.Hurt:
                UpdateHurt((float)delta);
                break;
            case MeleeEnemyState.Death:
                UpdateDeath((float)delta);
                break;
        }
    }

    private void UpdateIdle(float delta)
    {
        _idleRestTime -= delta;
        if(_idleRestTime > 0)
            return;

        _curTarget = FindNearestTarget();
        if (_curTarget != null && _curTarget.IsValid())
        {
            _curState = MeleeEnemyState.Chase;
            PlayTween();
            return;
        }
    }

    private void UpdateChase(float delta)
    {
        _curTarget = FindNearestTarget();
        if (_curTarget == null || _curTarget.IsValid() == false)
        {
            _curState = MeleeEnemyState.Idle;
            PlayTween();
            return;
        }

        Vector3 targetPos = _curTarget.GetPos();
        float distToTargetSq = GlobalPosition.DistanceSquaredTo(targetPos);
        float totalRange = AtkRange + _curTarget.GetModelRadius();
        float totalRangeSq = totalRange * totalRange;
        if (distToTargetSq <= totalRangeSq)
        {
            _curState = MeleeEnemyState.Atk;
            PlayTween();
            return;
        }

        Vector3 lookTarget = new Vector3(targetPos.X, GlobalPosition.Y, targetPos.Z);
        LookAt(lookTarget, Vector3.Up);
        Vector3 moveDir = (lookTarget - GlobalPosition).Normalized();
        GlobalPosition += moveDir * MoveSpeed * delta;
    }

    private void UpdateAtk(float delta)
    {
        
    }

    private void UpdateHurt(float delta)
    {
        
    }

    private void UpdateDeath(float delta)
    {
        
    }

    public override void TakeDmg(int damage)
    {
        if (_curState == MeleeEnemyState.Death) 
            return;
        _curHp -= damage;
        base.TakeDmg(damage);
        if (_curHp <= 0)
        {
            _curHp = 0;
            _curState = MeleeEnemyState.Death;
            PlayTween();
        }
    }

    private IEnemyTarget FindNearestTarget()
    {
        List<IEnemyTarget> targetList = GameManager.Instance.EnemyTargetList;
        if (targetList == null || targetList.Count == 0) 
            return null;
        IEnemyTarget nearest = null;
        float minDistance = float.MaxValue;
        foreach (IEnemyTarget target in targetList)
        {
            if (target.IsValid() == false)
                continue;
            float dist = GlobalPosition.DistanceSquaredTo(target.GetPos());
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = target;
            }
        }
        return nearest;
    }

    // 状态切换入口
    private void PlayTween()
    {
        if (_activeTween != null)
        {
            _activeTween.Kill();
            _activeTween = null;
        }
        
        _activeTween = CreateTween();
        switch (_curState)
        {
            case MeleeEnemyState.Idle:
                _activeTween.SetLoops();// 循环呼吸动画
                _activeTween.TweenProperty(ModelNode, "scale", new Vector3(1.05f, 0.95f, 1.05f), 0.6f)
                    .SetTrans(Tween.TransitionType.Sine);
                _activeTween.TweenProperty(ModelNode, "scale", Vector3.One, 0.6f)
                    .SetTrans(Tween.TransitionType.Sine);
                break;

            case MeleeEnemyState.Chase:
                _activeTween.SetLoops();// 循环呼吸动画
                _activeTween.Parallel().TweenProperty(ModelNode, "rotation:x", Mathf.DegToRad(15), 0.2f);
                _activeTween.TweenProperty(ModelNode, "scale", new Vector3(0.9f, 1.15f, 0.9f), 0.15f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                _activeTween.TweenProperty(ModelNode, "scale", new Vector3(1.1f, 0.85f, 1.1f), 0.15f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
                break;

            case MeleeEnemyState.Atk:
                _activeTween.TweenProperty(ModelNode, "position:z", 0.3f, 0.1f).AsRelative();
                var strikeTween = _activeTween.TweenProperty(ModelNode, "position:z", -0.5f, 0.05f).AsRelative().SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                strikeTween.Finished += () =>
                {
                    _curTarget.TakeDmg(Damage);
                    GD.Print("Hit!");
                };
                _activeTween.TweenProperty(ModelNode, "position:z", 0.2f, 0.1f).AsRelative();
                _activeTween.Finished += () =>
                {
                    _idleRestTime = AtkCdTime;
                    _curState = MeleeEnemyState.Idle;
                    PlayTween();
                };
                break;

            case MeleeEnemyState.Hurt:
                break;

            case MeleeEnemyState.Death:
                _activeTween.SetParallel(true);
                _activeTween.TweenProperty(ModelNode, "position:y", -1.5f, 1.0f).AsRelative();
                _activeTween.TweenProperty(ModelNode, "scale", Vector3.Zero, 1.0f);
                _activeTween.Finished += () =>
                {
                    GD.Print("death finished");
                    QueueFree();
                    GameManager.Instance.EnemyList.Remove(this);
                };
                break;
        }
    }


}
