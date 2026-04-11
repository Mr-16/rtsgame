using Godot;
using RtsGame.Scripts.EnemySystem;
using System;

public partial class MagicTowerBall : Node3D
{
    [Export] private float _moveSpeed = 20f;
    private EnemyBase _targetEnemy;
    private float _damage;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(_targetEnemy))
        {
            QueueFree();
            return;
        }
        Vector3 targetPos = _targetEnemy.GlobalPosition;
        Vector3 currentPos = GlobalPosition;
        Vector3 direction = targetPos - currentPos;
        float distanceSquared = direction.LengthSquared();
        if (distanceSquared < 1.0f)
        {
            _targetEnemy.TakeDmg(_damage);
            QueueFree();
            return;
        }

        GlobalPosition += direction.Normalized() * _moveSpeed * (float)delta;
    }

    public void Init(EnemyBase targetEnemy, float damage)
    {
        _targetEnemy = targetEnemy;
        _damage = damage;
    }

}