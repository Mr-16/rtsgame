using Godot;
using RtsGame.Scripts;
using System;
using static Godot.WebSocketPeer;

public enum WorkerState
{
    Idle,
    Move,
    ToRes,
    Capture,
    ReturnRes,
    ToBuild,
    Build,
}

public partial class Worker : UnitBase
{
    private bool _canMove;
    private ResourceBase _targetRes;//资源点 
    private bool _canToRes;
    private float _captureRange = 3;
    private float _captureRangeSq = 3;
    [Export] Node3D _headNode;
    [Export] private PackedScene _resItemPackedScene;
    private Node3D _curResItem;

    private MainBase _targetMainBase;//基地
    private bool _canReturnRes;
    private float _returnResRange = 5;
    private float _returnResRangeSq;
    

    public override void _Ready()
    {
        base._Ready();
        ChangeState(WorkerState.Idle);
        NaviAgent.AvoidanceEnabled = true;// 连接避障信号
        NaviAgent.VelocityComputed += OnVelocityComputed;
        _captureRangeSq = _captureRange * _captureRange;
        _returnResRangeSq = _returnResRange * _returnResRange;
        //Node3D resItemNode = _resItemPackedScene.Instantiate<Node3D>();
        //_headNode.AddChild(resItemNode);
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateState((float)delta);
        GD.Print("_curState : " + _curState);
    }

    private WorkerState? _curState = null;

    private void ChangeState(WorkerState state)
    {
        if (_curState != null)
            ExitState(_curState);
        EnterState(state);
        _curState = state;

    }
    private void EnterState(WorkerState? state)
    {
        switch (state)
        {
            case WorkerState.Idle:
                EnterIdle();
                break;
            case WorkerState.Move:
                EnterMove();
                break;
            case WorkerState.ToRes:
                EnterToRes();
                break;
            case WorkerState.Capture:
                EnterCapture();
                break;
            case WorkerState.ReturnRes:
                EnterReturnRes();
                break;
            case WorkerState.ToBuild:
                break;
            case WorkerState.Build:
                break;
            default:
                break;
        }
    }
    private void UpdateState(float delta)
    {
        switch (_curState)
        {
            case WorkerState.Idle:
                UpdateIdle(delta);
                break;
            case WorkerState.Move:
                UpdateMove(delta);
                break;
            case WorkerState.ToRes:
                UpdateToRes(delta);
                break;
            case WorkerState.Capture:
                UpdateCapture(delta);
                break;
            case WorkerState.ReturnRes:
                UpdateReturnRes(delta);
                break;
            case WorkerState.ToBuild:
                break;
            case WorkerState.Build:
                break;
            default:
                break;
        }
    }
    private void ExitState(WorkerState? state)
    {
        switch (state)
        {
            case WorkerState.Idle:
                ExitIdle();
                break;
            case WorkerState.Move:
                ExitMove();
                break;
            case WorkerState.ToRes:
                ExitToRes();
                break;
            case WorkerState.Capture:
                ExitCapture();
                break;
            case WorkerState.ReturnRes:
                ExitReturnRes();
                break;
            case WorkerState.ToBuild:
                break;
            case WorkerState.Build:
                break;
            default:
                break;
        }
    }

    //Idle
    private void EnterIdle()
    {
        AnimPlayer.Play("Idle", 0.2f);
    }
    private void UpdateIdle(float delta)
    {
        if (_canToRes)
        {
            ChangeState(WorkerState.ToRes);
            return;
        }
        if (_canMove == true)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        
    }
    private void ExitIdle()
    {
    }

    //Move
    private void EnterMove()
    {
        AnimPlayer.Play("Move", 0.2f);
    }
    private void UpdateMove(float delta)
    {
        if (_canToRes)
        {
            ChangeState(WorkerState.ToRes);
            return;
        }
        if (NaviAgent.IsNavigationFinished())
        {
            ChangeState(WorkerState.Idle);
            return;
        }
        Vector3 nextPathPos = NaviAgent.GetNextPathPosition();
        Vector3 direction = (nextPathPos - GlobalPosition);
        direction.Y = 0; // 锁定 Y 轴，防止单位仰头
        NaviAgent.Velocity = direction.Normalized() * MoveSpeed;// 告诉导航代理我们想往哪走
    }
    private void ExitMove()
    {
        _canMove = false;
    }

