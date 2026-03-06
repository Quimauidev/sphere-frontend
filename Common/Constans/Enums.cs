using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Constans
{
    public enum Gender
    {
        Male = 0, // nam
        Female = 1 // nữ
    }

    public enum AppPermission
    {
        ReadImages, // READ_EXTERNAL_STORAGE
        Camera, // CAMERA
        Location, //ACCESS_FINE_LOCATION, BỎ ACCESS_COARSE_LOCATION, chạy ngầm ACCESS_BACKGROUND_LOCATION
        Microphone //RECORD_AUDIO
    }

    public enum Privacy
    {
        Public = 0,
        Friends = 1,
        Private = 2
    }

    public enum UiViewState
    {
        Loading,
        Success,
        Empty,
        Error,
    }
    public enum PermissionResult
    {
        Granted, // cho phép
        Denied, // từ chối vẫn cho phép hỏi lại
        DeniedDontAskAgain // từ chối và không hỏi lại
    }

    public enum MessageStatus
    {
        Sending = 0, // đang gửi
        Sent = 1,    // đã gửi
        Delivered = 2, // đã đến thiết bị người nhận
        Seen = 3,     // đã xem
        Failed = 4    // gửi thất bại
    }
}