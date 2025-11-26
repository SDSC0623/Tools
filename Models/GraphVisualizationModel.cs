// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace Tools.Models;

public enum GraphType {
    [Description("有向图")] Directed,
    [Description("无向图")] Undirected
}

// 图数据类 - 整合节点和边
public partial class GraphData : ObservableObject {
    // 节点类作为图的内部类
    public partial class Node : ObservableObject {
        [ObservableProperty] private double _x;

        [ObservableProperty] private double _y;

        [ObservableProperty] private string _label = string.Empty;

        [ObservableProperty] private bool _isSelected;


        public Node(string label, double x = 0, double y = 0) {
            Label = label;
            X = x;
            Y = y;
        }
    }

    // 边类作为图的内部类
    public partial class Edge : ObservableObject {
        [ObservableProperty] private Node _startNode;

        [ObservableProperty] private Node _endNode;

        [ObservableProperty] private int _weight;

        public Edge(Node start, Node end, int weight) {
            StartNode = start;
            EndNode = end;
            Weight = weight;
        }
    }

    // 图的节点和边集合
    [ObservableProperty] private ObservableCollection<Node> _nodes = [];

    [ObservableProperty] private ObservableCollection<Edge> _edges = [];

    // 布局参数
    private double _canvasWidth = 800;
    private double _canvasHeight = 500;
    private double _baseRadius = 200;

    // 图信息属性
    private int NodeCount => Nodes.Count;
    private int EdgeCount => Edges.Count;
    public string GraphInfo => $"节点: {NodeCount}, 边: {EdgeCount}";

    public GraphData() {
        Nodes.CollectionChanged += (_, _) => ApplyCircularLayout();
        SetCanvasSize(_canvasWidth, _canvasHeight);
    }

    public void SetCanvasSize(double width, double height) {
        _canvasWidth = width;
        _canvasHeight = height;
        _baseRadius = Math.Min(_canvasWidth / 4.0, _canvasHeight / 4.0);
        ApplyCircularLayout();
    }

    // 圆形布局算法
    private void ApplyCircularLayout() {
        if (Nodes.Count == 0) {
            return;
        }

        var radius = CalculateOptimalRadius(Nodes.Count);

        for (int i = 0; i < Nodes.Count; i++) {
            double angle = 2 * Math.PI * i / Nodes.Count;
            Nodes[i].X = _canvasWidth / 2 + radius * Math.Cos(angle);
            Nodes[i].Y = _canvasHeight / 2 + radius * Math.Sin(angle);
        }
    }

    // 根据节点数量计算最优半径
    private double CalculateOptimalRadius(int nodeCount) {
        if (nodeCount <= 1) {
            return 0;
        }

        // 基础半径加上根据节点数量的调整
        double adjustedRadius = _baseRadius;

        if (nodeCount > 8) {
            adjustedRadius += (nodeCount - 8) * 50;
        } else if (nodeCount < 4) {
            adjustedRadius += (4 - nodeCount) * 20;
        }
        // 确保半径不会太大或太小
        return Math.Clamp(adjustedRadius, 200, 200);
    }

    // 图操作方法
    public void AddNode(string label, double x = 0, double y = 0) {
        var node = new Node(label, x, y);
        Nodes.Add(node);
        OnPropertyChanged(nameof(GraphInfo));
    }

    public void AddEdge(Node start, Node end, int weight = 1, bool isBidirectional = false) {
        var edge = new Edge(start, end, weight);
        Edges.Add(edge);

        if (isBidirectional) {
            var reverseEdge = new Edge(end, start, weight);
            Edges.Add(reverseEdge);
        }

        OnPropertyChanged(nameof(GraphInfo));
    }

    public void Clear() {
        Nodes.Clear();
        Edges.Clear();
        OnPropertyChanged(nameof(GraphInfo));
    }

    public async Task RandomGenerate(GraphType type) {
        int nodeCount = Random.Shared.Next(5, 25);
        int edgeCount = Random.Shared.Next(nodeCount - 1, nodeCount * (nodeCount - 1) / 2);
        for (int i = 0; i < nodeCount; i++) {
            Nodes.Add(new Node(i.ToString()));
            await Task.Delay(100);
        }

        for (int i = 0; i < edgeCount; i++) {
            int startIndex = Random.Shared.Next(nodeCount);
            int endIndex = Random.Shared.Next(nodeCount);
            while (startIndex == endIndex ||
                   Edges.Any(edge => edge.StartNode == Nodes[startIndex] && edge.EndNode == Nodes[endIndex])
                   || (type == GraphType.Undirected && Edges.Any(edge =>
                       edge.StartNode == Nodes[endIndex] && edge.EndNode == Nodes[startIndex]))) {
                startIndex = Random.Shared.Next(nodeCount);
                endIndex = Random.Shared.Next(nodeCount);
            }

            AddEdge(Nodes[startIndex], Nodes[endIndex], Random.Shared.Next(1, 100), type == GraphType.Undirected);
            await Task.Delay(100);
        }
    }

