using Godot;
using RtsGame.Scripts;
using System;
using System.Collections.Generic;

public struct EnemyData
{
    public int TypeId;//敌人类型
    public int LocalIndex;//对应类型里的索引
    public Vector3 Position;
    public int MaxHp;
    public int CurHp;
    public float MoveSpeed;
    public Node3D CurTarget;
}

public partial class EnemyManager : Node3D
{
    [Export] public Mesh[] EnemyMeshes;
    [Export] public int[] CountsPerType;
    [Export] public float CellSize = 5.0f;
    [Export] public float WanderChangeTime = 2.0f; // 多少秒换一次方向
    [Export] public ShaderMaterial EnemyShaderMaterial;

    private MultiMeshInstance3D[] _mmNodes;
    private EnemyData[] _allEnemies;
    private int _totalCount;
    private Dictionary<Vector2I, List<int>> _spatialHash = new Dictionary<Vector2I, List<int>>();

    public override void _Ready()
    {
        InitEnemies();
    }

    private void InitEnemies()
    {
        _totalCount = 0;
        foreach (int count in CountsPerType) _totalCount += count;

        _allEnemies = new EnemyData[_totalCount];
        _mmNodes = new MultiMeshInstance3D[EnemyMeshes.Length];

        int globalIdx = 0;
        for (int t = 0; t < EnemyMeshes.Length; t++)
        {
            var mm = new MultiMesh();
            mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            mm.UseCustomData = true;
            mm.Mesh = EnemyMeshes[t];
            mm.InstanceCount = CountsPerType[t];

            var mmInst = new MultiMeshInstance3D();
            mmInst.Multimesh = mm;
            mmInst.MaterialOverride = EnemyShaderMaterial;
            AddChild(mmInst);
            _mmNodes[t] = mmInst;

            for (int l = 0; l < CountsPerType[t]; l++)
            {
                _allEnemies[globalIdx] = new EnemyData
                {
                    // 确保初始位置 Y 为 0
                    Position = new Vector3((float)GD.RandRange(-100, 100), 0, (float)GD.RandRange(-100, 100)),
                    MoveSpeed = 3,
                    MaxHp = 100,
                    CurHp = 100,
                    TypeId = t,
                    LocalIndex = l
                };
                globalIdx++;
            }
        }
    }

    public override void _Process(double delta)
    {
        float fDelta = (float)delta;
        UpdateSpatialHash();

        for (int i = 0; i < _totalCount; i++)
        {
            if (_allEnemies[i].CurHp <= 0) continue;
            BuildingBase targetBd = FindNearestBuilding(i);
            Vector3 curDir = Vector3.Zero;
            if ( targetBd != null )
                curDir = _allEnemies[i].Position.DirectionTo(targetBd.GlobalPosition);
            _allEnemies[i].Position += curDir * _allEnemies[i].MoveSpeed * fDelta;
            //更新渲染
            UpdateRenderer(i, curDir * _allEnemies[i].MoveSpeed);
        }
    }

    private void UpdateRenderer(int i, Vector3 moveVec)
    {
        var data = _allEnemies[i];
        Transform3D t = Transform3D.Identity;
        t.Origin = data.Position;

        // 归一化移动方向，以便 Shader 处理
        Vector3 normMove = moveVec.Normalized();

        if (moveVec.LengthSquared() > 0.01f)
        {
            float angle = Mathf.Atan2(moveVec.X, moveVec.Z);
            t.Basis = new Basis(Vector3.Up, angle);
        }

        _mmNodes[data.TypeId].Multimesh.SetInstanceTransform(data.LocalIndex, t);

        Color customData = new Color(
            i * 0.13f,
            normMove.X,
            normMove.Z,
            0.0f
        );
        _mmNodes[data.TypeId].Multimesh.SetInstanceCustomData(data.LocalIndex, customData);
    }

    private void UpdateSpatialHash()
    {
        _spatialHash.Clear();
        for (int i = 0; i < _totalCount; i++)
        {
            if (_allEnemies[i].CurHp <= 0) continue;
            Vector2I key = GetGridKey(_allEnemies[i].Position);
            if (!_spatialHash.ContainsKey(key)) _spatialHash[key] = new List<int>();
            _spatialHash[key].Add(i);
        }
    }

    private Vector2I GetGridKey(Vector3 pos) 
        => new Vector2I(Mathf.FloorToInt(pos.X / CellSize), Mathf.FloorToInt(pos.Z / CellSize));

    private BuildingBase FindNearestBuilding(int index)
    {
        var buildings = GameManager.Instance.BuildingList;
        if (buildings == null || buildings.Count == 0) return null;

        BuildingBase nearest = null;
        float minDistance = float.MaxValue;

        foreach (var building in buildings)
        {
            if (!IsInstanceValid(building)) continue;

            float dist = _allEnemies[index].Position.DistanceSquaredTo(building.GlobalPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = building;
            }
        }
        return nearest;
    }
}