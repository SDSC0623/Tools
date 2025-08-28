// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Tools.Models;

namespace Tools.Services.IServices;

public interface ICodeforcesApiService {
    Task<ObservableCollection<ContestModel>> GetContestsAsync(TimeSpan maxTimeToLoad);
    Task<UserModel> GetUserInfoAsync(string username);
    Task<List<UserModel>> GetUsersInfoAsync(List<string> username);
    Task<ObservableCollection<RatingChangeModel>> GetUserRatingChangesAsync(string username);

    Task<ObservableCollection<FriendModel>> GetFriendsAsync(string apiKey, string apiSecret,
        Action<string>? updateProgress = null);
}