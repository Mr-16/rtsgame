using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Godot.Node;

namespace RtsGame.Scripts.Global
{
    public class NodePool
    {
        private readonly PackedScene _scene;
        private readonly Node _parent;
        private readonly Stack<Node3D> _pool = new();

        public NodePool(PackedScene scene, Node parent, int initialSize)
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
            Deactivate(node);
            return node;
        }

        public Node3D Get()
        {
            var node = _pool.Count > 0 ? _pool.Pop() : CreateNewInstance();
            Activate(node);
            return node;
        }

        public void Release(Node3D node)
        {
            Deactivate(node);
            _pool.Push(node);
        }

        private void Activate(Node3D node)
        {
            node.Show();
            node.ProcessMode = ProcessModeEnum.Inherit;
        }

        private void Deactivate(Node3D node)
        {
            node.Hide();
            node.ProcessMode = ProcessModeEnum.Disabled;
        }
    }
}
