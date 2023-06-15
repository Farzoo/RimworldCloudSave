using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RimworldCloudSave;

public interface ICloudStorageService
{
    public Task WriteFileAsync(string cloudFileName, Stream fileData, CancellationToken cancellationToken = default);
    
    public Task WriteFileAsync(string cloudFileName, Stream fileData, uint byteSize, CancellationToken cancellationToken = default);
    
    public Task DeleteFileAsync(string cloudFileName, CancellationToken cancellationToken = default);
    
    public Task CreateFileAsync(string cloudFileName, CancellationToken cancellationToken = default);
    
    public Task<Stream> ReadFileAsync(string cloudFileName, CancellationToken cancellationToken = default);
    
    public Task RenameFileAsync(string oldCloudFileName, string newCloudFileName, CancellationToken cancellationToken = default);
    
    public Task<List<string>> ListFilesAsync(CancellationToken cancellationToken = default);
    
    public Task<bool> FileExistsAsync(string cloudFileName, CancellationToken cancellationToken = default);
    
    public Task<FileMetadata> GetFileMetadataAsync(string cloudFileName, CancellationToken cancellationToken = default);
    
    public Task<bool> IsCloudEnabledAsync(CancellationToken cancellationToken = default);
    
    public Task<ulong> GetMaxCloudSpaceAsync(CancellationToken cancellationToken = default);
    public Task<ulong> GetAvailableCloudSpaceAsync(CancellationToken cancellationToken = default);
}

public struct FileMetadata
{
    public int FileSize { get; set; }
    public DateTime LastModified { get; set; }
}