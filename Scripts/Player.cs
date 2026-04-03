using Godot;
using RtsGame.Scripts;
using System;
using System.Collections.Generic;

public enum PlayerInputState
{
    Normal,//一般状态
    Building,//建筑状态
}

public partial class Player : Node3D
{
    [Export] public float MoveSpeed = 20.0f;
    [Export] public float ZoomSpeed = 10.0f;
    [Export] public float MinZoom = -100.0f;
    [Export] public float MaxZoom = 100.0f;
    [Export] private Camera3D _camera;
    private float _zoomTarget;

    [Export] public SelectRect _selectRect;
    private bool isLeftBtnDown = false;
    private bool isDragging;
    private Vector2 dragStart;
    private Vector2 dragEnd;
    const float DragThreshold = 5f; // 拖拽阈值, 超过这个值才算拖拽, 不然算单击

    [Export] public Label GoldCountLb;
    private int _goldCount = 0;
    private List<UnitBase> _curSelectedUnitList = new List<UnitBase>();

    public PlayerInputState CurInputState = PlayerInputState.Normal;

    public override void _Ready()
    {
        GameManager.Instance.Player = this;
        _zoomTarget = _camera.Position.Y;
        GoldCountLb.Text = $"Gold : {_goldCount}";
    }

    public override void _Process(double delta)
    {
        HandleMovement((float)delta);
        HandleZoom((float)delta);
        if(_curSelectedUnitList.Count == 1 && _curSelectedUnitList[0] is Worker worker && Input.IsActionJustPressed("Build"))
        {
            worker.OpenBuildingNode(true);
        }
    }

    private void HandleMovement(float delta)
    {
        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBack");

        // 3. 计算并应用移动
        if (inputDir.Length() > 0)
        {
            // 归一化防止斜向移动过快
            inputDir = inputDir.Normalized();

            // 直接利用 Basis 转换到局部坐标系的方向，这样旋转相机后，按W依然是向“前方”走
            // Transform.Basis * Vector3 会自动处理 Forward/Right 的组合
            Vector3 moveDir = (Transform.Basis.Z * inputDir.Y) + (Transform.Basis.X * inputDir.X);

            // 锁定 Y 轴，防止相机因为俯仰角产生高度位移
            moveDir.Y = 0;

            GlobalPosition += moveDir.Normalized() * MoveSpeed * delta;
        }
    }

    private void HandleZoom(float delta)
    {
        // 平滑缩放 Camera3D 的高度 (Y) 和偏移 (Z)
        Vector3 camPos = _camera.Position;
        float newY = Mathf.Lerp(camPos.Y, _zoomTarget, ZoomSpeed * delta);

        // 可选：如果希望缩放时视角有变化，可以根据 Y 同步调整 Z
        _camera.Position = new Vector3(camPos.X, newY, camPos.Z);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch(CurInputState)
        {
            case PlayerInputState.Normal:
            {
                HandleNormalInput(@event);
                break;
            }
            case PlayerInputState.Building:
            {
                HandleBuildingInput(@event);
                break;
            }
        }

        
    }

