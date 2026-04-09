using Godot;
using System;
using System.Collections.Generic;

namespace RtsGame.Scripts
{
    public partial class BuildingPreviewBase : Node3D
    {
        [Export] public int Width = 3;
        [Export] public int Height = 3;

        private List<StandardMaterial3D> _materialList = new();
        // 记录原始透明度状态，以便恢复
        private List<BaseMaterial3D.TransparencyEnum> _defaultTransparencyList = new();

        public override void _Ready()
        {
            // 清理列表，防止在编辑器内运行或重载时重复添加
            _materialList.Clear();
            _defaultTransparencyList.Clear();

            InitializeMaterials(this);
        }

        private void InitializeMaterials(Node root)
        {
            foreach (var child in root.GetChildren())
            {
                //GD.Print($"正在检查节点: {child.Name} 类型: {child.GetType()}");

                if (child is MeshInstance3D meshInstance)
                {
                    if (meshInstance.Mesh == null) continue;

                    int surfaceCount = meshInstance.Mesh.GetSurfaceCount();
                    //GD.Print($"找到 Mesh: {meshInstance.Name}, Surface数量: {surfaceCount}");

                    for (int i = 0; i < surfaceCount; i++)
                    {
                        var mat = meshInstance.GetActiveMaterial(i);
                        if (mat != null)
                        {
                            if (mat is StandardMaterial3D stdMaterial)
                            {
                                var uniqueMat = (StandardMaterial3D)stdMaterial.Duplicate();
                                meshInstance.SetSurfaceOverrideMaterial(i, uniqueMat);

                                _materialList.Add(uniqueMat);
                            }
                        }
                    }
                }

                // 递归
                if (child.GetChildCount() > 0)
                {
                    InitializeMaterials(child);
                }
            }
        }

        public void SetCanPlace(bool canPlace)
        {
            for (int i = 0; i < _materialList.Count; i++)
            {
                var mat = _materialList[i];
                if (canPlace)
                {
                    // 变为红色半透明
                    mat.AlbedoColor = new Color(0.0f, 1.0f, 0.0f, 0.5f);

                    // 如果原本不是透明的，必须开启 Alpha 混合，否则半透明无效
                    if (mat.Transparency == BaseMaterial3D.TransparencyEnum.Disabled)
                    {
                        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    }
                }
                else
                {
                    // 变为红色半透明
                    mat.AlbedoColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);

                    // 如果原本不是透明的，必须开启 Alpha 混合，否则半透明无效
                    if (mat.Transparency == BaseMaterial3D.TransparencyEnum.Disabled)
                    {
                        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    }
                }
            }
        }
    }
}