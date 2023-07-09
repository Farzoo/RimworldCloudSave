using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Verse.Steam;

namespace RimworldCloudSave;

public class SteamRemoteStorageService : ICloudStorageService 
{

    public SteamRemoteStorageService()
    {
        if (!SteamManager.Initialized) throw new InvalidOperationException("SteamManager not initialized");
    }

    public Task WriteFileAsync(string cloudFileName, Stream fileData, CancellationToken cancellationToken = default)
    {
        return this.WriteFileAsync(cloudFileName, fileData, (uint)fileData.Length, cancellationToken);
    }

    public Task WriteFileAsync(string cloudFileName, Stream fileData, uint byteSize, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        Task.Factory.StartNew(() =>
        {
            var fileDataBuffer = new byte[byteSize];
            byteSize = (uint) fileData.Read(fileDataBuffer, 0, (int)byteSize);
            if (byteSize == 0)
            {
                this.CreateFileAsync(cloudFileName, cancellationToken);
                tcs.SetResult(true);
                return;
            }
            var apiCall = SteamRemoteStorage.FileWriteAsync(cloudFileName, fileDataBuffer, byteSize);
            var callback = CallResult<RemoteStorageFileWriteAsyncComplete_t>.Create((param, failure) =>
            {
                if (failure)
                {
                    tcs.SetException(new InvalidOperationException($"SteamRemoteStorage.FileWriteAsync failed for file {cloudFileName}"));
                }
                else 
                {
                    tcs.SetResult(true);
                }
            });
            if(apiCall == SteamAPICall_t.Invalid) Log.Error($"byteSize: {byteSize} cloudFileName: {cloudFileName} ");
            callback.Set(apiCall);
        }, cancellationToken).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Log.Error($"SteamRemoteStorage.FileWriteAsync failed for file {cloudFileName} in ContinueWith");
            }
        }, cancellationToken);
        
        Log.Message($"SteamRemoteStorage.FileWriteAsync for file {cloudFileName} started");

        return tcs.Task;
    }

    public Task DeleteFileAsync(string cloudFileName, CancellationToken cancellationToken = default)
    {

        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        
        Task.Factory.StartNew(() =>
        {
            var success = SteamRemoteStorage.FileDelete(cloudFileName);
            tcs.SetResult(success);
        }, cancellationToken);
        
        return tcs.Task;
    }

    public Task CreateFileAsync(string cloudFileName, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        
        Task.Factory.StartNew(() =>
        {
            var success = SteamRemoteStorage.FileWrite(cloudFileName, new []{Convert.ToByte('\0')}, 1);
            tcs.SetResult(success);
        }, cancellationToken);
        
        return tcs.Task;
    }

    // Specify thrown exceptions as XML comments
    /// <exception cref="InvalidOperationException">Thrown when SteamManager is not initialized</exception>
    /// <exception cref="InvalidOperationException">Thrown when SteamRemoteStorage.FileReadAsync failed for cloud file {cloudFileName}</exception>
    public Task<Stream> ReadFileAsync(string cloudFileName, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<Stream>();

        Task.Factory.StartNew(() =>
        {
            var byteSize = SteamRemoteStorage.GetFileSize(cloudFileName);
            var fileData = new byte[byteSize];
            SteamAPICall_t apiCall = SteamRemoteStorage.FileReadAsync(cloudFileName, 0, (uint) byteSize);
            var callback = CallResult<RemoteStorageFileReadAsyncComplete_t>.Create((param, failure) =>
            {
                failure = SteamRemoteStorage.FileReadAsyncComplete(param.m_hFileReadAsync, fileData, (uint) byteSize);
                if (failure)
                {
                    tcs.SetException(new InvalidOperationException($"[RimworldCloudSave] SteamRemoteStorage.FileReadAsync failed for cloud file {cloudFileName}"));
                }
                else
                {
                    tcs.SetResult(new MemoryStream(fileData));
                }
            });
            callback.Set(apiCall);
        }, cancellationToken);

        return tcs.Task;
    }

    public Task RenameFileAsync(string oldCloudFileName, string newCloudFileName, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        
        Task.Run(() =>
        {
            var task = this.ReadFileAsync(oldCloudFileName, cancellationToken);
            var fileData = task.Result;
            if (fileData is null) throw new InvalidOperationException($"[RimworldCloudSave] SteamRemoteStorageService.ReadFileAsync returned null for file {oldCloudFileName}");
            SteamRemoteStorage.FileDelete(oldCloudFileName);
            var success = this.WriteFileAsync(newCloudFileName, fileData, cancellationToken);
            tcs.SetResult(success.IsCompletedSuccessfully);
        }, cancellationToken);
        
        return tcs.Task;
    }

    public Task<List<string>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        return Task<List<string>>.Factory.StartNew(() =>
        {
            int fileCount = SteamRemoteStorage.GetFileCount();
            var files = new List<string>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                files.Add(SteamRemoteStorage.GetFileNameAndSize(i, out _));
            }
            return files;
        }, cancellationToken);
    }

    public Task<bool> FileExistsAsync(string cloudFileName, CancellationToken cancellationToken = default)
    {
        return Task<bool>.Factory.StartNew(() => SteamRemoteStorage.FileExists(cloudFileName), cancellationToken);
    }

    public Task<FileMetadata> GetFileMetadataAsync(string cloudFileName, CancellationToken cancellationToken = default)
    {
        return Task<FileMetadata>.Factory.StartNew(() =>
        {
            var fileMetadata = new FileMetadata(
                SteamRemoteStorage.GetFileSize(cloudFileName),
            DateTimeOffset.FromUnixTimeSeconds(SteamRemoteStorage.GetFileTimestamp(cloudFileName)).DateTime.ToLocalTime()
            );
            return fileMetadata;
        }, cancellationToken);
    }

    public Task<bool> IsCloudEnabledAsync(CancellationToken cancellationToken = default)
    {
        return Task<bool>.Factory.StartNew(() =>
        {
            return SteamRemoteStorage.IsCloudEnabledForAccount() && SteamRemoteStorage.IsCloudEnabledForApp();
        }, cancellationToken);
    }

    public Task<ulong> GetMaxCloudSpaceAsync(CancellationToken cancellationToken = default)
    {
        return Task<ulong>.Factory.StartNew(() =>
        {
            SteamRemoteStorage.GetQuota(out var totalBytes, out var availableBytes);
            return totalBytes;
        }, cancellationToken);
    }
    
    public Task<ulong> GetAvailableCloudSpaceAsync(CancellationToken cancellationToken = default)
    {
        return Task<ulong>.Factory.StartNew(() =>
        {
            SteamRemoteStorage.GetQuota(out var totalBytes, out var availableBytes);
            return availableBytes;
        }, cancellationToken);
    }
}