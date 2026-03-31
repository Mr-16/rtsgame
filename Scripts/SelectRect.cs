using Godot;
using System;

public partial class SelectRect : Control
{
    public bool IsSelecting = false;
    public Vector2 StartPos;
    public Vector2 EndPos;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!IsSelecting) return;
        Rect2 rect = new Rect2(StartPos, EndPos - StartPos).Abs();
        DrawRect(rect, new Color(0, 1, 0, 0.10f), true); // 填充
        DrawRect(rect, Colors.Green, false, 2);        // 边框
    }
}
