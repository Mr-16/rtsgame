using Godot;
using RtsGame.Scripts;
using System.Collections.Generic;

public enum WorkerState
{
    Idle,
    Move,
    ToRes,
    Capture,
    ReturnRes,
}

public enum TargetType
{
    None,
    MainBase,
    Resource,
    Normal,
}

public class WorkerTarget()
{
    public TargetType Type;
    public Vector3 Position;
}

public partial class Worker : UnitBase
{
    private ResourceBase _curRes;
    private WorkerTarget _curTarget = new WorkerTarget();
    private float _captureRange = 4;
    private float _captureRangeSq;

    [Export] Node3D _headNode;
    private ResItemBase _curResItem;
    [Export] private PackedScene _resItemPackedScene;

    [Export] public Node3D ModelRootNode;

    private float _returnResRange = 5;
    private float _returnResRangeSq;

    private BuildingBase _curBuilding;
    private float _buildRange = 5;
    private float _buildRangeSq;

    public override void _Ready()
    {
        base._Ready();
        ChangeState(WorkerState.Idle);
        NaviAgent.AvoidanceEnabled = true;// 连接避障信号
        NaviAgent.VelocityComputed += OnVelocityComputed;
        _captureRangeSq = _captureRange * _captureRange;
        _returnResRangeSq = _returnResRange * _returnResRange;
        _buildRangeSq = _buildRange * _buildRange;
        OwnerPlayer = GameManager.Instance.Player;
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateState((float)delta);
        //GD.Print("_curState : " + _curState);
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
        if (_curTarget.Type == TargetType.Resource)
        {
            ChangeState(WorkerState.ToRes);
            return;
        }
        if(_curTarget.Type == TargetType.MainBase)
        {
            ChangeState(WorkerState.ReturnRes);
            return;
        }
        if (_curTarget.Type == TargetType.Normal)
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
        if (_curTarget.Type == TargetType.Resource)
        {
            ChangeState(WorkerState.ToRes);
            return;
        }
        if (_curTarget.Type == TargetType.MainBase)
        {
            ChangeState(WorkerState.ReturnRes);
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
        _curTarget.Type = TargetType.None;
    }

    //ToRes
    private void EnterToRes()
    {
        AnimPlayer.Play("Move", 0.2f);
    }
    private void UpdateToRes(float delta)
    {
        if (IsInstanceValid(_curRes) == false)
        {
            ChangeState(WorkerState.Idle);
            return;
        }
        if (_curTarget.Type == TargetType.Normal)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        if (_curTarget.Type == TargetType.MainBase)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        if (_curTarget.Position.DistanceSquaredTo(GlobalPosition) <= _captureRangeSq)
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
        _curTarget.Type = TargetType.None;
    }

    //Capture
    private float CaptureDuration = 3f;
    private float CaptureTimer = 0;
    private void EnterCapture()
    {
        AnimPlayer.Play("Capture", 0.2f);
        NaviAgent.Velocity = Vector3.Zero;
        Vector3 lookDirection = (_curTarget.Position - GlobalPosition);
        lookDirection.Y = 0; // 保持水平，防止弯腰或仰头
        ModelRootNode.LookAt(GlobalPosition + lookDirection.Normalized(), Vector3.Up);
    }
    private void UpdateCapture(float delta)
    {
        if (_curTarget.Type == TargetType.Normal)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        if (_curTarget.Type == TargetType.MainBase)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        if (IsInstanceValid(_curRes) == false)
        {
            ChangeState(WorkerState.Idle);
            return;
        }
        if(_curResItem != null)
        {
            float minDistanceSq = float.MaxValue;
            MainBase nearestBase = null;
            foreach (MainBase mainBase in GameManager.Instance.MainBaseList)
            {
                float distSq = GlobalPosition.DistanceSquaredTo(mainBase.GlobalPosition);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    nearestBase = mainBase;
                }
            }
            SetTarget(TargetType.MainBase, nearestBase.GlobalPosition);
            ChangeState(WorkerState.ReturnRes);
            return;
        }
        if (CaptureTimer > CaptureDuration)
        {
            //todo switch case resType
            _curResItem = _resItemPackedScene.Instantiate<ResItemBase>();
            _curResItem.CurCount = _curRes.GetRes();
            _headNode.AddChild(_curResItem);
            float minDistanceSq = float.MaxValue;
            MainBase nearestBase = null;
            foreach (MainBase mainBase in GameManager.Instance.MainBaseList)
            {
                float distSq = GlobalPosition.DistanceSquaredTo(mainBase.GlobalPosition);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    nearestBase = mainBase;
                }
            }
            SetTarget(TargetType.MainBase, nearestBase.GlobalPosition);
            ChangeState(WorkerState.ReturnRes);
            return;
        }
        CaptureTimer += delta;
    }
    private void ExitCapture()
    {
        CaptureTimer = 0;
        
    }

    //ReturnRes
    private void EnterReturnRes()
    {
        AnimPlayer.Play("Move", 0.2f);
    }
    private void UpdateReturnRes(float delta)
    {
        if (_curTarget.Type == TargetType.Normal)
        {
            ChangeState(WorkerState.Move);
            return;
        }
        if (_curTarget.Type == TargetType.Resource)
        {
            ChangeState(WorkerState.ToRes);
            return;
        }
        if (_curTarget.Position.DistanceSquaredTo(GlobalPosition) <= _returnResRangeSq)
        {
            
            if (_curResItem != null)
            {
                OwnerPlayer.ChangeGoldCount(_curResItem.CurCount);
                _curResItem.QueueFree();
                _curResItem = null;
                if (IsInstanceValid(_curRes))
                {
                    SetTarget(TargetType.Resource, _curRes.GlobalPosition);
                    ChangeState(WorkerState.ToRes);
                    return;
                }
                else
                {
                    ChangeState(WorkerState.Idle);
                    return;
                }
            }
        }
        Vector3 nextPathPos = NaviAgent.GetNextPathPosition();
        Vector3 direction = (nextPathPos - GlobalPosition);
        direction.Y = 0; // 锁定 Y 轴，防止单位仰头
        NaviAgent.Velocity = direction.Normalized() * MoveSpeed;// 告诉导航代理我们想往哪走
    }
    private void ExitReturnRes()
    {
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
        Quaternion currentRotation = ModelRootNode.GlobalBasis.GetRotationQuaternion();
        Quaternion nextRotation = currentRotation.Slerp(targetRotation, delta * RotationSpeed);
        ModelRootNode.GlobalBasis = new Basis(nextRotation);
    }
    public override void SetTarget(TargetType type, Vector3 pos)
    {
        _curTarget.Type = type;
        _curTarget.Position = pos;
        NaviAgent.TargetPosition = pos;
    }
    public void SetResource(ResourceBase res)
    {
        _curRes = res;
    }

}
