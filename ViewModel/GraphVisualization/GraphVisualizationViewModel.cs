// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Models;
using Tools.Services;

namespace Tools.ViewModel.GraphVisualization;

public partial class GraphVisualizationViewModel : ObservableObject {
    // 日志
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public static readonly int ArrowOffset = 20;
    public static readonly int TextOffset = 40;

    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private GraphData _graphData = new();

    [ObservableProperty] private string _newNodeLabel = string.Empty;

    [ObservableProperty] private GraphData.Node? _startNode;

    [ObservableProperty] private GraphData.Node? _endNode;

    [ObservableProperty] private int _edgeWeight = 1;

    [ObservableProperty] private bool _isBidirectional;

    [ObservableProperty] private double _canvasWidth;

    [ObservableProperty] private double _canvasHeight;

    [ObservableProperty] private GraphType _randomGenerateType = GraphType.Directed;

    public GraphVisualizationViewModel(ILogger logger, SnackbarServiceHelper snackbarService) {
        _logger = logger;
        _snackbarService = snackbarService;
        GraphData.SetCanvasSize(_canvasWidth, _canvasHeight);
        // GraphData.PropertyChanged += (_, _) => {
        //     string temp = "";
        //     foreach (var graphDataEdge in GraphData.Edges) {
        //         temp +=
        //             $"从{graphDataEdge.StartNode.Label} 到 {graphDataEdge.EndNode.Label} 权重为 {graphDataEdge.Weight}\n";
        //     }
        //
        //     _logger.Information(temp);
        // };
    }

    partial void OnCanvasHeightChanged(double value) {
        StatusMessage = $"已设置画布高度为: {value}";
        GraphData.SetCanvasSize(_canvasWidth, value);
    }

    partial void OnCanvasWidthChanged(double value) {
        StatusMessage = $"已设置画布宽度为: {value}";
        GraphData.SetCanvasSize(value, _canvasHeight);
    }

    [RelayCommand]
    private void AddNode() {
        if (string.IsNullOrWhiteSpace(NewNodeLabel)) {
            StatusMessage = "节点标签不能为空";
            _snackbarService.ShowWarning("添加节点失败", "尝试添加空标签节点");
            return;
        }

        // 检查节点标签是否已存在
        if (GraphData.Nodes.Any(n => n.Label == NewNodeLabel)) {
            StatusMessage = $"节点标签 '{NewNodeLabel}' 已存在";
            _snackbarService.ShowWarning("添加节点失败", $"尝试添加已存在的节点标签: {NewNodeLabel}");
            return;
        }

        try {
            GraphData.AddNode(NewNodeLabel);
            StatusMessage = $"已添加节点: {NewNodeLabel}";
            NewNodeLabel = string.Empty;
        } catch (Exception ex) {
            StatusMessage = "添加节点时发生错误";
            _logger.Error(ex, "添加节点时发生错误");
        }
    }

    [RelayCommand]
    private void AddEdge() {
        if (StartNode == null || EndNode == null) {
            StatusMessage = "请选择起点和终点节点";
            _snackbarService.ShowWarning("添加边失败", "请选择起点和终点节点");
            return;
        }

        if (StartNode == EndNode) {
            _snackbarService.ShowWarning("添加边失败", "起点和终点不能相同");
            StatusMessage = "起点和终点不能相同";
            return;
        }

        // 检查同向边是否已存在
        if (GraphData.Edges.Any(e => e.StartNode == StartNode && e.EndNode == EndNode)) {
            StatusMessage = $"边 {StartNode.Label} → {EndNode.Label} 已存在";
            _snackbarService.ShowWarning("添加边失败", $"边已存在: {StartNode.Label} → {EndNode.Label}");
            return;
        }

        // 如果是双向边，检查反向边是否已存在
        if (IsBidirectional && GraphData.Edges.Any(e => e.StartNode == EndNode && e.EndNode == StartNode)) {
            StatusMessage = $"反向边 {EndNode.Label} → {StartNode.Label} 已存在，无法添加双向边";
            _snackbarService.ShowWarning("添加边失败", $"反向边已存在: {EndNode.Label} → {StartNode.Label}");
            return;
        }

        try {
            GraphData.AddEdge(StartNode, EndNode, EdgeWeight, IsBidirectional);
            StatusMessage =
                $"已添加{(IsBidirectional ? "双向" : "")}边: {StartNode.Label} {(IsBidirectional ? "↔" : "→")} {EndNode.Label} (权重: {EdgeWeight})";
        } catch (Exception ex) {
            StatusMessage = "添加边时发生错误";
            _logger.Error(ex, "添加边时发生错误");
        }
    }

    [RelayCommand]
    private void ClearGraph() {
        try {
            GraphData.Clear();
            StartNode = null;
            EndNode = null;
            StatusMessage = "已清空图";
        } catch (Exception ex) {
            StatusMessage = "清空图时发生错误";
            _logger.Error(ex, "清空图时发生错误");
        }
    }

    [RelayCommand]
    private void RandomGenerate() {
        GraphData.RandomGenerate(RandomGenerateType);
    }

    [RelayCommand]
    private async Task KruskalGenerate() {
        await GraphData.Kruskal();
        StatusMessage = "已生成最小生成树";
    }
}