    private void HandleNormalInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            switch (mb.ButtonIndex)
            {
                case MouseButton.Left:
                    if (mb.Pressed)
                    {
                        GD.Print("左键按下");
                        Camera3D camera = GetViewport().GetCamera3D();
                        if (camera == null) return;
                        Vector3 from = camera.ProjectRayOrigin(mb.Position);
                        Vector3 to = from + camera.ProjectRayNormal(mb.Position) * 1000; // 射线长度 1000 米
                        var query = PhysicsRayQueryParameters3D.Create(from, to, GameManager.Instance.UI3D_MASK);
                        query.CollideWithAreas = true;
                        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
                        if (result.Count > 0)
                        {
                            Node3D hitObject = (Node3D)result["collider"];
                            if (hitObject is Area3D area && area.GetParent() is BuildingItem buildingItem)
                            {
                                CurInputState = PlayerInputState.Building;
                                GD.Print("左键了一个建筑item, 进入建筑输入模式");
                                return;
                            }
                        }
                        isLeftBtnDown = true;
                        dragStart = mb.Position;
                        isDragging = false;
                        _selectRect.StartPos = dragStart;
                        _selectRect.EndPos = dragStart;
                    }
                    else
                    {
                        GD.Print("左键松开");
                        isLeftBtnDown = false;
                        if (isDragging)
                            HandleMultiSelection(dragStart, mb.Position);
                        else
                            HandleSingleSelect(mb.Position);

                        isDragging = false;
                        _selectRect.IsSelecting = false;
                    }
                    break;

                case MouseButton.Right:
                    if (mb.Pressed)
                    {
                        Camera3D camera = GetViewport().GetCamera3D();
                        if (camera == null) return;
                        Vector3 from = camera.ProjectRayOrigin(mb.Position);
                        Vector3 to = from + camera.ProjectRayNormal(mb.Position) * 1000; // 射线长度 1000 米
                        var query = PhysicsRayQueryParameters3D.Create(from, to, GameManager.Instance.TERRAIN_MASK | GameManager.Instance.INTERACTABLE_MASK);
                        query.CollideWithAreas = true;
                        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
                        if (result.Count > 0)
                        {
                            Node3D hitObject = (Node3D)result["collider"];
                            if (hitObject is Area3D area)
                            {
                                if (area.Owner is ResourceBase targetRes)
                                {
                                    GD.Print("右键了一个资源点");
                                    Vector3 targetResPos = (Vector3)result["position"];
                                    int unitCount = _curSelectedUnitList.Count;
                                    if (unitCount == 0) return;
                                    for (int i = 0; i < unitCount; i++)
                                    {
                                        if (_curSelectedUnitList[i] is Worker worker)
                                        {
                                            worker.SetTarget(TargetType.Resource, targetRes.GlobalPosition);
                                            worker.SetResource(targetRes);
                                        }
                                        _curSelectedUnitList[i].SetTarget(TargetType.Resource, targetRes.GlobalPosition);
                                    }
                                }
                                else if (area.Owner is MainBase targetBuilding)
                                {
                                    GD.Print("右键了一个基地");
                                    Vector3 targetBuildingPos = (Vector3)result["position"];
                                    int unitCount = _curSelectedUnitList.Count;
                                    if (unitCount == 0) return;
                                    for (int i = 0; i < unitCount; i++)
                                    {
                                        if (_curSelectedUnitList[i] is Worker worker && targetBuilding is MainBase targetBase)
                                        {
                                            worker.SetTarget(TargetType.Normal, targetBuilding.GlobalPosition);
                                        }
                                        _curSelectedUnitList[i].SetTarget(TargetType.MainBase, targetBuilding.GlobalPosition);
                                    }
                                }
                                else if (area.Owner is UnitBase targetUnit)
                                {
                                    GD.Print("右键了一个单位");
                                    Vector3 targetUnitPos = (Vector3)result["position"];
                                    int unitCount = _curSelectedUnitList.Count;
                                    if (unitCount == 0) return;
                                    float spacing = 3.0f; // 单位之间的间距，根据模型大小调整
                                    int columns = Mathf.CeilToInt(Mathf.Sqrt(unitCount)); // 自动计算列数，趋向于正方形
                                    for (int i = 0; i < unitCount; i++)
                                    {
                                        int row = i / columns;// 计算当前单位在网格中的行和列
                                        int col = i % columns;
                                        float xOffset = (col - (columns - 1) / 2.0f) * spacing;// 计算偏移量（以点击点为中心排列）
                                        float zOffset = (row - (Mathf.CeilToInt((float)unitCount / columns) - 1) / 2.0f) * spacing;
                                        Vector3 formationPos = new Vector3(targetUnitPos.X + xOffset, targetUnitPos.Y, targetUnitPos.Z + zOffset);
                                        _curSelectedUnitList[i].SetTarget(TargetType.Normal, formationPos);
                                    }
                                }
                                else
                                {
                                    GD.PrintErr("不知道点了个啥");
                                }
                            }
                            else
                            {
                                GD.Print("右键了地面");//全部单位移动过去
                                Vector3 targetGroundPos = (Vector3)result["position"];
                                int unitCount = _curSelectedUnitList.Count;
                                if (unitCount == 0) return;
                                float spacing = 3.0f; // 单位之间的间距，根据模型大小调整
                                int columns = Mathf.CeilToInt(Mathf.Sqrt(unitCount)); // 自动计算列数，趋向于正方形
                                for (int i = 0; i < unitCount; i++)
                                {
                                    int row = i / columns;// 计算当前单位在网格中的行和列
                                    int col = i % columns;
                                    float xOffset = (col - (columns - 1) / 2.0f) * spacing;// 计算偏移量（以点击点为中心排列）
                                    float zOffset = (row - (Mathf.CeilToInt((float)unitCount / columns) - 1) / 2.0f) * spacing;
                                    Vector3 formationPos = new Vector3(targetGroundPos.X + xOffset, targetGroundPos.Y, targetGroundPos.Z + zOffset);
                                    _curSelectedUnitList[i].SetTarget(TargetType.Normal, formationPos);
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                    break;

                case MouseButton.Middle:
                    if (mb.Pressed)
                    {

                    }
                    else
                    {

                    }
                    break;

                case MouseButton.WheelUp:
                    if (mb.Pressed)
                    {
                        _zoomTarget = Mathf.Clamp(_zoomTarget - 2.0f, MinZoom, MaxZoom);
                    }
                    break;

                case MouseButton.WheelDown:
                    if (mb.Pressed)
                    {
                        _zoomTarget = Mathf.Clamp(_zoomTarget + 2.0f, MinZoom, MaxZoom);
                    }
                    break;
            }
        }

        // 处理鼠标移动
        if (@event is InputEventMouseMotion motion)
        {
            if (isLeftBtnDown == false)
                return;
            dragEnd = motion.Position;

            if (!isDragging && dragStart.DistanceTo(dragEnd) > DragThreshold)
            {
                isDragging = true;
                _selectRect.IsSelecting = true;
            }

            if (isDragging)
            {
                _selectRect.EndPos = dragEnd;
            }
        }
    }

    private void HandleBuildingInput(InputEvent @event)
    {

    }

    private void UpdateSelectRect()
    {
        if (_selectRect == null) return;

        _selectRect.Visible = true;
        Vector2 pos = new Vector2(Mathf.Min(dragStart.X, dragEnd.X), Mathf.Min(dragStart.Y, dragEnd.Y));
        Vector2 size = (dragStart - dragEnd).Abs();
        _selectRect.Position = pos;
        _selectRect.Size = size;
        _selectRect.QueueRedraw();
    }

    private void HandleSingleSelect(Vector2 position)
    {
        ClearSelectedUnitList();
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;
        Vector3 from = camera.ProjectRayOrigin(position);
        Vector3 to = from + camera.ProjectRayNormal(position) * 1000; // 射线长度 1000 米
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            Node3D hitObject = (Node3D)result["collider"];
            if (hitObject is Area3D area && area.GetParent() is UnitBase u)
            {
                _curSelectedUnitList.Add(u);
                u.SetSelected(true);
            }
        }
    }

    private void HandleMultiSelection(Vector2 start, Vector2 end)
    {
        //GD.Print($"框选完成！从 {start} 到 {end}");
        var camera = GetViewport().GetCamera3D();
        ClearSelectedUnitList();

        // 1. 确定矩形的最小和最大坐标
        Vector2 min = new Vector2(Mathf.Min(start.X, end.X), Mathf.Min(start.Y, end.Y));
        Vector2 max = new Vector2(Mathf.Max(start.X, end.X), Mathf.Max(start.Y, end.Y));
        foreach (UnitBase unit in GameManager.Instance.UnitList)
        {
            // 将单位的 3D 位置转换回屏幕 2D 位置
            Vector2 screenPos = camera.UnprojectPosition(unit.GlobalPosition);

            // 检查该 2D 位置是否在框选矩形内
            // 顺便检查物体是否在相机前方（IsPositionBehind 返回 true 代表在背后）
            if (screenPos.X >= min.X && screenPos.X <= max.X &&
                screenPos.Y >= min.Y && screenPos.Y <= max.Y &&
                !camera.IsPositionBehind(unit.GlobalPosition))
            {
                // 选中该单位
                _curSelectedUnitList.Add(unit);
                unit.SetSelected(true);
            }
        }
    }
    
    private void ClearSelectedUnitList()
    {
        foreach (var unit in _curSelectedUnitList)
        {
            if(unit is Worker worker)
            {
                worker.OpenBuildingNode(false);
            }
            unit.SetSelected(false);
        }
        _curSelectedUnitList.Clear();
    }

    public void SetGoldCount(int count)
    {
        _goldCount += count;
        GoldCountLb.Text = $"Gold : {_goldCount}";
    }
}