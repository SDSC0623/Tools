// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Media;
using Tools.Models;

namespace Tools.Services.IServices;

public interface IWindowsPickerService {
    /// <summary>
    /// 获取所有可见的应用程序窗口
    /// </summary>
    /// <returns>窗口信息集合</returns>
    Task<ObservableCollection<WindowInfo>> GetAllWindowsAsync(bool needVisible = true);

    /// <summary>
    /// 开始实时捕获窗口画面
    /// </summary>
    /// <param name="windowInfo">窗口信息</param>
    /// <param name="onFrameCaptured">帧捕获回调</param>
    /// <param name="fps">帧率 (1-30)</param>
    void StartShowWindow(WindowInfo windowInfo, Action<ImageSource> onFrameCaptured, double fps = 24);

    /// <summary>
    /// 停止实时捕获
    /// </summary>
    void StopShowWindow();
}