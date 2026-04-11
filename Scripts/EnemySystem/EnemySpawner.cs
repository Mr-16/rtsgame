using Godot;
using RtsGame.Scripts.EnemySystem;
using RtsGame.Scripts.Global;
using System;

public partial class EnemySpawner : Node3D
{
    [Export] public EnemeyType Type;
    [Export] public float CdTime = 3.0f;
    [Export] public PackedScene ZombiePs;

    // --- 半径控制 ---
    [Export] public float SpawnRadius = 50.0f;

    private float _curTimer = 0;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize(); // 确保每次运行的随机种子不同
    }

    public override void _PhysicsProcess(double delta)
    {
        _curTimer += (float)delta;
        if (_curTimer < CdTime)
            return;

        _curTimer = 0;

        if (Type == EnemeyType.Zombie)
        {
            SpawnZombie();
        }
    }

    private void SpawnZombie()
    {
        if (ZombiePs == null) return;

        // 1. 实例化
        Zombie zombie = ZombiePs.Instantiate<Zombie>();

        // 2. 计算圆周随机偏移
        // 随机角度 (0 到 360度)
        float angle = _rng.RandfRange(0, Mathf.Tau);
        // 随机距离 (0 到 半径)
        // 注意：为了让分布更均匀，可以使用 Mathf.Sqrt(_rng.Randf()) * SpawnRadius
        float distance = _rng.RandfRange(0, SpawnRadius);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0, // 假设在水平面生成，高度保持一致
            Mathf.Sin(angle) * distance
        );

        // 3. 添加到场景并设置全球坐标
        GetTree().CurrentScene.AddChild(zombie);
        zombie.GlobalPosition = GlobalPosition + offset;
    }
}