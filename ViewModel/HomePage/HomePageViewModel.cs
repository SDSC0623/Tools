// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Tools.Helpers;
using Tools.Models;

namespace Tools.ViewModel.HomePage;

public partial class HomePageViewModel : ObservableObject {
    public static string HomePageTitle => $"主页 v{GlobalSettings.Version}";

    public static string HomePageDescription => "欢迎使用\"各种工具\"，本软件是一个集成了多种实用工具的应用程序。";

    public static string HomePageUseHelp => "您可以通过左侧导航菜单访问各个工具。每个工具都有直观的用户界面，使您能够轻松使用其功能。";

    [ObservableProperty] private ObservableCollection<ToolItem> _tools = [];

    public HomePageViewModel() {
        Tools = [
            ToolItem.Create(
                name: "Codeforces信息查看",
                description: "此工具可以帮助您查看Codeforces平台上的比赛信息、用户排名和题目详情。\n您可以快速获取比赛时间、题目难度、用户评分等信息，方便编程竞赛的准备和练习。",
                features: [
                    "查看即将到来和已结束的比赛",
                    "查询你的用户信息和Rating变化(需设定用户名)",
                    "好友的基本信息查询(需设置你的ApiKey和ApiSecret)"
                ]
            ),
            ToolItem.Create(
                name: "HideInBMP - BMP文件隐写工具",
                description: "此工具允许您将文件隐藏到BMP图片中，实现数据的隐蔽传输。\n通过利用BMP文件的特性，您可以在不显著改变图片外观的情况下，将重要文件隐藏在普通图片中。",
                features: [
                    "将任何类型的文件隐藏到BMP图片中",
                    "从BMP图片中提取隐藏的文件",
                    "保持图片视觉质量，难以察觉变化"
                ]
            )
        ];
    }
}