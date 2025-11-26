// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Models;
using Tools.Services;

namespace Tools.ViewModel.DataStructureDisplay.TreeVisualization;

public partial class TreeVisualizationViewModel : ObservableObject {
    private readonly ILogger _logger;
    private readonly SnackbarServiceHelper _snackbarService;

    [ObservableProperty] private string _statusMessage = "就绪";
    [ObservableProperty] private TreeData _treeData = new();
    [ObservableProperty] private string _newNodeValue = string.Empty;
    [ObservableProperty] private TreeData.TreeNode? _parentNode;
    [ObservableProperty] private bool _insertLeft = true;
    [ObservableProperty] private bool _insertRight;

    [ObservableProperty] private string _operateNodeValue = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsBinaryTree), nameof(IsNotBinarySearchTree))]
    private TreeType _currentTreeType = TreeType.BinarySearchTree;

    [ObservableProperty] private TraversalType _selectedTraversalType = TraversalType.PreOrder;
    [ObservableProperty] private string _traversalResult = string.Empty;
    [ObservableProperty] private int _randomNodeCount = 10;

    [ObservableProperty] private bool _hasNodes;

    public bool IsBinaryTree => CurrentTreeType == TreeType.BinaryTree;

    public bool IsNotBinarySearchTree => CurrentTreeType != TreeType.BinarySearchTree;

    public TreeVisualizationViewModel(ILogger logger, SnackbarServiceHelper snackbarService) {
        _logger = logger;
        _snackbarService = snackbarService;

        TreeData.PropertyChanged += (_, _) => { HasNodes = TreeData.Nodes.Count > 0; };
    }

    public void UpdateSize(double width, double height) {
        TreeData.SetCanvasSize(width, height);
    }

    partial void OnCurrentTreeTypeChanged(TreeType value) {
        ClearTreeCommand.Execute(null);
        StatusMessage = $"已切换到{value}模式";
    }

    [RelayCommand]
    private void AddNode() {
        if (string.IsNullOrWhiteSpace(NewNodeValue)) {
            StatusMessage = "节点值不能为空";
            _snackbarService.ShowWarning("添加节点失败", "节点值不能为空");
            return;
        }

        // 检查节点值是否已存在（对于二叉搜索树需要检查，普通树可以重复）
        if (CurrentTreeType == TreeType.BinarySearchTree &&
            TreeData.Nodes.Any(n => n.Value == NewNodeValue)) {
            StatusMessage = $"节点值 '{NewNodeValue}' 已存在";
            _snackbarService.ShowWarning("添加节点失败", $"节点值已存在: {NewNodeValue}");
            return;
        }

        try {
            if (CurrentTreeType == TreeType.BinaryTree) {
                TreeData.AddBinaryNode(NewNodeValue, ParentNode, InsertLeft);
            } else if (CurrentTreeType == TreeType.GeneralTree) {
                TreeData.AddGeneralNode(NewNodeValue, ParentNode);
            } else if (CurrentTreeType == TreeType.BinarySearchTree) {
                TreeData.AddBinarySearchNode(NewNodeValue, TreeData.Root);
            }

            StatusMessage = $"已添加节点: {NewNodeValue}";
            NewNodeValue = string.Empty;
            InsertLeft = true;
            InsertRight = false;
        } catch (Exception ex) {
            StatusMessage = "添加节点时发生错误";
            _logger.Error(ex, "添加节点时发生错误");
            _snackbarService.ShowError("添加节点失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task QueryNode() {
        if (string.IsNullOrWhiteSpace(OperateNodeValue)) {
            StatusMessage = "节点值不能为空";
            _snackbarService.ShowWarning("查询节点失败", "节点值不能为空");
            return;
        }

        if (CurrentTreeType != TreeType.BinarySearchTree) {
            StatusMessage = "未知错误导致树类型错误";
            _snackbarService.ShowError("查询节点失败", "未知错误导致树类型错误");
            return;
        }

        try {
            await TreeData.SearchNode(OperateNodeValue, TreeData.Root);
            StatusMessage = $"已查询节点: {OperateNodeValue}";
        } catch (Exception ex) {
            StatusMessage = "查询节点时发生错误";
            _logger.Error(ex, "查询节点时发生错误");
            _snackbarService.ShowError("查询节点失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteNode() {
        if (string.IsNullOrWhiteSpace(OperateNodeValue)) {
            StatusMessage = "节点值不能为空";
            _snackbarService.ShowWarning("查询节点失败", "节点值不能为空");
            return;
        }

        if (CurrentTreeType != TreeType.BinarySearchTree) {
            StatusMessage = "未知错误导致树类型错误";
            _snackbarService.ShowError("查询节点失败", "未知错误导致树类型错误");
            return;
        }

        try {
            await TreeData.DeleteNode(OperateNodeValue, TreeData.Root);
            StatusMessage = $"已删除节点: {OperateNodeValue}";
        } catch (Exception ex) {
            StatusMessage = "删除节点时发生错误";
            _logger.Error(ex, "删除节点时发生错误");
            _snackbarService.ShowError("删除节点失败", ex.Message);
        }
    }

    [RelayCommand]
    private void ClearTree() {
        try {
            TreeData.Clear();
            ParentNode = null;
            TraversalResult = string.Empty;
            StatusMessage = "已清空树";
        } catch (Exception ex) {
            StatusMessage = "清空树时发生错误";
            _logger.Error(ex, "清空树时发生错误");
        }
    }

    [RelayCommand]
    private async Task RandomGenerate() {
        try {
            await TreeData.RandomGenerate(RandomNodeCount, CurrentTreeType);
            StatusMessage = $"已随机生成包含{RandomNodeCount}个节点的树";
        } catch (Exception ex) {
            StatusMessage = "随机生成树时发生错误";
            _logger.Error(ex, "随机生成树时发生错误");
        }
    }

    [RelayCommand]
    private void Traverse() {
        try {
            var result = TreeData.Traverse(SelectedTraversalType);
            TraversalResult = string.Join(" → ", result);
            StatusMessage = $"已执行{SelectedTraversalType}遍历";
        } catch (Exception ex) {
            StatusMessage = "遍历树时发生错误";
            _logger.Error(ex, "遍历树时发生错误");
            _snackbarService.ShowError("遍历失败", ex.Message);
        }
    }
}