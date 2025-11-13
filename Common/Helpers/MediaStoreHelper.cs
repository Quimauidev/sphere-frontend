#if ANDROID
using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using Sphere.Common.Constans;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System.Diagnostics;

public class MediaStoreHelper
{
    public readonly IPermissionService _permissionService;
    public MediaStoreHelper(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<List<Android.Net.Uri>> GetImageUrisAsync(int limit = 50, int offset = 0)
    {
        bool granted = await _permissionService!.EnsureGrantedAsync(AppPermission.ReadImages);

        if (!granted)
        {
            return [];
        }

        var result = new List<Android.Net.Uri>();

        await Task.Run(() =>
        {
            var uri = MediaStore.Images.Media.ExternalContentUri;
            var projection = new[] { MediaStore.Images.Media.InterfaceConsts.Id};
            var sortOrder = $"{MediaStore.Images.Media.InterfaceConsts.DateAdded} DESC";

            using ICursor? cursor = Android.App.Application.Context.ContentResolver?.Query(uri!, projection, null, null, sortOrder);

            if (cursor == null || !cursor.MoveToFirst()) return;

            int idColumn = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Id);

            int count = 0;

            do
            {
                if (count >= offset && result.Count < limit)
                {

                    long id = cursor.GetLong(idColumn);
                    var contentUri = ContentUris.WithAppendedId(uri!, id);
                    result.Add(contentUri);

                }

                count++;
            } while (cursor.MoveToNext());

        });

        return result;
    }
}
#endif