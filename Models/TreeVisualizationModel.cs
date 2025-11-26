// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tools.Models;

public enum TreeType {
    [Description("普通树")] GeneralTree,

    [Description("二叉树")] BinaryTree,

    [Description("二叉排序树")] BinarySearchTree
}

public enum TraversalType {
    [Description("先序遍历")] PreOrder,

    [Description("中序遍历")] InOrder,

    [Description("后序遍历")] PostOrder,

    [Description("层次遍历")] LevelOrder
}

public partial class TreeData : ObservableObject {
    private const double MinNodeSpacing = 50; // 节点最小水平间距
    private const double VerticalSpacing = 80; // 层级垂直间距
    private const double NodeRadius = 20; // 节点半径（用于间距计算）

    public partial class TreeNode : ObservableObject {
        [ObservableProperty] private string _value = string.Empty;
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;
        [ObservableProperty] private TreeNode? _parent;
        [ObservableProperty] private TreeNode? _leftChild;
        [ObservableProperty] private TreeNode? _rightChild;
        [ObservableProperty] private ObservableCollection<TreeNode> _children = new();
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isTarget;

        // 布局辅助字段：记录子树宽度（用于动态分配空间）
        internal double SubtreeWidth { get; set; }

        public override string ToString() => Value;
    }

    public partial class TreeConnection : ObservableObject {
        [ObservableProperty] private TreeNode? _startNode;
        [ObservableProperty] private TreeNode? _endNode;
    }

    [ObservableProperty] private TreeNode? _root;
    [ObservableProperty] private ObservableCollection<TreeNode> _nodes = new();
    [ObservableProperty] private ObservableCollection<TreeConnection> _connections = new();
    [ObservableProperty] private string _treeInfo = "节点数: 0, 高度: 0";
    [ObservableProperty] private double _canvasWidth = 800;
    [ObservableProperty] private double _canvasHeight = 600;
    private const int MillisecondsDelay = 500;

    private readonly Random _random = new();

    public void SetCanvasSize(double width, double height) {
        CanvasWidth = width;
        CanvasHeight = height;
        UpdateLayout();
    }

