using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsGame.Scripts.Global
{
    public class NodePool
    {
        private readonly PackedScene _scene;
        private readonly Stack<Node3D> _pool = new Stack<Node3D>();
        private readonly Node _parent;

        public NodePool(PackedScene scene, Node parent, int initialSize = 0)
        {
            _scene = scene;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(CreateNewInstance());
            }
        }

        private Node3D CreateNewInstance()
        {
            var node = _scene.Instantiate<Node3D>();
            _parent.AddChild(node);
            node.SetProcess(false);
            node.Hide(); // 初始状态隐藏并禁用
            return node;
        }

        public Node3D Get()
        {
            Node3D node = _pool.Count > 0 ? _pool.Pop() : CreateNewInstance();
            node.Show();
            node.SetProcess(true);
            return node;
        }

        public void Release(Node3D node)
        {
            node.Hide();
            node.SetProcess(false);
            // 这里可以重置单位的状态（如生命值、速度等）
            _pool.Push(node);
        }
    }
}
