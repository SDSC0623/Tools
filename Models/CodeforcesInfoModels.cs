// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tools.Models;

public class CodeforcesApiResponse<T> {
    [JsonProperty("status")] public string Status { get; set; } = string.Empty;
    [JsonProperty("result")] public T? Result { get; set; }
    [JsonProperty("comment")] public string? Comment { get; set; }
}

public class CodeforcesApiContest {
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = string.Empty;

    [JsonProperty("type")] public string Type { get; set; } = string.Empty;

    [JsonProperty("phase")] public string Phase { get; set; } = string.Empty;

    [JsonProperty("frozen")] public bool Frozen { get; set; }

    [JsonProperty("durationSeconds")] public long DurationSeconds { get; set; }

    [JsonProperty("startTimeSeconds")] public long StartTimeSeconds { get; set; }

    [JsonProperty("relativeTimeSeconds")] public long RelativeTimeSeconds { get; set; }
}

// ReSharper disable once NotAccessedPositionalProperty.Global
public record TimerTickMessage(string TimerName);

// 业务逻辑模型（适用于WPF绑定）
public partial class ContestModel : ObservableRecipient, IRecipient<TimerTickMessage> {
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public bool Frozen { get; set; }
    public TimeSpan Duration { get; private set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [ObservableProperty] private TimeSpan _count;

    private ContestModel Init() {
        EndTime = StartTime + Duration;
        if (EndTime >= DateTime.Now) {
            IsActive = true;
        }

        return this;
    }

    public ContestModel FromApi(CodeforcesApiContest contest) {
        Id = contest.Id;
        Name = contest.Name;
        Type = contest.Type;
        Phase = contest.Phase;
        Frozen = contest.Frozen;
        Duration = TimeSpan.FromSeconds(contest.DurationSeconds);
        StartTime = DateTimeOffset.FromUnixTimeSeconds(contest.StartTimeSeconds).DateTime.ToLocalTime();
        return Init();
    }

    public void Receive(TimerTickMessage message) {
        if (StartTime > DateTime.Now) {
            Count = StartTime.Subtract(DateTime.Now);
        } else if (EndTime > DateTime.Now) {
            Count = EndTime.Subtract(DateTime.Now);
        } else {
            Count = TimeSpan.Zero;
        }
    }
}

public class CodeforcesApiUser {
    [JsonProperty("contribution")] public int Contribution { get; set; }

    [JsonProperty("lastOnlineTimeSeconds")]
    public long LastOnlineTimeSeconds { get; set; }

    [JsonProperty("rating")] public int Rating { get; set; }

    [JsonProperty("friendOfCount")] public int FriendOfCount { get; set; }

    [JsonProperty("titlePhoto")] public string TitlePhoto { get; set; } = string.Empty;

    [JsonProperty("rank")] public string Rank { get; set; } = string.Empty;

    [JsonProperty("handle")] public string Handle { get; set; } = string.Empty;

    [JsonProperty("maxRating")] public int MaxRating { get; set; }

    [JsonProperty("avatar")] public string Avatar { get; set; } = string.Empty;

    [JsonProperty("registrationTimeSeconds")]
    public long RegistrationTimeSeconds { get; set; }

    [JsonProperty("maxRank")] public string MaxRank { get; set; } = string.Empty;
}

public class UserModel {
    public DateTime LastOnlineTime { get; set; }
    public int Rating { get; set; }
    public string TitlePhoto { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public int MaxRating { get; set; }
    public DateTime RegistrationTime { get; set; }
    public string MaxRank { get; set; } = string.Empty;
    public SolidColorBrush CurColor { get; set; } = Brushes.Gray;
    public SolidColorBrush MaxColor { get; set; } = Brushes.Gray;

    private UserModel Init() {
        CurColor = GetColor(Rating);
        MaxColor = GetColor(MaxRating);

        return this;
    }

    public UserModel FromApi(CodeforcesApiUser user) {
        LastOnlineTime = DateTimeOffset.FromUnixTimeSeconds(user.LastOnlineTimeSeconds).DateTime.ToLocalTime();
        Rating = user.Rating;
        TitlePhoto = user.TitlePhoto;
        Rank = user.Rank;
        Handle = user.Handle;
        MaxRating = user.MaxRating;
        RegistrationTime = DateTimeOffset.FromUnixTimeSeconds(user.RegistrationTimeSeconds).DateTime.ToLocalTime();
        MaxRank = user.MaxRank;
        return Init();
    }

    private static SolidColorBrush GetColor(int rating) {
        return rating switch {
            >= 2400 => Brushes.Red,
            >= 2100 => Brushes.Orange,
            >= 1900 => new SolidColorBrush(Color.FromRgb(0x9C, 0x1F, 0xA4)),
            >= 1600 => Brushes.Blue,
            >= 1400 => new SolidColorBrush(Color.FromRgb(0x4B, 0xA5, 0x9E)),
            >= 1200 => Brushes.Green,
            _ => Brushes.Gray
        };
    }
}

public class FriendModel : UserModel {
    public bool IsOnline { get; set; }