    public void AddBinaryNode(string value, TreeNode? parent, bool insertLeft) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("节点值不能为空");
        }

        var newNode = new TreeNode { Value = value, X = 0, Y = 0 };

        if (parent == null) {
            if (Root != null) {
                throw new InvalidOperationException("根节点已存在");
            }

            Root = newNode;
        } else {
            if (insertLeft) {
                if (parent.LeftChild != null) {
                    throw new InvalidOperationException("左子节点已存在");
                }

                parent.LeftChild = newNode;
            } else {
                if (parent.RightChild != null) {
                    throw new InvalidOperationException("右子节点已存在");
                }

                parent.RightChild = newNode;
            }

            newNode.Parent = parent;
        }

        Nodes.Add(newNode);
        UpdateLayout();
        UpdateTreeInfo();
    }

    public void AddGeneralNode(string value, TreeNode? parent) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("节点值不能为空");
        }

        var newNode = new TreeNode { Value = value, X = 0, Y = 0 };

        if (parent == null) {
            if (Root != null) {
                throw new InvalidOperationException("根节点已存在");
            }

            Root = newNode;
        } else {
            parent.Children.Add(newNode);
            newNode.Parent = parent;
        }

        Nodes.Add(newNode);
        UpdateLayout();
        UpdateTreeInfo();
    }

    private void AddBinarySearchNode(TreeNode node, TreeNode? parent) {
        if (parent == null) {
            if (Root != null) {
                throw new InvalidOperationException("根节点已存在");
            }

            Root = node;
        } else {
            int comparison = int.Parse(node.Value) - int.Parse(parent.Value);
            if (comparison < 0) {
                if (parent.LeftChild != null) {
                    AddBinarySearchNode(node, parent.LeftChild);
                    return;
                }

                parent.LeftChild = node;
            } else if (comparison > 0) {
                if (parent.RightChild != null) {
                    AddBinarySearchNode(node, parent.RightChild);
                    return;
                }

                parent.RightChild = node;
            } else {
                throw new InvalidOperationException("二叉搜索树中不能有重复值");
            }

            node.Parent = parent;
        }

        Nodes.Add(node);
        UpdateLayout();
        UpdateTreeInfo();
    }

    public void AddBinarySearchNode(string value, TreeNode? parent) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("节点值不能为空");
        }

        var newNode = new TreeNode { Value = value, X = 0, Y = 0 };

        AddBinarySearchNode(newNode, parent);
    }

    public void Clear() {
        Root = null;
        Nodes.Clear();
        Connections.Clear();
        TreeInfo = "节点数: 0, 高度: 0";
    }

    public async Task RandomGenerate(int nodeCount, TreeType treeType) {
        Clear();
        if (nodeCount <= 0) {
            return;
        }

        var values = GenerateRandomValues(nodeCount);
        try {
            switch (treeType) {
                case TreeType.BinaryTree:
                    await GenerateRandomBinaryTree(values);
                    break;
                case TreeType.BinarySearchTree:
                    await GenerateRandomBinarySearchTree(values);
                    break;
                case TreeType.GeneralTree:
                    await GenerateRandomGeneralTree(values);
                    break;
            }

            UpdateLayout();
            UpdateTreeInfo();
        } catch (Exception ex) {
            throw new InvalidOperationException("随机生成树失败", ex);
        }
    }

    public List<string> Traverse(TraversalType traversalType) {
        var result = new List<string>();
        if (Root == null) {
            return result;
        }

        switch (traversalType) {
            case TraversalType.PreOrder:
                PreOrderTraversal(Root, result);
                break;
            case TraversalType.InOrder:
                InOrderTraversal(Root, result);
                break;
            case TraversalType.PostOrder:
                PostOrderTraversal(Root, result);
                break;
            case TraversalType.LevelOrder:
                LevelOrderTraversal(Root, result);
                break;
            default:
                throw new InvalidEnumArgumentException("枚举值越界，未知错误");
        }

        return result;
    }

    /// <summary>
    /// 获取树的层级节点列表（每层的节点集合）
    /// </summary>
    private void LevelGet(TreeNode? node, List<List<TreeNode>> result, int level) {
        if (node == null) {
            return;
        }

        if (result.Count <= level) {
            result.Add(new List<TreeNode>());
        }

        result[level].Add(node);
        LevelGet(node.LeftChild, result, level + 1);
        LevelGet(node.RightChild, result, level + 1);
        foreach (var nodeChild in node.Children) {
            LevelGet(nodeChild, result, level + 1);
        }
    }

    private void UpdateLayout() {
        Connections.Clear();
        if (Root == null) {
            return;
        }

        // 先计算子树宽度（为动态布局做准备）
        if (Nodes.Any(n => n.Children.Count > 0)) {
            CalculateGeneralSubtreeWidth(Root);
        } else {
            CalculateBinarySubtreeWidth(Root);
        }

        // 获取树的层级节点（用于计算整体偏移，让树居中）
        var levels = new List<List<TreeNode>>();
        LevelGet(Root, levels, 0);
        int treeHeight = levels.Count;

        // 计算整体垂直偏移：让树在画布垂直方向居中
        double totalVerticalSpace = (treeHeight - 1) * VerticalSpacing + 2 * NodeRadius;
        double startY = Math.Max(NodeRadius, (CanvasHeight - totalVerticalSpace) / 2);

        // 执行布局算法
        if (Nodes.Any(n => n.Children.Count > 0)) {
            LayoutGeneralTree(Root, startY, 0, Root.SubtreeWidth);
        } else {
            LayoutBinaryTree(Root, startY, 0, Root.SubtreeWidth);
        }

        // 调整整体水平偏移：让树在画布水平方向居中
        AdjustTreeHorizontalCenter(levels);

        // 检查并限制树的尺寸不超过画布
        LimitTreeToCanvas();

        // 更新连接线
        UpdateConnections(Root);
    }

    /// <summary>
    /// 限制树的整体尺寸不超过画布大小
    /// </summary>
    private void LimitTreeToCanvas() {
        if (Nodes.Count == 0) return;

        // 找到当前节点的边界
        double minX = Nodes.Min(n => n.X);
        double maxX = Nodes.Max(n => n.X);
        double minY = Nodes.Min(n => n.Y);
        double maxY = Nodes.Max(n => n.Y);

        double treeWidth = maxX - minX;
        double treeHeight = maxY - minY;

        // 如果树尺寸超过画布，进行缩放
        if (treeWidth > CanvasWidth || treeHeight > CanvasHeight) {
            // 计算缩放比例（保留10%的边距）
            double scaleX = CanvasWidth * 0.9 / Math.Max(treeWidth, 1);
            double scaleY = CanvasHeight * 0.9 / Math.Max(treeHeight, 1);
            double scale = Math.Min(scaleX, scaleY);

            // 计算树的中心点
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            // 计算目标中心点（画布中心）
            double targetCenterX = CanvasWidth / 2;
            double targetCenterY = CanvasHeight / 2;

            // 应用缩放：以树中心为基准进行缩放，然后平移到画布中心
            foreach (var node in Nodes) {
                // 相对于树中心进行缩放
                double scaledX = (node.X - centerX) * scale + targetCenterX;
                double scaledY = (node.Y - centerY) * scale + targetCenterY;

                node.X = scaledX;
                node.Y = scaledY;
            }
        }
    }

    /// <summary>
    /// 调整树的整体水平位置，使其在画布中居中
    /// </summary>
    private void AdjustTreeHorizontalCenter(List<List<TreeNode>> levels) {
        if (levels.Count == 0) return;

        // 找到树的最左和最右X坐标
        double minX = levels.SelectMany(level => level).Min(n => n.X);
        double maxX = levels.SelectMany(level => level).Max(n => n.X);
        double treeTotalWidth = maxX - minX;

        // 如果树宽度小于画布，则居中；否则左对齐（会在LimitTreeToCanvas中处理缩放）
        if (treeTotalWidth < CanvasWidth) {
            double offsetX = (CanvasWidth - treeTotalWidth) / 2 - minX;

            // 应用偏移
            foreach (var node in Nodes) {
                node.X += offsetX;
            }
        }
    }

    /// <summary>
    /// 二叉树布局：基于子树宽度动态分配位置，避免空间浪费
    /// </summary>
    private void LayoutBinaryTree(TreeNode? node, double y, double leftOffset, double parentSubtreeWidth) {
        if (node == null) {
            return;
        }

        // 当前节点的X坐标：父节点分配的空间中点
        node.X = leftOffset + parentSubtreeWidth / 2;
        node.Y = y;

        // 检查Y坐标是否超出画布底部
        if (node.Y > CanvasHeight - NodeRadius - 20) {
            node.Y = CanvasHeight - NodeRadius - 20;
            return; // 超出画布底部，不再布局子节点
        }

        double childY = y + VerticalSpacing;
        double leftChildWidth = node.LeftChild?.SubtreeWidth ?? 0;
        double rightChildWidth = node.RightChild?.SubtreeWidth ?? 0;

        // 分配左子节点位置：从leftOffset开始，占用leftChildWidth空间
        if (node.LeftChild != null) {
            double leftChildOffset = leftOffset;
            LayoutBinaryTree(node.LeftChild, childY, leftChildOffset, leftChildWidth);
        }

        // 分配右子节点位置：左子树结束后 + 最小间距，占用rightChildWidth空间
        if (node.RightChild != null) {
            double rightChildOffset = leftOffset + leftChildWidth + MinNodeSpacing;
            LayoutBinaryTree(node.RightChild, childY, rightChildOffset, rightChildWidth);
        }
    }

    /// <summary>
    /// 普通树布局：按子树宽度比例分配空间，而非均分
    /// </summary>
    private void LayoutGeneralTree(TreeNode? node, double y, double leftOffset, double parentSubtreeWidth) {
        if (node == null) {
            return;
        }

        node.X = leftOffset + parentSubtreeWidth / 2;
        node.Y = y;

        // 检查Y坐标是否超出画布底部
        if (node.Y > CanvasHeight - NodeRadius - 20) {
            node.Y = CanvasHeight - NodeRadius - 20;
            return; // 超出画布底部，不再布局子节点
        }

        if (node.Children.Count == 0) {
            return;
        }

        double childY = y + VerticalSpacing;

        // 检查子节点Y坐标是否超出画布底部
        if (childY > CanvasHeight - NodeRadius - 20) {
            return; // 超出画布底部，不布局子节点
        }

        double currentOffset = leftOffset;

        // 为每个子节点分配其实际需要的空间
        foreach (var child in node.Children) {
            LayoutGeneralTree(child, childY, currentOffset, child.SubtreeWidth);
            currentOffset += child.SubtreeWidth + MinNodeSpacing;
        }
    }

    /// <summary>
    /// 计算二叉树节点的子树宽度（包含自身和子节点的总水平空间）
    /// </summary>
    private void CalculateBinarySubtreeWidth(TreeNode? node) {
        if (node == null) {
            return;
        }

        // 叶子节点的宽度为自身占用空间
        if (node.LeftChild == null && node.RightChild == null) {
            node.SubtreeWidth = MinNodeSpacing;
            return;
        }

        // 递归计算左右子树宽度
        CalculateBinarySubtreeWidth(node.LeftChild);
        CalculateBinarySubtreeWidth(node.RightChild);

        double leftWidth = node.LeftChild?.SubtreeWidth ?? 0;
        double rightWidth = node.RightChild?.SubtreeWidth ?? 0;

        // 当前节点的子树宽度 = 左子树宽度 + 右子树宽度 + 节点间距
        double calculatedWidth = leftWidth + rightWidth + MinNodeSpacing;

        // 限制最大宽度不超过画布宽度（减去边距）
        double maxAllowedWidth = CanvasWidth - 2 * MinNodeSpacing;
        node.SubtreeWidth = Math.Min(calculatedWidth, maxAllowedWidth);
    }

    /// <summary>
    /// 计算普通树节点的子树宽度
    /// </summary>
    private void CalculateGeneralSubtreeWidth(TreeNode? node) {
        if (node == null) {
            return;
        }

        if (node.Children.Count == 0) {
            node.SubtreeWidth = MinNodeSpacing;
            return;
        }

        // 递归计算所有子节点的子树宽度
        double totalChildWidth = 0;
        foreach (var child in node.Children) {
            CalculateGeneralSubtreeWidth(child);
            totalChildWidth += child.SubtreeWidth;
        }

        // 当前节点的子树宽度 = 所有子节点宽度之和 + 子节点之间的间距
        double calculatedWidth = totalChildWidth + (node.Children.Count - 1) * MinNodeSpacing;

        // 限制最大宽度不超过画布宽度（减去边距）
        double maxAllowedWidth = CanvasWidth - 2 * MinNodeSpacing;
        node.SubtreeWidth = Math.Min(calculatedWidth, maxAllowedWidth);
    }

    private void UpdateConnections(TreeNode? node) {
        if (node == null) {
            return;
        }

        if (node.LeftChild != null) {
            Connections.Add(new TreeConnection { StartNode = node, EndNode = node.LeftChild });
            UpdateConnections(node.LeftChild);
        }

        if (node.RightChild != null) {
            Connections.Add(new TreeConnection { StartNode = node, EndNode = node.RightChild });
            UpdateConnections(node.RightChild);
        }

        foreach (var child in node.Children) {
            Connections.Add(new TreeConnection { StartNode = node, EndNode = child });
            UpdateConnections(child);
        }
    }

    private void UpdateTreeInfo() {
        int height = Root != null ? CalculateHeight(Root) : 0;
        TreeInfo = $"节点数: {Nodes.Count}, 高度: {height}";
    }

    private int CalculateHeight(TreeNode? node) {
        if (node == null) {
            return 0;
        }

        if (node.LeftChild != null || node.RightChild != null) {
            int leftHeight = CalculateHeight(node.LeftChild);
            int rightHeight = CalculateHeight(node.RightChild);
            return Math.Max(leftHeight, rightHeight) + 1;
        }

        if (node.Children.Count > 0) {
            int maxChildHeight = 0;
            foreach (var child in node.Children) {
                maxChildHeight = Math.Max(maxChildHeight, CalculateHeight(child));
            }

            return maxChildHeight + 1;
        }

        return 1;
    }

    private List<string> GenerateRandomValues(int count) {
        var values = new List<string>();
        var hasValue = new HashSet<string>();
        for (int i = 0; i < count; i++) {
            var s = _random.Next(1, 100).ToString();
            while (hasValue.Contains(s)) {
                s = _random.Next(1, 100).ToString();
            }

            values.Add(s);
            hasValue.Add(s);
        }

        return values;
    }

    private async Task GenerateRandomBinaryTree(List<string> values) {
        if (values.Count == 0) {
            return;
        }

        var queue = new Queue<TreeNode>();
        Root = new TreeNode { Value = values[0], X = 0, Y = 0 };
        Nodes.Add(Root);
        queue.Enqueue(Root);

        int index = 1;
        while (queue.Count > 0 && index < values.Count) {
            var current = queue.Dequeue();

            if (index < values.Count) {
                current.LeftChild = new TreeNode { Value = values[index], X = 0, Y = 0, Parent = current };
                Nodes.Add(current.LeftChild);
                queue.Enqueue(current.LeftChild);
                index++;
                UpdateLayout();
                UpdateTreeInfo();
                await Task.Delay(MillisecondsDelay);
            }

            if (index < values.Count) {
                current.RightChild = new TreeNode { Value = values[index], X = 0, Y = 0, Parent = current };
                Nodes.Add(current.RightChild);
                queue.Enqueue(current.RightChild);
                index++;
                UpdateLayout();
                UpdateTreeInfo();
                await Task.Delay(MillisecondsDelay);
            }
        }
    }

    private async Task GenerateRandomBinarySearchTree(List<string> values) {
        if (values.Count == 0) {
            return;
        }

        Root = new TreeNode { Value = values[0], X = 0, Y = 0 };
        Nodes.Add(Root);

        for (int i = 1; i < values.Count; i++) {
            AddBinarySearchNode(values[i], Root);
            await Task.Delay(MillisecondsDelay);
        }
    }

    private async Task GenerateRandomGeneralTree(List<string> values) {
        if (values.Count == 0) {
            return;
        }

        Root = new TreeNode { Value = values[0], X = 0, Y = 0 };
        Nodes.Add(Root);

        var queue = new Queue<TreeNode>();
        queue.Enqueue(Root);

        int index = 1;
        while (queue.Count > 0 && index < values.Count) {
            var current = queue.Dequeue();
            int childCount = _random.Next(1, 4);

            for (int i = 0; i < childCount && index < values.Count; i++) {
                var child = new TreeNode { Value = values[index], X = 0, Y = 0, Parent = current };
                current.Children.Add(child);
                Nodes.Add(child);
                queue.Enqueue(child);
                index++;
                await Task.Delay(MillisecondsDelay);
            }

            UpdateLayout();
            UpdateTreeInfo();
        }
    }

    private void PreOrderTraversal(TreeNode? node, List<string> result) {
        if (node == null) {
            return;
        }

        result.Add(node.Value);
        PreOrderTraversal(node.LeftChild, result);
        PreOrderTraversal(node.RightChild, result);
        foreach (var child in node.Children) {
            PreOrderTraversal(child, result);
        }
    }

    private void InOrderTraversal(TreeNode? node, List<string> result) {
        if (node == null) {
            return;
        }

        InOrderTraversal(node.LeftChild, result);
        result.Add(node.Value);
        InOrderTraversal(node.RightChild, result);
    }

    private void PostOrderTraversal(TreeNode? node, List<string> result) {
        if (node == null) {
            return;
        }

        PostOrderTraversal(node.LeftChild, result);
        PostOrderTraversal(node.RightChild, result);
        foreach (var child in node.Children) {
            PostOrderTraversal(child, result);
        }

        result.Add(node.Value);
    }

    private void LevelOrderTraversal(TreeNode root, List<string> result) {
        var queue = new Queue<TreeNode>();
        queue.Enqueue(root);

        while (queue.Count > 0) {
            var current = queue.Dequeue();
            result.Add(current.Value);

            if (current.LeftChild != null) {
                queue.Enqueue(current.LeftChild);
            }

            if (current.RightChild != null) {
                queue.Enqueue(current.RightChild);
            }

            foreach (var child in current.Children) {
                queue.Enqueue(child);
            }
        }
    }

    public async Task<TreeNode?> SearchNode(string value, TreeNode? node, bool needShow = true) {
        foreach (var treeNode in Nodes) {
            treeNode.IsSelected = false;
            treeNode.IsTarget = false;
        }

        if (node == null) {
            return null;
        }

        node.IsSelected = needShow;
        await Task.Delay((int)(MillisecondsDelay * (needShow ? 0.8 : 0)));
        if (node.Value == value) {
            node.IsTarget = needShow;
            return node;
        }

        node.IsSelected = false;
        return await SearchNode(value,
            int.Parse(value) < int.Parse(node.Value) ? node.LeftChild : node.RightChild, needShow);
    }

    public async Task DeleteNode(string value, TreeNode? node) {
        if (node == null || Root == null) {
            return;
        }

        // 查找要删除的节点
        TreeNode? nodeToDelete = await SearchNode(value, node, false);
        if (nodeToDelete == null) {
            return; // 未找到要删除的节点
        }

        // 记录被删除的节点，用于后续从集合中移除
        TreeNode? deletedNode;

        // 情况1：叶子节点
        if (nodeToDelete.LeftChild == null && nodeToDelete.RightChild == null) {
            deletedNode = DeleteLeafNode(nodeToDelete);
        } else if (nodeToDelete.LeftChild == null || nodeToDelete.RightChild == null) {
            deletedNode = DeleteNodeWithOneChild(nodeToDelete);
        } else {
            deletedNode = await DeleteNodeWithTwoChildren(nodeToDelete);
        }

        // 从节点集合中移除被删除的节点
        Nodes.Remove(deletedNode);

        // 如果删除的是根节点，更新根节点引用
        if (deletedNode == Root) {
            Root = null;
        }

        // 更新布局和树信息
        UpdateLayout();
        UpdateTreeInfo();
    }

    public async Task DeleteNode2(string value, TreeNode? node) {
        if (node == null || Root == null) {
            return;
        }

        // 查找要删除的节点
        TreeNode? nodeToDelete = await SearchNode(value, node, false);
        if (nodeToDelete == null) {
            return; // 未找到要删除的节点
        }

        TreeNode? left = nodeToDelete.LeftChild;
        TreeNode? right = nodeToDelete.RightChild;
        TreeNode? parent = nodeToDelete.Parent;
        if (parent != null) {
            if (parent.LeftChild == nodeToDelete) {
                parent.LeftChild = null;
            } else if (parent.RightChild == nodeToDelete) {
                parent.RightChild = null;
            }
        }

        if (left != null) {
            AddBinarySearchNode(left, Root);
            Nodes.Remove(left);
        }

        if (right != null) {
            AddBinarySearchNode(right, Root);
            Nodes.Remove(right);
        }


        // 从节点集合中移除被删除的节点
        Nodes.Remove(nodeToDelete);

        // 如果删除的是根节点，更新根节点引用
        if (nodeToDelete == Root) {
            Root = null;
        }

        // 更新布局和树信息
        UpdateLayout();
        UpdateTreeInfo();
    }

    private TreeNode DeleteLeafNode(TreeNode nodeToDelete) {
        TreeNode? parent = nodeToDelete.Parent;

        if (parent != null) {
            if (parent.LeftChild == nodeToDelete) {
                parent.LeftChild = null;
            } else if (parent.RightChild == nodeToDelete) {
                parent.RightChild = null;
            }
        }

        nodeToDelete.Parent = null;
        return nodeToDelete;
    }

    private TreeNode DeleteNodeWithOneChild(TreeNode nodeToDelete) {
        TreeNode? parent = nodeToDelete.Parent;
        TreeNode? child = nodeToDelete.LeftChild ?? nodeToDelete.RightChild;

        if (child != null) {
            child.Parent = parent;
        }

        if (parent != null) {
            if (parent.LeftChild == nodeToDelete) {
                parent.LeftChild = child;
            } else if (parent.RightChild == nodeToDelete) {
                parent.RightChild = child;
            }
        } else {
            // 如果删除的是根节点，更新根节点引用
            Root = child;
        }

        nodeToDelete.Parent = null;
        nodeToDelete.LeftChild = null;
        nodeToDelete.RightChild = null;
        return nodeToDelete;
    }

    private async Task<TreeNode> DeleteNodeWithTwoChildren(TreeNode nodeToDelete) {
        // 找到右子树中的最小节点（中序后继）
        TreeNode successor = FindMinNode(nodeToDelete.RightChild!);
        // 展示搜索过程
        await SearchNode(successor.Value, nodeToDelete);

        await Task.Delay((int)(MillisecondsDelay * 0.5));
        nodeToDelete.Value = successor.Value;
        await Task.Delay((int)(MillisecondsDelay * 0.8));
        await DeleteNode(successor.Value, nodeToDelete.RightChild);

        return successor;
    }


    private TreeNode FindMinNode(TreeNode node) {
        TreeNode current = node;
        while (current.LeftChild != null) {
            current = current.LeftChild;
        }

        return current;
    }
}