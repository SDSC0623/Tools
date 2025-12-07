// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Tools.Services.IServices;

public interface INotificationService {
    void ShowNotification(string title, List<string> message, TimeSpan? duration = null);
}