    //ToRes
    private void EnterToRes()
    {
        AnimPlayer.Play("Move", 0.2f);
    }
    private void UpdateToRes(float delta)
    {
        if (_targetRes.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= _captureRangeSq)
        {
            ChangeState(WorkerState.Capture);
            return;
        }
        Vector3 nextPathPos = NaviAgent.GetNextPathPosition();
        Vector3 direction = (nextPathPos - GlobalPosition);
        direction.Y = 0; // 锁定 Y 轴，防止单位仰头
        NaviAgent.Velocity = direction.Normalized() * MoveSpeed;// 告诉导航代理我们想往哪走
    }
    private void ExitToRes()
    {
        NaviAgent.Velocity = Vector3.Zero;
        _canToRes = false;
    }

    //Capture
    private float CaptureDuration = 3f;
    private float CaptureTimer = 0;
    private void EnterCapture()
    {
        AnimPlayer.Play("Capture", 0.2f);
        NaviAgent.Velocity = Vector3.Zero;
        Vector3 lookDirection = (_targetRes.GlobalPosition - GlobalPosition);
        lookDirection.Y = 0; // 保持水平，防止弯腰或仰头
        LookAt(GlobalPosition + lookDirection.Normalized(), Vector3.Up);
    }
    private void UpdateCapture(float delta)
    {
        if (CaptureTimer > CaptureDuration)
        {
            _curResItem = _resItemPackedScene.Instantiate<Node3D>();
            _headNode.AddChild(_curResItem);
            ChangeState(WorkerState.ReturnRes);
            return;
        }
        CaptureTimer += delta;
    }
    private void ExitCapture()
    {
        CaptureTimer = 0;
        if (_targetMainBase == null)
        {
            float minDistanceSq = float.MaxValue;
            MainBase nearestBase = null;
            Vector3 currentPos = GlobalPosition; // 或者 GlobalTransform.Origin
            foreach (MainBase mainBase in GameManager.Instance.MainBaseList)
            {
                float distSq = currentPos.DistanceSquaredTo(mainBase.GlobalPosition);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    nearestBase = mainBase;
                }
            }
            _targetMainBase = nearestBase;
        }
        NaviAgent.TargetPosition = _targetMainBase.GlobalPosition;
    }

    //ReturnRes
    private void EnterReturnRes()
    {
        AnimPlayer.Play("Move", 0.2f);
        
    }
    private void UpdateReturnRes(float delta)
    {
        if (_targetMainBase.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= _returnResRangeSq)
        {
            _curResItem.QueueFree();
            SetTargetPos(_targetRes.GlobalPosition);
            ChangeState(WorkerState.ToRes);
            return;
        }
        Vector3 nextPathPos = NaviAgent.GetNextPathPosition();
        Vector3 direction = (nextPathPos - GlobalPosition);
        direction.Y = 0; // 锁定 Y 轴，防止单位仰头
        NaviAgent.Velocity = direction.Normalized() * MoveSpeed;// 告诉导航代理我们想往哪走
    }
    private void ExitReturnRes()
    {
        _canToRes = false;
        _targetMainBase = null;
        NaviAgent.Velocity = Vector3.Zero;
    }



    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        // 过滤微小抖动
        if (safeVelocity.Length() < 0.1f) return;

        float delta = (float)GetPhysicsProcessDeltaTime();

        // 1. 处理旋转 (修复了报错的地方)
        SmoothLookAt(safeVelocity.Normalized(), delta);

        // 2. 处理移动
        Vector3 horizontalVelocity = new Vector3(safeVelocity.X, 0, safeVelocity.Z);
        GlobalPosition += horizontalVelocity * delta;
    }

    private void SmoothLookAt(Vector3 forwardDirection, float delta)
    {
        Basis targetBasis = Basis.LookingAt(forwardDirection, Vector3.Up);
        Quaternion targetRotation = targetBasis.GetRotationQuaternion();
        Quaternion currentRotation = GlobalBasis.GetRotationQuaternion();
        Quaternion nextRotation = currentRotation.Slerp(targetRotation, delta * RotationSpeed);
        GlobalBasis = new Basis(nextRotation);
    }
    public override void SetTargetPos(Vector3 targetWorldPos)
    {
        _canMove = true;
        NaviAgent.TargetPosition = targetWorldPos;
    }
    public void SetTargetRes(ResourceBase targetRes)
    {
        _canToRes = true;
        _targetRes = targetRes;
    }
    public void SetTargetBase(MainBase targetBase)
    {
        _canReturnRes = true;
        _targetMainBase = targetBase;
    }
}
