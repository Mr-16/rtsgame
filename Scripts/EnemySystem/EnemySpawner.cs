using Godot;
using RtsGame.Scripts.EnemySystem;
using RtsGame.Scripts.Global;
using System.Collections.Generic;

public partial class EnemySpawner : Node3D
{
    [Export] public PackedScene ZombiePs;
    [Export] public int InitialPoolSize = 500;

    private NodePool _zombiePool;

    public override void _Ready()
    {
        // 初始化对象池
        _zombiePool = new NodePool(ZombiePs, this, InitialPoolSize);
    }

    

    // 此时你的 SpawnRectangle 和 SpawnCircle 内部逻辑不变，
    // 只需将 InstantiateEnemy 替换为 SpawnFromPool 即可
    public void SpawnRectangle(EnemeyType type, Vector3 worldPos, int width, int height, int space)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xOffset = (x - (width - 1) / 2.0f) * space;
                float zOffset = (z - (height - 1) / 2.0f) * space;
                SpawnFromPool(type, worldPos + new Vector3(xOffset, 0, zOffset));
            }
        }
    }

    // 修改后的实例化方法
    private void SpawnFromPool(EnemeyType type, Vector3 position)
    {
        Node3D enemy = null;
        if (type == EnemeyType.Zombie)
        {
            enemy = _zombiePool.Get();
        }

        if (enemy != null)
        {
            enemy.GlobalPosition = position;
            // 如果你的单位有初始化方法，可以在这里调用
            // if (enemy is IEnemy e) e.Reset();
        }
    }
}