﻿using CasCap.Exceptions;
using CasCap.Messages;
using CasCap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
namespace CasCap.Services
{
    /// <summary>
    /// This class chains together the inherited GooglePhotosServiceBase REST methods into more useful combos/actions.
    /// </summary>
    //https://developers.google.com/photos/library/guides/get-started
    //https://developers.google.com/photos/library/guides/authentication-authorization
    public class GooglePhotosService : GooglePhotosServiceBase
    {
        public GooglePhotosService(ILogger<GooglePhotosService> logger,
            IOptions<GooglePhotosOptions> options,
            HttpClient client
            ) : base(logger, options, client)
        {

        }

        public async Task<Album?> GetOrCreateAlbumAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var album = await GetAlbumByTitleAsync(title, comparisonType);
            if (album is null) album = await CreateAlbumAsync(title);
            return album;
        }

        public async Task<Album?> GetAlbumByTitleAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var albums = await GetAlbumsAsync();
            return albums.FirstOrDefault(p => p.title.Equals(title, comparisonType));
        }

        public async Task<NewMediaItemResult?> UploadSingle(string path, string? albumId = null, string? description = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
        {
            var uploadToken = await UploadMediaAsync(path, uploadMethod);
            if (!string.IsNullOrWhiteSpace(uploadToken))
                return await AddMediaItemAsync(uploadToken!, path, description, albumId);
            return null;
        }

        public Task<mediaItemsCreateResponse?> UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, IProgress<Event>? callback = null)
            => _UploadMultiple(filePaths, albumId, uploadMethod, callback);

        public Task<mediaItemsCreateResponse?> UploadMultiple(string folderPath, string? searchPattern = null, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, IProgress<Event>? callback = null)
        {
            var filePaths = searchPattern is object ? Directory.GetFiles(folderPath, searchPattern) : Directory.GetFiles(folderPath);
            return _UploadMultiple(filePaths, albumId, uploadMethod, callback);
        }

        async Task<mediaItemsCreateResponse?> _UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, IProgress<Event>? callback = null)
        {
            var uploadItems = new List<UploadItem>(filePaths.Length);
            foreach (var filePath in filePaths)
            {
                var uploadToken = await UploadMediaAsync(filePath, uploadMethod, callback);

                if (!string.IsNullOrWhiteSpace(uploadToken))
                    uploadItems.Add(new UploadItem(uploadToken!, filePath));
            }
            return await AddMediaItemsAsync(uploadItems, albumId, GooglePhotosPositionType.LAST_IN_ALBUM, default, default, callback);
        }

        public Task<byte[]?> DownloadBytes(MediaItem mediaItem, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool download = false)
            => DownloadBytes(mediaItem.baseUrl, maxWidth, maxHeight, crop, downloadPhoto: mediaItem.isPhoto && download, downloadVideo: mediaItem.isVideo && download);

        //https://developers.google.com/photos/library/guides/access-media-items#image-base-urls
        //https://developers.google.com/photos/library/guides/access-media-items#video-base-urls
        async Task<byte[]?> DownloadBytes(string baseUrl, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool downloadPhoto = false, bool downloadVideo = false)
        {
            var qs = new List<string>();
            if (maxWidth.HasValue || maxHeight.HasValue)
            {
                if (maxWidth.HasValue) qs.Add($"w{maxWidth.Value}");
                if (maxHeight.HasValue) qs.Add($"h{maxHeight.Value}");
                if (crop) qs.Add("c");
            }
            if (downloadVideo) qs.Add("dv");
            if (downloadPhoto || qs.Count == 0) qs.Add("d");
            baseUrl += $"={string.Join("-", qs)}";
            var tpl = await Get<byte[], Error>(baseUrl);
            if (tpl.error is object)
                throw new GooglePhotosException(tpl.error);
            else
                return tpl.result;
        }
    }
}