    public async Task Kruskal() {
        var sortedEdges = Edges.OrderBy(edge => edge.Weight).ToList();
        Edges.Clear();
        PriorityQueue<Edge, int> pq = new PriorityQueue<Edge, int>();
        foreach (var sortedEdge in sortedEdges) {
            pq.Enqueue(sortedEdge, sortedEdge.Weight);
        }

        UnionFind<string> uf = new UnionFind<string>(Nodes.Select(node => node.Label));
        while (uf.Size > 1 && pq.Count > 0) {
            Edge edge = pq.Dequeue();
            if (!uf.IsConnected(edge.StartNode.Label, edge.EndNode.Label)) {
                Edges.Add(edge);
                Edges.Add(new Edge(edge.EndNode, edge.StartNode, edge.Weight));
                uf.Union(edge.StartNode.Label, edge.EndNode.Label);
                OnPropertyChanged(nameof(GraphInfo));
                await Task.Delay(500);
            }
        }
    }
}

// 并查集泛型类
public class UnionFind<T> where T : notnull {
    private readonly Dictionary<T, int> _index;
    private readonly List<int> _parent;
    private readonly List<int> _counts;
    private int _size;

    public int Size => _size;

    public UnionFind(IEnumerable<T> elements, IEqualityComparer<T>? comparer = null) {
        var comparer1 = comparer ?? EqualityComparer<T>.Default;
        var distinctElements = elements.Distinct(comparer1).ToList();
        _size = distinctElements.Count;

        _index = new Dictionary<T, int>(comparer1);
        _parent = new List<int>();
        _counts = new List<int>();

        // 索引从1开始，0位置不使用
        _parent.Add(-1);
        _counts.Add(-1);

        for (int i = 0; i < _size; i++) {
            _parent.Add(i + 1);
            _counts.Add(1);
            _index[distinctElements[i]] = i + 1;
        }
    }

    // int 类型的特殊构造函数
    public UnionFind(int n) : this(Enumerable.Range(1, n).Cast<T>()) {
        if (typeof(T) != typeof(int)) {
            throw new InvalidOperationException("此构造函数仅适用于 int 类型");
        }
    }

    private int FindInternal(int x) {
        if (_parent[x] == x)
            return x;

        _parent[x] = FindInternal(_parent[x]);
        return _parent[x];
    }

    public T Find(T element) {
        if (!_index.TryGetValue(element, out var value)) {
            throw new ArgumentException($"元素 {element} 不存在");
        }

        int rootIndex = FindInternal(value);
        // 返回原始元素类型的结果
        return _index.First(kv => kv.Value == rootIndex).Key;
    }

    public void Union(T x, T y) {
        if (!_index.ContainsKey(x) || !_index.ContainsKey(y)) {
            string missing = "";
            if (!_index.ContainsKey(x)) missing += x.ToString();
            if (!_index.ContainsKey(x) && !_index.ContainsKey(y)) missing += " 和 ";
            if (!_index.ContainsKey(y)) missing += y.ToString();
            throw new ArgumentException($"元素{missing}不存在");
        }

        int rootX = FindInternal(_index[x]);
        int rootY = FindInternal(_index[y]);

        if (rootX != rootY) {
            if (_counts[rootX] > _counts[rootY]) {
                (rootX, rootY) = (rootY, rootX);
            }

            _parent[rootX] = rootY;
            _counts[rootY] += _counts[rootX];
            _size--;
        }
    }

    public int GetCount(T element) {
        if (!_index.TryGetValue(element, out var value)) {
            throw new ArgumentException($"元素 {element} 不存在");
        }

        int root = FindInternal(value);
        return _counts[root];
    }

    public bool IsConnected(T x, T y) {
        if (!_index.ContainsKey(x) || !_index.TryGetValue(y, out var value)) {
            return false;
        }

        return FindInternal(_index[x]) == FindInternal(value);
    }
}