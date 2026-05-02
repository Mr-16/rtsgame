using Godot;
using RtsGame.Scripts.EnemySystem;
using System;

public partial class MagicTowerBall : Node3D
{
    [Export] private float _moveSpeed = 20f;

    private EnemyBase _target;
    private Vector3 _lastTargetPosition;
    private int _damage;
    private bool _targetLost = false;

    public override void _PhysicsProcess(double delta)
    {
        // 1. 更新目标位置或处理目标丢失
        if (IsInstanceValid(_target))
        {
            _lastTargetPosition = _target.GlobalPosition;
        }
        else
        {
            _targetLost = true;
        }

        // 2. 计算位移
        Vector3 currentPos = GlobalPosition;
        Vector3 direction = _lastTargetPosition - currentPos;
        float distanceSquared = direction.LengthSquared();

        // 3. 到达判定 (使用较小的阈值避免抖动)
        if (distanceSquared < 0.5f)
        {
            OnReachDestination();
            return;
        }

        // 4. 移动
        GlobalPosition += direction.Normalized() * _moveSpeed * (float)delta;

        // 可选：让球始终朝向飞行方向
        if (direction != Vector3.Zero)
        {
            LookAt(GlobalPosition + direction, Vector3.Up);
        }
    }

    private void OnReachDestination()
    {
        // 如果目标还在，且是因为到达而触发，则造成伤害
        if (!_targetLost && IsInstanceValid(_target))
        {
            _target.TakeDmg(_damage);
        }

        // 无论是否击中，到达最后位置后都销毁
        QueueFree();
    }

    public void Init(EnemyBase target, int damage)
    {
        _target = target;
        _damage = damage;
        if (IsInstanceValid(_target))
        {
            _lastTargetPosition = _target.GlobalPosition;
        }
    }
}