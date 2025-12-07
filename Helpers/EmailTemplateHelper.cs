// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using Tools.Models;

namespace Tools.Helpers;

public class EmailTemplateHelper {
    public static string GenerateAppNotificationHtml(List<WindowInfo> monitoredApps, TimeSpan dayOffset) {
        var now = DateTime.Now;
        var separatorTime = now.Date + dayOffset;

        int startedCount = monitoredApps.Count(app => app.HasStartToday);
        int notStartedCount = monitoredApps.Count - startedCount;
        string statusEmoji = notStartedCount == 0 ? "✅" : "⚠️";
        string statusText = notStartedCount == 0 ? "所有应用均已启动" : $"{notStartedCount} 个应用未启动";
        string daySeparatorTime = separatorTime.ToString("HH:mm");

        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {
            font-family: 'Segoe UI', 'Microsoft YaHei', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 20px;
            background-color: #f5f7fa;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
            overflow: hidden;
        }
        .email-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 40px;
            text-align: center;
        }
        .email-header h1 {
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }
        .status-badge {
            display: inline-block;
            background: rgba(255, 255, 255, 0.2);
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 14px;
            margin-top: 12px;
            backdrop-filter: blur(10px);
        }
        .content-section {
            padding: 30px 40px;
        }
        .summary-card {
            background: #f8f9ff;
            border-left: 4px solid #667eea;
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 30px;
        }
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            margin-bottom: 30px;
        }
        .stat-item {
            text-align: center;
            padding: 20px;
            background: #f8f9ff;
            border-radius: 10px;
            border: 1px solid #eaefff;
        }
        .stat-number {
            font-size: 32px;
            font-weight: 700;
            margin: 10px 0;
        }
        .stat-label {
            font-size: 14px;
            color: #666;
        }
        .green { color: #10b981; }
        .yellow { color: #f59e0b; }
        .red { color: #ef4444; }
        .apps-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }
        .apps-table th {
            background: #f8f9ff;
            padding: 16px;
            text-align: left;
            font-weight: 600;
            color: #4b5563;
            border-bottom: 2px solid #e5e7eb;
        }
        .apps-table td {
            padding: 16px;
            border-bottom: 1px solid #e5e7eb;
        }
        .app-row:hover {
            background: #f9fafb;
        }
        .app-name {
            font-weight: 500;
            color: #1f2937;
        }
        .app-process {
            font-size: 12px;
            color: #6b7280;
            margin-top: 4px;
        }
        .status-indicator {
            display: inline-block;
            width: 10px;
            height: 10px;
            border-radius: 50%;
            margin-right: 8px;
        }
        .status-started { background-color: #10b981; }
        .status-not-started { background-color: #ef4444; }
        .time-cell {
            font-family: 'Consolas', monospace;
            color: #6b7280;
        }
        .footer {
            margin-top: 40px;
            padding: 20px;
            text-align: center;
            color: #9ca3af;
            font-size: 12px;
            border-top: 1px solid #e5e7eb;
        }
        .time-info {
            background: #fef3c7;
            border: 1px solid #fde68a;
            padding: 12px 20px;
            border-radius: 8px;
            margin: 20px 0;
            color: #92400e;
        }
        @media (max-width: 600px) {
            .stats-grid { grid-template-columns: 1fr; }
            .content-section { padding: 20px; }
        }
    </style>
</head>
<body>");

        htmlBuilder.AppendLine($@"<div class='email-container'>
    <div class='email-header'>
        <h1>📱 应用启动监控报告</h1>
        <div class='status-badge'>
            {statusEmoji} {statusText}
        </div>
    </div>
    
    <div class='content-section'>
        <div class='time-info'>
            ⏰ 今日统计截止时间：每天 {daySeparatorTime} 前启动的应用计入今日统计
        </div>
        
        <div class='stats-grid'>
            <div class='stat-item'>
                <div class='stat-label'>监控应用总数</div>
                <div class='stat-number'>{monitoredApps.Count}</div>
            </div>
            <div class='stat-item'>
                <div class='stat-label'>已启动应用</div>
                <div class='stat-number green'>{startedCount}</div>
            </div>
            <div class='stat-item'>
                <div class='stat-label'>未启动应用</div>
                <div class='stat-number {(notStartedCount > 0 ? "red" : "green")}'>{notStartedCount}</div>
            </div>
        </div>");

        if (notStartedCount > 0) {
            htmlBuilder.AppendLine(@"<div class='summary-card'>
            <h3 style='margin-top: 0; color: #dc2626;'>⚠️ 以下应用尚未启动：</h3>
            <table class='apps-table'>
                <thead>
                    <tr>
                        <th style='width: 5%;'>状态</th>
                        <th style='width: 45%;'>应用名称</th>
                        <th style='width: 30%;'>进程名称</th>
                        <th style='width: 20%;'>上次启动</th>
                    </tr>
                </thead>
                <tbody>");

            foreach (var app in monitoredApps.Where(a => !a.HasStartToday).OrderBy(a => a.ProcessName)) {
                string lastStartTimeStr = app.LastStartTime == DateTime.MinValue
                    ? "从未启动"
                    : app.LastStartTime.ToString("yyyy-MM-dd HH:mm");

                htmlBuilder.AppendLine($@"<tr class='app-row'>
                    <td><span class='status-indicator status-not-started'></span></td>
                    <td>
                        <div class='app-name'>{EscapeHtml(app.DisplayTitle)}</div>
                    </td>
                    <td>
                        <div class='app-process'>{EscapeHtml(app.ProcessName)}</div>
                    </td>
                    <td class='time-cell'>{lastStartTimeStr}</td>
                </tr>");
            }

            htmlBuilder.AppendLine("</tbody></table></div>");
        }

        if (startedCount > 0) {
            htmlBuilder.AppendLine(@"<div class='summary-card'>
            <h3 style='margin-top: 0; color: #059669;'>✅ 已启动应用列表：</h3>
            <table class='apps-table'>
                <thead>
                    <tr>
                        <th style='width: 5%;'>状态</th>
                        <th style='width: 45%;'>应用名称</th>
                        <th style='width: 30%;'>进程名称</th>
                        <th style='width: 20%;'>启动时间</th>
                    </tr>
                </thead>
                <tbody>");

            foreach (var app in monitoredApps.Where(a => a.HasStartToday).OrderBy(a => a.ProcessName)) {
                htmlBuilder.AppendLine($@"<tr class='app-row'>
                    <td><span class='status-indicator status-started'></span></td>
                    <td>
                        <div class='app-name'>{EscapeHtml(app.DisplayTitle)}</div>
                    </td>
                    <td>
                        <div class='app-process'>{EscapeHtml(app.ProcessName)}</div>
                    </td>
                    <td class='time-cell'>{app.LastStartTime:HH:mm:ss}</td>
                </tr>");
            }

            htmlBuilder.AppendLine("</tbody></table></div>");
        }

        htmlBuilder.AppendLine($@"<div class='footer'>
            <p>📅 报告时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>📊 统计数据来源：Tools 应用启动监控</p>
            <p style='margin-top: 8px; color: #9ca3af; font-size: 11px;'>
                此邮件为自动发送，如需修改通知设置，请在应用设置中调整
            </p>
        </div>
    </div>
</div>
</body>
</html>");

        return htmlBuilder.ToString();
    }

    private static string EscapeHtml(string input) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}