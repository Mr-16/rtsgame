using Godot;
using System;

public partial class DebugLb : Label
{
    public override void _Process(double delta)
    {
        // 1. 获取帧率
        int fps = (int)Engine.GetFramesPerSecond();

        // 2. 获取 CPU 处理耗时 (单位：秒，转换为毫秒 ms)
        // Process 指的是常规脚本逻辑耗时
        double processTime = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;

        // Physics Process 指的是物理引擎（FixedUpdate）耗时
        double physicsTime = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;

        // 3. 静态内存（当前 CPU 维护的对象所占空间）
        double mem = OS.GetStaticMemoryUsage() / 1024.0 / 1024.0;

        // 拼接信息
        Text = $"[CPU Performance]\n" +
               $"FPS: {fps}\n" +
               $"Main Process: {processTime:F3} ms\n" +
               $"Physics Process: {physicsTime:F3} ms\n" +
               $"Static Mem: {mem:F2} MB\n" +
               $"Draw Calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)}";

        // 性能警告：如果逻辑耗时超过 16.6ms (60帧标准)，文字变红
        //Modulate = processTime > 16.6 ? Colors.Red : Colors.White;
    }
}
