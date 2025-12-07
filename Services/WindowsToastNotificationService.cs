// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.Toolkit.Uwp.Notifications;
using Tools.Services.IServices;

namespace Tools.Services;

public class WindowsToastNotificationService : INotificationService {
    public void ShowNotification(string title, List<string> message, TimeSpan? duration = null) {
        var toastContentBuilder = new ToastContentBuilder()
            .AddText(title);
        foreach (var msg in message) {
            toastContentBuilder.AddText(msg);
        }

        duration ??= TimeSpan.FromMinutes(1);

        toastContentBuilder.Show(toast => { toast.ExpirationTime = DateTime.Now.Add(duration.Value); });
    }
}