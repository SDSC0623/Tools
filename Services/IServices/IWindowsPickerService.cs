// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Tools.Models;

namespace Tools.Services.IServices;

public interface IWindowsPickerService {
    /// <summary>
    /// 获取所有可见的应用程序窗口
    /// </summary>
    /// <returns>窗口信息集合</returns>
    ObservableCollection<WindowInfo> GetAllWindows();
}