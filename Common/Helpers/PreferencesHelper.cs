using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.Common.Helpers
{
    internal class PreferencesHelper
    {
        private const string AuthTokenKey = "AuthToken";
        private const string UserKey = "CurrentUser";
        private const string RefreshTokenKey = "RefreshToken";

        private const string RefreshTokenIdKey = "RefreshTokenId";
        private const string IntroShownKey = "IntroShown";
        private const string ChatUnlockedKeyPrefix = "ChatUnlocked_";
        // 🔹 Vị trí tìm quanh đây
        private const string LocationEnabledKey = "LocationEnabled";
        private const string DiamondPackagesKey = "DiamondPackages";
        // 🔹 Đã có location record trên server hay chưa
        private const string HasLocationRecordKey = "HasLocationRecord";


        // Lưu trạng thái đã mở khóa
        public static void SetChatUnlocked(Guid conversationId, bool unlocked)
        {
            Preferences.Set(ChatUnlockedKeyPrefix + conversationId, unlocked);
        }

        // Lấy trạng thái đã mở khóa
        public static bool IsChatUnlocked(Guid conversationId)
        {
            return Preferences.Get(ChatUnlockedKeyPrefix + conversationId, false);
        }

        // Xóa trạng thái (nếu cần)
        public static void ClearChatUnlocked(Guid conversationId)
        {
            Preferences.Remove(ChatUnlockedKeyPrefix + conversationId);
        }
        public static bool GetLocationEnabled()
        {
            return Preferences.Get(LocationEnabledKey, false);
        }
        public static void SetLocationEnabled(bool isEnabled)
        {
            Preferences.Set(LocationEnabledKey, isEnabled);
        }

        public static void ClearLocationEnabled()
        {
            Preferences.Remove(LocationEnabledKey);
        }
        public static bool GetHasLocationRecord()
        {
            return Preferences.Get(HasLocationRecordKey, false);
        }

        public static void SetHasLocationRecord(bool value)
        {
            Preferences.Set(HasLocationRecordKey, value);
        }

        public static void ClearHasLocationRecord()
        {
            Preferences.Remove(HasLocationRecordKey);
        }
        public static bool HasSeenIntro()
        {
            return Preferences.Get(IntroShownKey, false);
        }

        public static void SetIntroShown()
        {
            Preferences.Set(IntroShownKey, true);
        }

        public static void SetRefreshTokenId(string id) => Preferences.Set(RefreshTokenIdKey, id);
        public static string? GetRefreshTokenId() => Preferences.Get(RefreshTokenIdKey, null);
        public static void ClearRefreshTokenId() => Preferences.Remove(RefreshTokenIdKey);


        private const string AuthTokenExpiresAtKey = "AuthTokenExpiresAt";

        public static void SetAuthTokenExpiresAt(DateTime expiresAt) =>
            Preferences.Set(AuthTokenExpiresAtKey, expiresAt.ToString("o"));

        public static DateTime? GetAuthTokenExpiresAt()
        {
            if (!Preferences.ContainsKey(AuthTokenExpiresAtKey))
                return null;

            var raw = Preferences.Get(AuthTokenExpiresAtKey, "");
            if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;

            return null; // Không parse được cũng trả null
        }
        public static List<DiamondModel>? LoadDiamondPackages()
        {
            var json = Preferences.Get(DiamondPackagesKey, null);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<List<DiamondModel>>(json);
        }
        public static void SaveDiamondPackages(IEnumerable<DiamondModel> packages)
        {
            var json = JsonSerializer.Serialize(packages);
            Preferences.Set(DiamondPackagesKey, json);
        }


        public static void SetRefreshToken(string token) => Preferences.Set(RefreshTokenKey, token);
        public static void SetAuthToken(string token) => Preferences.Set(AuthTokenKey, token);

        public static string? GetRefreshToken() => Preferences.Get(RefreshTokenKey, null);
        public static string? GetAuthToken() => Preferences.Get(AuthTokenKey, null);

        public static bool IsUserLoggedIn() => !string.IsNullOrEmpty(GetAuthToken());

        public static void ClearRefreshToken() => Preferences.Remove(RefreshTokenKey);
        public static void ClearAuthToken() => Preferences.Remove(AuthTokenKey);
        public static void ClearDiamondPackages() =>  Preferences.Remove(DiamondPackagesKey);
        


        // 👤 User Profile
        public static void SaveCurrentUser(UserWithUserProfileModel user)
        {
            var json = JsonSerializer.Serialize(user);
            Preferences.Set(UserKey, json);
        }

        public static UserWithUserProfileModel? LoadCurrentUser()
        {
            var json = Preferences.Get(UserKey, null);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<UserWithUserProfileModel>(json);
        }

        public static void ClearCurrentUser() => Preferences.Remove(UserKey);

        // Đăng xuất toàn bộ
        public static void Logout()
        {
            ClearAuthToken();
            ClearRefreshToken();
            ClearCurrentUser();
            ClearRefreshTokenId();
            ClearLocationEnabled();
            ClearDiamondPackages();
            ClearHasLocationRecord();
        }
    }
}