    public FriendModel FromUser(UserModel user) {
        LastOnlineTime = user.LastOnlineTime;
        Rating = user.Rating;
        TitlePhoto = user.TitlePhoto;
        Rank = user.Rank;
        Handle = user.Handle;
        MaxRating = user.MaxRating;
        RegistrationTime = user.RegistrationTime;
        MaxRank = user.MaxRank;
        CurColor = user.CurColor;
        MaxColor = user.MaxColor;
        return this;
    }

    public FriendModel SetOnlineStatus(bool isOnline) {
        IsOnline = isOnline;
        return this;
    }
}

public enum TimeUnit {
    [Description("秒")] Second,
    [Description("分")] Minute,
    [Description("小时")] Hour,
    [Description("天")] Day,
    [Description("月")] Month,
    [Description("年")] Year
}

public class TimeRange {
    public double Value { get; set; }
    public TimeUnit Unit { get; set; }

    public TimeSpan ToTimeSpan() {
        switch (Unit) {
            case TimeUnit.Second:
                return TimeSpan.FromSeconds(Value);
            case TimeUnit.Minute:
                return TimeSpan.FromMinutes(Value);
            case TimeUnit.Hour:
                return TimeSpan.FromHours(Value);
            case TimeUnit.Day:
                return TimeSpan.FromDays(Value);
            case TimeUnit.Month:
                return TimeSpan.FromDays(Value * 30);
            case TimeUnit.Year:
                return TimeSpan.FromDays(Value * 365);
            default:
                return TimeSpan.FromDays(30);
        }
    }

    public override string ToString() {
        return $"{Value} {GetUnitDisplayName(Unit)}";
    }

    private static string GetUnitDisplayName(TimeUnit unit) {
        return unit switch {
            TimeUnit.Second => "秒",
            TimeUnit.Minute => "分",
            TimeUnit.Hour => "小时",
            TimeUnit.Day => "天",
            TimeUnit.Month => "月",
            TimeUnit.Year => "年",
            _ => "天"
        };
    }
}

public class CodeforcesApiRatingChange {
    [JsonProperty("contestId")] public int ContestId { get; set; }
    [JsonProperty("contestName")] public string ContestName { get; set; } = string.Empty;
    [JsonProperty("handle")] public string Handle { get; set; } = string.Empty;
    [JsonProperty("rank")] public int Rank { get; set; }

    [JsonProperty("ratingUpdateTimeSeconds")]
    public int RatingUpdateTimeSeconds { get; set; }

    [JsonProperty("oldRating")] public int OldRating { get; set; }
    [JsonProperty("newRating")] public int NewRating { get; set; }
}

public class RatingChangeModel {
    public int ContestId { get; set; }
    public string ContestName { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public int Rank { get; set; }
    public DateTime RatingUpdateTime { get; set; }
    public int OldRating { get; set; }
    public int NewRating { get; set; }
    public SolidColorBrush ChangeColor { get; set; } = Brushes.Gray;
    public string RatingChangeDisplay { get; set; } = string.Empty;
    public bool GainScore { get; set; }

    private RatingChangeModel Init() {
        ChangeColor = NewRating > OldRating ? Brushes.Green : Brushes.Red;
        RatingChangeDisplay = (NewRating > OldRating ? "+" : "") + (NewRating - OldRating);
        GainScore = NewRating > OldRating;
        return this;
    }

    public RatingChangeModel FromApi(CodeforcesApiRatingChange change) {
        ContestId = change.ContestId;
        ContestName = change.ContestName;
        Handle = change.Handle;
        Rank = change.Rank;
        RatingUpdateTime = DateTimeOffset.FromUnixTimeSeconds(change.RatingUpdateTimeSeconds).DateTime.ToLocalTime();
        OldRating = change.OldRating;
        NewRating = change.NewRating;
        return Init();
    }
}