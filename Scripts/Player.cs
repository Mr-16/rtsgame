using Godot;
using RtsGame.Scripts;
using RtsGame.Scripts.Global;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

public enum PlayerState
{
    Normal,//一般状态
    ChooseBuilding,//打开面板状态
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

    private BuildingBase _curSelectedBuilding;

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
                NormalProcess((float)delta);
                break;
            case PlayerState.ChooseBuilding:
                ChooseBuildingProcess((float)delta);
                break;
            case PlayerState.PreviewBuilding:
                PreviewBuildingProcess((float)delta);
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (CurState)
        {
            case PlayerState.Normal:
                NormalPhysicsProcess((float)delta);
                break;
            case PlayerState.ChooseBuilding:
                ChooseBuildingPhysicsProcess((float)delta);
                break;
            case PlayerState.PreviewBuilding:
                PreviewBuildingPhysicsProcess((float)delta);
                break;
        }
    }


    //正常状态
    private void NormalProcess(float delta)
    {
        HandleSelectionAndCommand();
    }
    private void NormalPhysicsProcess(float delta)
    {
        HandleMovement(delta);
        if (Input.IsActionJustPressed("Build"))
        {
            BuildingPanelNode.Visible = true;
            BuildingPanelNode.ProcessMode = ProcessModeEnum.Inherit;
            CurState = PlayerState.ChooseBuilding;
        }
    }

    //选建筑状态
    private void ChooseBuildingProcess(float delta)
    {
        
    }
    private void ChooseBuildingPhysicsProcess(float delta)
    {
        if (Input.IsActionJustPressed("Exit"))
        {
            BuildingPanelNode.Visible = false;
            BuildingPanelNode.ProcessMode = ProcessModeEnum.Disabled;
            CurState = PlayerState.Normal;
        }
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (Input.IsActionJustReleased("LeftMouseBtn"))
        {
            Vector3 from = _camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + _camera.ProjectRayNormal(mousePos) * 1000; // 射线长度 1000 米
            var query = PhysicsRayQueryParameters3D.Create(from, to, GameManager.Instance.UI3D_MASK);
            query.CollideWithAreas = true;
            var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count <= 0)
                return;
            Node3D hitObject = (Node3D)result["collider"];
            if (hitObject is not Area3D area || area.Owner is not BuildingItemBase item)
                return;
            foreach (var mainBase in GameManager.Instance.MainBaseList)
                mainBase.ShowFlagRing(true);
            foreach (var flag in GameManager.Instance.FlagList)
                flag.ShowBuildingRing(true);
            switch (item.Type)
            {
                case BuildingType.MainBase:
                    GD.Print("选了主基地");
                    _curBuildingType = BuildingType.MainBase;
                    curBuildingPreview = MainBasePreviewPs.Instantiate<BuildingPreviewBase>();
                    break;
                case BuildingType.Flag:
                    GD.Print("选了旗帜");
                    _curBuildingType = BuildingType.Flag;
                    curBuildingPreview = FlagPreviewPs.Instantiate<BuildingPreviewBase>();
                    break;
                case BuildingType.GoldMaker:
                    GD.Print("选了金矿机");
                    _curBuildingType = BuildingType.GoldMaker;
                    curBuildingPreview = GoldMakerPreviewPs.Instantiate<BuildingPreviewBase>();
                    break;
                case BuildingType.MagicTower:
                    GD.Print("选了法术塔");
                    _curBuildingType = BuildingType.MagicTower;
                    curBuildingPreview = MagicTowerPreviewPs.Instantiate<BuildingPreviewBase>();
                    break;
            }
            GD.Print("点击了一个建筑item, 进入建筑预览模式");
            BuildingPanelNode.Visible = false;
            BuildingPanelNode.ProcessMode = ProcessModeEnum.Disabled;
            GetTree().CurrentScene.AddChild(curBuildingPreview);
            CurState = PlayerState.PreviewBuilding;
        }
    }

    //预览建筑状态
    private void PreviewBuildingProcess(float delta)
    {
        
    }
    private void PreviewBuildingPhysicsProcess(float delta)
    {
        HandleMovement((float)delta);

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
                if (distSq <= curFlag.BuildingRangeSq)
                {
                    inRange = true;
                    break;
                }
            }
        }

        switch (_curBuildingType)
        {
            case BuildingType.MainBase:
                PreviewMainBase(snapPos);
                break;
            case BuildingType.Flag:
                PreviewFlag(snapPos, inRange);
                break;
            case BuildingType.GoldMaker:
                PreviewGoldMaker(snapPos, inRange);
                break;
            case BuildingType.MagicTower:
                PreviewMagicTower(snapPos, inRange);
                break;
        }
        curBuildingPreview.GlobalPosition = snapPos;
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
            isLeftBtnDown = true;
            dragStart = mousePos;
            isDragging = false;
            _selectRect.StartPos = dragStart;
            _selectRect.EndPos = dragStart;
        }
        if (Input.IsActionJustReleased("LeftMouseBtn"))
        {
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
    }

    private void SingleSelect(Vector2 position)
    {
        ClearSelect();
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
            if (hitObject is not Area3D area)
                return;
            GD.Print("单选了个area");
            if (area.GetParent() is UnitBase unit)
            {
                GD.Print("单选了个单位");
                _curSelectedUnitList.Add(unit);
                unit.SetSelected(true);
                return;
            }
            if (area.GetParent() is BuildingBase building)
            {
                GD.Print("单选了个建筑");
                _curSelectedBuilding = building;
                building.SetSelected(true);
                return;
            }
        }
    }

    private void MultiSelection(Vector2 start, Vector2 end)
    {
        var camera = GetViewport().GetCamera3D();
        ClearSelect();
        Vector2 min = new Vector2(Mathf.Min(start.X, end.X), Mathf.Min(start.Y, end.Y));
        Vector2 max = new Vector2(Mathf.Max(start.X, end.X), Mathf.Max(start.Y, end.Y));
        foreach (UnitBase unit in GameManager.Instance.UnitList)
        {
            Vector2 screenPos = camera.UnprojectPosition(unit.GlobalPosition);
            if (screenPos.X >= min.X && screenPos.X <= max.X &&
                screenPos.Y >= min.Y && screenPos.Y <= max.Y &&
                !camera.IsPositionBehind(unit.GlobalPosition))
            {
                _curSelectedUnitList.Add(unit);
                unit.SetSelected(true);
            }
        }
    }
    
    private void ClearSelect()
    {
        if(_curSelectedBuilding != null)
            _curSelectedBuilding.SetSelected(false);
        foreach (var unit in _curSelectedUnitList)
            unit.SetSelected(false);
        _curSelectedUnitList.Clear();
    }

    private void PreviewMainBase(Vector3 snapPos)
    {
        bool canPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
        curBuildingPreview.SetCanPlace(canPlace);
        if (Input.IsActionJustReleased("LeftMouseBtn") && canPlace)
        {
            MainBase mainBase = MainBasePs.Instantiate<MainBase>();
            mainBase.Position = snapPos;
            GetTree().CurrentScene.AddChild(mainBase);
            GameManager.Instance.BuildingGridMap.Place(snapPos, curBuildingPreview.Width, curBuildingPreview.Height);
            curBuildingPreview.QueueFree();
            CurState = PlayerState.Normal;
        }
    }
    private void PreviewFlag(Vector3 snapPos, bool inRange)
    {
        bool canPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height) && inRange;
        curBuildingPreview.SetCanPlace(canPlace);
        if (Input.IsActionJustReleased("LeftMouseBtn") && canPlace)
        {
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
        }
    }
    private void PreviewGoldMaker(Vector3 snapPos, bool inRange)
    {
        bool canPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height) && inRange;
        curBuildingPreview.SetCanPlace(canPlace);
        if (Input.IsActionJustReleased("LeftMouseBtn") && canPlace)
        {
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
        }
    }
    private void PreviewMagicTower(Vector3 snapPos, bool inRange)
    {
        bool canPlace = GameManager.Instance.BuildingGridMap.CanPlace(snapPos, curBuildingPreview.Width, curBuildingPreview.Height) && inRange;
        curBuildingPreview.SetCanPlace(canPlace);
        if (Input.IsActionJustReleased("LeftMouseBtn") && canPlace)
        {
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
        }
    }


    public void TakeGoldCount(int count)
    {
        _goldCount += count;
        GoldCountLb.Text = $"Gold : {_goldCount}";
    }
}