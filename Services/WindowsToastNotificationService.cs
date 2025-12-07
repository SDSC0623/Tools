// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Mail;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using Tools.Services.IServices;

namespace Tools.Services;

public class WindowsToastNotificationService : INotificationService {
    // 日志
    private readonly ILogger _logger;

    public WindowsToastNotificationService(ILogger logger) {
        _logger = logger;
    }

    public void ShowWindowsToastNotification(string title, List<string> message, TimeSpan? duration = null) {
        if (message.Count > 3) {
            throw new Exception("Windows简单通知最多支持3行文本");
        }

        var toastContentBuilder = new ToastContentBuilder()
            .AddText(title);
        foreach (var msg in message) {
            toastContentBuilder.AddText(msg);
        }

        duration ??= TimeSpan.FromMinutes(1);

        toastContentBuilder.Show(toast => { toast.ExpirationTime = DateTime.Now.Add(duration.Value); });
    }

    public void PostEmail(string subject, string body, string toAddress, string fromAddress, string fromPassword) {
        try {
            var smtp = new SmtpClient {
                Host = "smtp.qq.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress, fromPassword)
            };

            using var message = new MailMessage(fromAddress, toAddress);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            smtp.Send(message);
        } catch (Exception e) {
            _logger.Error("发送邮件失败: {Message}", e.Message);
        }
    }
}