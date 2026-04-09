using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.Global;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

public enum PlayerState
{
    Normal,//一般状态
    BuildingPanel,//打开面板状态
    PreviewBuilding,//预览状态
}

public partial class Player : Node3D
{
    [Export] public float MoveSpeed = 20.0f;
    [Export] public float ZoomSpeed = 10.0f;
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

    public PlayerState CurState = PlayerState.Normal;

    private BuildingType _curBuildingType;
    [Export] public Node3D BuildingPanelNode;
    private BuildingPreviewBase curBuildingPreview;

    [Export] public PackedScene MainBasePs;
    [Export] public PackedScene MainBasePreviewPs;

    [Export] public PackedScene FlagPs;
    [Export] public PackedScene FlagPreviewPs;

    [Export] public PackedScene GoldMakerPs;
    [Export] public PackedScene GoldMakerPreviewPs;

    [Export] public PackedScene MagicTowerPs;
    [Export] public PackedScene MagicTowerPreviewPs;

    public override void _Ready()
    {
        GameManager.Instance.Player = this;
        GoldCountLb.Text = $"Gold : {_goldCount}";
        BuildingPanelNode.Visible = false;
        BuildingPanelNode.ProcessMode = ProcessModeEnum.Disabled;
        _zoomTarget = GlobalPosition.Y;
    }

    public override void _Process(double delta)
    {
        switch (CurState)
        {
            case PlayerState.Normal:
                HandleSelectionAndCommand();
                break;
            case PlayerState.BuildingPanel:
                
                break;
            case PlayerState.PreviewBuilding:
                if (Input.IsActionJustPressed("Exit") || Input.IsActionJustPressed("RightMouseBtn"))
                {
                    foreach (var showFlagRingMainBase in GameManager.Instance.MainBaseList)
                        showFlagRingMainBase.ShowFlagRing(false);
                    foreach (var showBuildingRingFlag in GameManager.Instance.FlagList)
                        showBuildingRingFlag.ShowBuildingRing(false);
                    curBuildingPreview.QueueFree();
                    CurState = PlayerState.Normal;
                }
                Vector2 mousePos = GetViewport().GetMousePosition();
                Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
                Vector3 rayDir = _camera.ProjectRayNormal(mousePos);
                PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDir * 1000f);
                query.CollisionMask = GameManager.Instance.TERRAIN_MASK;
                Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
                if (result.Count == 0) return;
                Vector3 hitPos = (Vector3)result["position"];
                Vector3 snapPos = GameManager.Instance.BuildingGridMap.SnapToGrid(hitPos);
                switch(_curBuildingType)
                {
                    case BuildingType.MainBase:
                        curBuildingPreview.SetCanPlace(GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height));
                        
                        break;
                    case BuildingType.Flag:
                    case BuildingType.GoldMaker:
                    case BuildingType.MagicTower:
                        bool inRange = false;
                        float distSq = 0;
                        foreach (var curMainBase in GameManager.Instance.MainBaseList)
                        {
                            distSq = snapPos.DistanceSquaredTo(curMainBase.GlobalPosition);
                            if (distSq <= curMainBase.FlagRangeSq)
                            {
                                inRange = true;
                                break;
                            }
                        }
                        if (!inRange)
                        {
                            foreach (var curFlag in GameManager.Instance.FlagList)
                            {
                                distSq = snapPos.DistanceSquaredTo(curFlag.GlobalPosition);
                                if (distSq <= curFlag.BuildingRangeSq) // 或者使用 curFlag.FlagRangeSq，取决于你的属性命名
                                {
                                    inRange = true;
                                    break;
                                }
                            }
                        }
                        bool canGridPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                        curBuildingPreview.SetCanPlace(inRange && canGridPlace);
                        break;
                }

