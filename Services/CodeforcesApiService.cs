// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Tools.Models;
using Tools.Services.IServices;

namespace Tools.Services;

public class CodeforcesApiService : ICodeforcesApiService {
    private readonly HttpClient _client = new() {
        BaseAddress = new Uri("https://codeforces.com/api/"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private async Task<CodeforcesApiResponse<T>> GetFromApiAsync<T>(string method) {
        try {
            HttpResponseMessage response = await _client.GetAsync(method);

            var jsonContent = await response.Content.ReadAsStringAsync();

            // 解析 JSON
            var apiResponse = JsonConvert.DeserializeObject<CodeforcesApiResponse<T>>(jsonContent);
            if (apiResponse == null) {
                throw new Exception("Json解析失败，返回内容为空");
            }

            if (!response.IsSuccessStatusCode && apiResponse is not { Status: "FAILED" }) {
                throw new HttpRequestException($"HTTP 错误: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return apiResponse;
        } catch (TaskCanceledException) {
            throw new Exception("API 请求超时，请检查网络连接");
        } catch (Exception ex) {
            throw new Exception($"API 请求失败: {ex.Message}");
        }
    }

    public async Task<ObservableCollection<ContestModel>> GetContestsAsync(TimeSpan maxTimeToLoad) {
        try {
            var apiResponse = await GetFromApiAsync<List<CodeforcesApiContest>>("contest.list");

            if (apiResponse == null) {
                throw new Exception("API请求失败，无内容返回");
            }

            if (apiResponse.Status != "OK") {
                throw new Exception($"API返回错误信息: {apiResponse.Comment}");
            }

            var temp = apiResponse.Result!
                .Where(c => c.RelativeTimeSeconds <= maxTimeToLoad.TotalSeconds)
                .ToList();

            temp = SortContests(temp);

            var result = new ObservableCollection<ContestModel>();
            temp.ForEach(c => result.Add(new ContestModel().FromApi(c)));

            return result;
        } catch (Exception ex) {
            throw new Exception($"获取比赛列表失败: {ex.Message}");
        }
    }

    private static List<CodeforcesApiContest> SortContests(List<CodeforcesApiContest> contests) {
        var beforeContests = new List<CodeforcesApiContest>();
        var otherContests = new List<CodeforcesApiContest>();

        foreach (var contest in contests) {
            if (contest.Phase != "FINISHED" && contest.Id != 1309 && contest.Id != 1308) {
                beforeContests.Add(contest);
            } else {
                otherContests.Add(contest);
            }
        }

        beforeContests.Sort((a, b) => a.StartTimeSeconds.CompareTo(b.StartTimeSeconds));

        otherContests.Sort((a, b) => b.StartTimeSeconds.CompareTo(a.StartTimeSeconds));

        var sortedList = new List<CodeforcesApiContest>();
        sortedList.AddRange(beforeContests);
        sortedList.AddRange(otherContests);

        return sortedList;
    }

    public async Task<UserModel> GetUserInfoAsync(string username) {
        try {
            var response = await GetUsersInfoAsync([username]);
            return response[0];
        } catch (Exception ex) {
            throw new Exception($"获取用户信息失败: {ex.Message}");
        }
    }

    public async Task<List<UserModel>> GetUsersInfoAsync(List<string> username) {
        try {
            StringBuilder names = new();
            foreach (var name in username) {
                names.Append(name).Append(';');
            }

            names.Remove(names.Length - 1, 1);
            var usernames = names.ToString();

            var apiResponse = await GetFromApiAsync<List<CodeforcesApiUser>>($"user.info?handles={usernames}");

            if (apiResponse == null) {
                throw new Exception("API请求失败，无内容返回");
            }

            if (apiResponse.Status != "OK") {
                throw new Exception($"API返回错误信息: {apiResponse.Comment}");
            }

            var result = new List<UserModel>();
            apiResponse.Result!.ForEach(c => result.Add(new UserModel().FromApi(c)));
            return result;
        } catch (Exception ex) {
            throw new Exception($"获取用户信息失败: {ex.Message}");
        }
    }

    public async Task<ObservableCollection<RatingChangeModel>> GetUserRatingChangesAsync(string username) {
        try {
            var apiResponse = await GetFromApiAsync<List<CodeforcesApiRatingChange>>($"user.rating?handle={username}");

            if (apiResponse == null) {
                throw new Exception("API请求失败，无内容返回");
            }

            if (apiResponse.Status != "OK") {
                throw new Exception($"API返回错误信息: {apiResponse.Comment}");
            }

            var result = new ObservableCollection<RatingChangeModel>();
            apiResponse.Result!.Reverse();
            apiResponse.Result.ForEach(c => result.Add(new RatingChangeModel().FromApi(c)));
            return result;
        } catch (Exception e) {
            throw new Exception($"获取用户Rating变化失败: {e.Message}");
        }
    }

    public async Task<ObservableCollection<FriendModel>> GetFriendsAsync(string apiKey,
        string apiSecret, Action<string>? updateProgress = null) {
        try {
            updateProgress?.Invoke("正在获取用户好友列表...");

            var friendsResult = await Task.WhenAll(
                GetFriendsNameAsync(apiKey, apiSecret, false),
                GetFriendsNameAsync(apiKey, apiSecret, true)
            );

            updateProgress?.Invoke("已获取好友ID，等待 1.5s 防止429错误(短时间内发送过多请求)");
            await Task.Delay(1500);


            var friendNames = friendsResult[0];

            updateProgress?.Invoke("正在获取所有好友信息...");
            var friendsInfo = await GetUsersInfoAsync(friendNames);
            updateProgress?.Invoke("已获取所有好友信息，开始解析...");

            var onlineSet = new HashSet<string>(friendsResult[1]);
            var friends = new ObservableCollection<FriendModel>();

            for (var i = 0; i < friendNames.Count; i++) {
                var friendName = friendNames[i];

                updateProgress?.Invoke($"正在解析好友{friendName}的信息，当前进度({i + 1}/{friendNames.Count})");

                var startTime = DateTime.Now;

                friends.Add(new FriendModel().FromUser(friendsInfo[i]).SetOnlineStatus(onlineSet.Contains(friendName)));

                var remainTime = (int)(DateTime.Now - startTime).TotalSeconds;
                if (remainTime < 200) {
                    await Task.Delay(200 - remainTime);
                }
            }

            updateProgress?.Invoke("已获取所有好友信息，开始排序...");
            var sortedFriends = SortFriends(friends);
            await Task.Delay(200);
            updateProgress?.Invoke("已排序所有好友信息，获取完成！");
            await Task.Delay(300);

            return sortedFriends;
        } catch (Exception e) {
            throw new Exception($"获取用户好友列表失败: {e.Message}");
        }
    }

    private static ObservableCollection<FriendModel> SortFriends(ObservableCollection<FriendModel> friends) {
        if (friends.Count == 0) {
            return [];
        }

        var sortedFriends = friends
            .OrderByDescending(f => f.IsOnline)
            .ThenByDescending(f => f.Rating)
            .ThenByDescending(f => f.MaxRating)
            .ThenBy(f => f.Handle)
            .ToList();

        return new ObservableCollection<FriendModel>(sortedFriends);
    }

    private async Task<List<string>> GetFriendsNameAsync(string apiKey, string apiSecret,
        bool isOnline) {
        try {
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var parameters = new Dictionary<string, string> {
                ["apiKey"] = apiKey,
                ["time"] = time.ToString()
            };

            if (isOnline) {
                parameters["onlyOnline"] = "true";
            }

            var apiSig = GenerateApiSig("user.friends", parameters, apiSecret);
            parameters["apiSig"] = apiSig;

            var paramString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            var apiResponse = await GetFromApiAsync<List<string>>($"user.friends?{paramString}");

            if (apiResponse == null) {
                throw new Exception("API请求失败，无内容返回");
            }

            if (apiResponse.Status != "OK") {
                throw new Exception($"API返回错误信息: {apiResponse.Comment}");
            }

            return apiResponse.Result!;
        } catch (Exception e) {
            throw new Exception($"获取用户好友列表失败: {e.Message}");
        }
    }

    // 生成API签名
    private string GenerateApiSig(string methodName, Dictionary<string, string> parameters, string apiSecret) {
        // 1. 生成随机数
        var random = new Random();
        var randomNum = random.Next(100000, 999999);

        // 2. 按参数名排序
        var sortedParams = parameters.OrderBy(p => p.Key).ToList();

        // 3. 构建参数字符串
        var paramString = string.Join("&", sortedParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        // 4. 构建API签名字符串
        var apiSigString = $"{randomNum}/{methodName}?{paramString}#{apiSecret}";

        // 5. 计算SHA512哈希
        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(apiSigString);
        var hash = sha512.ComputeHash(bytes);

        // 6. 转换为十六进制字符串
        var hex = string.Concat(hash.Select(b => b.ToString("x2")));

        return $"{randomNum}{hex}";
    }
}