using Godot;
using RtsGame.Scripts;
using System;
using System.Collections.Generic;

public struct SlimeData
{
    public Vector3 Position;
    public int MaxHp;
    public int CurHp;
    public float MoveSpeed;
}

public partial class SlimeManager : Node3D
{
    [Export] public int Count = 500;
    [Export] public int UpdatesPerFrame = 10; // 每帧只允许 50 个单位寻找目标
    [Export] public MultiMeshInstance3D MultiMeshInstance;

    private SlimeData[] _enemies;
    private BuildingBase[] _targets; // 缓存每个单位当前的目标
    private int _updatePointer = 0;   // 轮询指针

    public override void _Ready()
    {
        _targets = new BuildingBase[Count];
        InitEnemies();
    }

    private void InitEnemies()
    {
        _enemies = new SlimeData[Count];
        MultiMeshInstance.Multimesh.InstanceCount = Count;

        for (int i = 0; i < Count; i++)
        {
            _enemies[i] = new SlimeData
            {
                Position = new Vector3((float)GD.RandRange(-250, 250), 0, (float)GD.RandRange(-250, 250)),
                MoveSpeed = 3.0f,
                MaxHp = 100,
                CurHp = 100
            };
            UpdateRenderer(i, Vector3.Zero);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;
        var buildings = GameManager.Instance.BuildingList;

        // --- 第一部分：分批更新目标 ---
        // 这一步最耗时，所以我们限制每帧只运行 UpdatesPerFrame 次
        for (int k = 0; k < UpdatesPerFrame; k++)
        {
            // 轮询索引，确保每个单位都能轮到
            _updatePointer = (_updatePointer + 1) % Count;

            if (_enemies[_updatePointer].CurHp > 0)
            {
                _targets[_updatePointer] = FindNearestBuilding(_updatePointer, buildings);
            }
        }

        // --- 第二部分：所有单位根据缓存的目标移动 ---
        // 移动和渲染相对开销较小，依然每帧执行以保证视觉平滑
        for (int i = 0; i < Count; i++)
        {
            if (_enemies[i].CurHp <= 0) continue;

            Vector3 moveVec = Vector3.Zero;
            BuildingBase target = _targets[i];

            // 检查目标是否依然有效（可能建筑已被摧毁）
            if (IsInstanceValid(target))
            {
                Vector3 dir = _enemies[i].Position.DirectionTo(target.GlobalPosition);
                moveVec = dir * _enemies[i].MoveSpeed * fDelta;
                _enemies[i].Position += moveVec;
            }

            UpdateRenderer(i, moveVec);
        }
    }


    private void UpdateRenderer(int index, Vector3 moveVec)
    {
        Transform3D t = Transform3D.Identity;
        t.Origin = _enemies[index].Position;

        // 处理旋转
        if (moveVec.LengthSquared() > 0.001f)
        {
            float angle = Mathf.Atan2(moveVec.X, moveVec.Z);
            t.Basis = new Basis(Vector3.Up, angle);
        }

        MultiMeshInstance.Multimesh.SetInstanceTransform(index, t);

        // 如果着色器需要额外数据（如动画偏置或方向）
        Vector3 normMove = moveVec.Normalized();
        MultiMeshInstance.Multimesh.SetInstanceCustomData(index, new Color(normMove.X, normMove.Z, 0, 0));
    }

    private BuildingBase FindNearestBuilding(int index, List<BuildingBase> buildings)
    {
        if (buildings == null || buildings.Count == 0) return null;

        BuildingBase nearest = null;
        float minDistance = float.MaxValue;

        foreach (var building in buildings)
        {
            if (!IsInstanceValid(building)) continue;
            float dist = _enemies[index].Position.DistanceSquaredTo(building.GlobalPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = building;
            }
        }
        return nearest;
    }
}