                //GD.Print("canplace : " + GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height));
                GD.Print(GameManager.Instance.BuildingGridMap.WorldToGrid(snapPos));
                curBuildingPreview.GlobalPosition = snapPos;
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (CurState)
        {
            case PlayerState.Normal:
                HandleMovement((float)delta);
                if(Input.IsActionJustPressed("Build"))
                {
                    BuildingPanelNode.Visible = true;
                    BuildingPanelNode.ProcessMode = ProcessModeEnum.Inherit;
                    CurState = PlayerState.BuildingPanel;
                }
                break;
            case PlayerState.BuildingPanel:
                if (Input.IsActionJustPressed("Exit"))
                {
                    BuildingPanelNode.Visible = false;
                    BuildingPanelNode.ProcessMode = ProcessModeEnum.Disabled;
                    CurState = PlayerState.Normal;
                }
                HandleSelectBuildingItem();
                break;
            case PlayerState.PreviewBuilding:
                HandleMovement((float)delta);
                HandlePlaceBuilding();
                break;
        }
    }
        
    private void HandleMovement(float delta)
    {
        //上下
        if (Input.IsActionJustPressed("ZoomUp"))
            _zoomTarget -= 2;
        if (Input.IsActionJustPressed("ZoomDown"))
            _zoomTarget += 2;
        float newY = Mathf.Lerp(GlobalPosition.Y, _zoomTarget, ZoomSpeed * delta);
        GlobalPosition = new Vector3(GlobalPosition.X, newY, GlobalPosition.Z);

        //前后左右
        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBack");
        if (inputDir.Length() > 0)
        {
            inputDir = inputDir.Normalized();
            Vector3 moveDir = (Transform.Basis.Z * inputDir.Y) + (Transform.Basis.X * inputDir.X);
            moveDir.Y = 0;
            GlobalPosition += moveDir.Normalized() * MoveSpeed * delta;
        }
    }

    private void HandleSelectionAndCommand()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (Input.IsActionJustPressed("LeftMouseBtn"))
        {
            GD.Print("左键按下");
            isLeftBtnDown = true;
            dragStart = mousePos;
            isDragging = false;
            _selectRect.StartPos = dragStart;
            _selectRect.EndPos = dragStart;
        }
        if (Input.IsActionJustReleased("LeftMouseBtn"))
        {
            GD.Print("左键松开");
            isLeftBtnDown = false;
            if (isDragging)
                MultiSelection(dragStart, mousePos);
            else
                SingleSelect(mousePos);
            isDragging = false;
            _selectRect.IsSelecting = false;
        }
        if (Input.IsActionJustPressed("RightMouseBtn"))
        {
            GD.Print("右键按下");
            if (_camera == null) return;
            Vector3 from = _camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + _camera.ProjectRayNormal(mousePos) * 1000; // 射线长度 1000 米
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
                            _curSelectedUnitList[i].SetSelected(false);
                            _curSelectedUnitList.RemoveAt(i);
                            return;
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
        
        if(isLeftBtnDown)
        {
            dragEnd = mousePos;

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
        //GD.Print("左键位置 : " + GetViewport().GetMousePosition());
    }

    private void HandleSelectBuildingItem()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (Input.IsActionJustReleased("LeftMouseBtn"))
        {
            Vector3 from = _camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + _camera.ProjectRayNormal(mousePos) * 1000; // 射线长度 1000 米
            var query = PhysicsRayQueryParameters3D.Create(from, to, GameManager.Instance.UI3D_MASK);
            query.CollideWithAreas = true;
            var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                if (hitObject is Area3D area && area.Owner is BuildingItemBase item)
                {
                    BuildingPanelNode.Visible = false;
                    BuildingPanelNode.ProcessMode = ProcessModeEnum.Disabled;
                    CurState = PlayerState.PreviewBuilding;
                    switch (item.Type)
                    {
                        case BuildingType.MainBase:
                            GD.Print("选了主基地");
                            _curBuildingType = BuildingType.MainBase;
                            curBuildingPreview = MainBasePreviewPs.Instantiate<BuildingPreviewBase>();
                            GetTree().CurrentScene.AddChild(curBuildingPreview);
                            break;
                        case BuildingType.Flag:
                            foreach (var mainBase in GameManager.Instance.MainBaseList)
                                mainBase.ShowFlagRing(true);
                            foreach (var flag in GameManager.Instance.FlagList)
                                flag.ShowBuildingRing(true);
                            GD.Print("选了旗帜");
                            _curBuildingType = BuildingType.Flag;
                            curBuildingPreview = FlagPreviewPs.Instantiate<BuildingPreviewBase>();
                            GetTree().CurrentScene.AddChild(curBuildingPreview);
                            break;
                        case BuildingType.GoldMaker:
                            foreach (var mainBase in GameManager.Instance.MainBaseList)
                                mainBase.ShowFlagRing(true);
                            foreach (var flag in GameManager.Instance.FlagList)
                                flag.ShowBuildingRing(true);
                            GD.Print("选了金矿机");
                            _curBuildingType = BuildingType.GoldMaker;
                            curBuildingPreview = GoldMakerPreviewPs.Instantiate<BuildingPreviewBase>();
                            GetTree().CurrentScene.AddChild(curBuildingPreview);
                            break;
                        case BuildingType.MagicTower:
                            foreach (var mainBase in GameManager.Instance.MainBaseList)
                                mainBase.ShowFlagRing(true);
                            foreach (var flag in GameManager.Instance.FlagList)
                                flag.ShowBuildingRing(true);
                            GD.Print("选了法术塔");
                            _curBuildingType = BuildingType.MagicTower;
                            curBuildingPreview = MagicTowerPreviewPs.Instantiate<BuildingPreviewBase>();
                            GetTree().CurrentScene.AddChild(curBuildingPreview);
                            break;
                    }
                    GD.Print("点击了一个建筑item, 进入建筑预览模式");
                    return;
                }
            }
        }
    }

    private void HandlePlaceBuilding()
    {
        if(Input.IsActionJustPressed("LeftMouseBtn"))
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
            Vector3 rayDir = _camera.ProjectRayNormal(mousePos);
            PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDir * 1000f);
            query.CollisionMask = GameManager.Instance.TERRAIN_MASK;
            Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count == 0) return;
            Vector3 hitPos = (Vector3)result["position"];
            Vector3 snapPos = GameManager.Instance.BuildingGridMap.SnapToGrid(hitPos);
            if(GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height) == false)
            {
                return;
            }
            switch (_curBuildingType)
            {
                case BuildingType.MainBase:
                    GD.Print("放置了主基地");
                    MainBase mainBase = MainBasePs.Instantiate<MainBase>();
                    mainBase.Position = snapPos;
                    GetTree().CurrentScene.AddChild(mainBase);
                    GameManager.Instance.BuildingGridMap.Place(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    curBuildingPreview.QueueFree();
                    CurState = PlayerState.Normal;
                    break;
                case BuildingType.Flag:
                    bool inRange = false;
                    float distSq = 0;
                    foreach (var curMainBase in GameManager.Instance.MainBaseList)
                    {
                        distSq = snapPos.DistanceSquaredTo(curMainBase.GlobalPosition);
                        if (distSq <= curMainBase.FlagRangeSq)
                        {
                            inRange = true;
                            break;
                        }
                    }
                    if (!inRange)
                    {
                        foreach (var curFlag in GameManager.Instance.FlagList)
                        {
                            distSq = snapPos.DistanceSquaredTo(curFlag.GlobalPosition);
                            if (distSq <= curFlag.BuildingRangeSq) // 或者使用 curFlag.FlagRangeSq，取决于你的属性命名
                            {
                                inRange = true;
                                break;
                            }
                        }
                    }
                    bool canGridPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    if (canGridPlace == false || inRange == false)
                        return;
                    GD.Print("放置了旗帜");
                    Flag flag = FlagPs.Instantiate<Flag>();
                    flag.Position = snapPos;
                    GetTree().CurrentScene.AddChild(flag);
                    GameManager.Instance.BuildingGridMap.Place(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    foreach (var showFlagRingMainBase in GameManager.Instance.MainBaseList)
                        showFlagRingMainBase.ShowFlagRing(false);
                    foreach (var showBuildingRingFlag in GameManager.Instance.FlagList)
                        showBuildingRingFlag.ShowBuildingRing(false);
                    curBuildingPreview.QueueFree();
                    CurState = PlayerState.Normal;
                    break;
                case BuildingType.GoldMaker:
                    bool goldMakerInRange = false;
                    float goldMakerDistSq = 0;
                    foreach (var curMainBase in GameManager.Instance.MainBaseList)
                    {
                        goldMakerDistSq = snapPos.DistanceSquaredTo(curMainBase.GlobalPosition);
                        if (goldMakerDistSq <= curMainBase.FlagRangeSq)
                        {
                            goldMakerInRange = true;
                            break;
                        }
                    }
                    if (!goldMakerInRange)
                    {
                        foreach (var curFlag in GameManager.Instance.FlagList)
                        {
                            goldMakerDistSq = snapPos.DistanceSquaredTo(curFlag.GlobalPosition);
                            if (goldMakerDistSq <= curFlag.BuildingRangeSq) // 或者使用 curFlag.FlagRangeSq，取决于你的属性命名
                            {
                                goldMakerInRange = true;
                                break;
                            }
                        }
                    }
                    bool canGridPlaceGoldMaker = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    if (canGridPlaceGoldMaker == false || goldMakerInRange == false)
                        return;
                    GD.Print("放置了金矿机");
                    GoldMaker goldMaker = GoldMakerPs.Instantiate<GoldMaker>();
                    goldMaker.Position = snapPos;
                    GetTree().CurrentScene.AddChild(goldMaker);
                    GameManager.Instance.BuildingGridMap.Place(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    foreach (var showFlagRingMainBase in GameManager.Instance.MainBaseList)
                        showFlagRingMainBase.ShowFlagRing(false);
                    foreach (var showBuildingRingFlag in GameManager.Instance.FlagList)
                        showBuildingRingFlag.ShowBuildingRing(false);
                    curBuildingPreview.QueueFree();
                    CurState = PlayerState.Normal;
                    break;
                case BuildingType.MagicTower:
                    bool magicTowerInRange = false;
                    float magicTowerDistSq = 0;
                    foreach (var curMainBase in GameManager.Instance.MainBaseList)
                    {
                        magicTowerDistSq = snapPos.DistanceSquaredTo(curMainBase.GlobalPosition);
                        if (magicTowerDistSq <= curMainBase.FlagRangeSq)
                        {
                            magicTowerInRange = true;
                            break;
                        }
                    }
                    if (!magicTowerInRange)
                    {
                        foreach (var curFlag in GameManager.Instance.FlagList)
                        {
                            magicTowerDistSq = snapPos.DistanceSquaredTo(curFlag.GlobalPosition);
                            if (magicTowerDistSq <= curFlag.BuildingRangeSq) // 或者使用 curFlag.FlagRangeSq，取决于你的属性命名
                            {
                                magicTowerInRange = true;
                                break;
                            }
                        }
                    }
                    bool canGridPlaceMagicTower = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    if (canGridPlaceMagicTower == false || magicTowerInRange == false)
                        return;
                    GD.Print("放置了魔法塔");
                    MagicTower magicTower = MagicTowerPs.Instantiate<MagicTower>();
                    magicTower.Position = snapPos;
                    GetTree().CurrentScene.AddChild(magicTower);
                    GameManager.Instance.BuildingGridMap.Place(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
                    foreach (var showFlagRingMainBase in GameManager.Instance.MainBaseList)
                        showFlagRingMainBase.ShowFlagRing(false);
                    foreach (var showBuildingRingFlag in GameManager.Instance.FlagList)
                        showBuildingRingFlag.ShowBuildingRing(false);
                    curBuildingPreview.QueueFree();
                    CurState = PlayerState.Normal;
                    break;
            }
        }
        
    }


    private void SingleSelect(Vector2 position)
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

    private void MultiSelection(Vector2 start, Vector2 end)
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
                //worker.OpenBuildingNode(false);
                //退出建筑状态
            }
            unit.SetSelected(false);
        }
        _curSelectedUnitList.Clear();
    }

    public void TakeGoldCount(int count)
    {
        _goldCount += count;
        GoldCountLb.Text = $"Gold : {_goldCount}";
    }
}