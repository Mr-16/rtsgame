using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.EnemySystem;
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class MagicTowerBall : Node3D
{
    [Export] private float _moveSpeed = 20f;
    private int _targetEnemyIndex;
    private int _damage;

    public override void _PhysicsProcess(double delta)
    {
        //if (GameManager.Instance.EnemyManager.DataList[_targetEnemyIndex].State == EnemyState.Death)
        //{
        //    QueueFree();
        //    return;
        //}
        //Vector3 targetPos = GameManager.Instance.EnemyManager.DataList[_targetEnemyIndex].Position;
        //Vector3 currentPos = GlobalPosition;
        //Vector3 direction = targetPos - currentPos;
        //float distanceSquared = direction.LengthSquared();
        //if (distanceSquared < 1.0f)
        //{
        //    GameManager.Instance.EnemyManager.TakeDmg(_targetEnemyIndex, _damage);
        //    QueueFree();
        //    return;
        //}

        //GlobalPosition += direction.Normalized() * _moveSpeed * (float)delta;
    }

    public void Init(int targetEnemyIndex, int damage)
    {
        _targetEnemyIndex = targetEnemyIndex;
        _damage = damage;
    }

}