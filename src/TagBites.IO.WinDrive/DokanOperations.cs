using System.Diagnostics;
using System.Security.AccessControl;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace TagBites.IO;

internal class DokanOperations : IDokanOperations
{
    private const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                          FileAccess.Execute |
                                          FileAccess.GenericExecute | FileAccess.GenericWrite |
                                          FileAccess.GenericRead;
    private const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                               FileAccess.Delete |
                                               FileAccess.GenericWrite;
    private readonly DirectoryLink _root;

    public string? Name { get; init; }
    public WinDriveAccessDenyFileCollection? AccessDenyFiles { get; init; }
    public WinDriveAccessDenyProcessCollection? AccessDenyProcesses { get; init; }

    public DokanOperations(DirectoryLink root) => _root = root;


    public NtStatus Mounted(string mountPoint, IDokanFileInfo info) => DokanResult.Success;
    public NtStatus Unmounted(IDokanFileInfo info) => DokanResult.Success;

    public NtStatus CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        if (AccessDenyFiles != null && Path.GetFileName(filename) is { } fn && !AccessDenyFiles.AllowAccess(fn))
            return DokanResult.AccessDenied;

        if (AccessDenyProcesses != null && info.ProcessId > 0 && !AccessDenyProcesses.AllowAccess(info.ProcessId))
            return DokanResult.AccessDenied;

        if (info.IsDirectory)
        {
            var directory = _root.GetDirectory(filename);

            if (directory.ExistsAsDifferentResource)
                return DokanResult.NotADirectory;

            switch (mode)
            {
                case FileMode.Create:
                case FileMode.CreateNew:
                    {
                        if (directory.Exists)
                            return DokanResult.FileExists;

                        directory.Create();

                        info.Context = new FileContext(directory);
                        return DokanResult.Success;
                    }

                case FileMode.OpenOrCreate:
                    {
                        if (!directory.Exists)
                            directory.Create();

                        info.Context = new FileContext(directory);
                        return DokanResult.Success;
                    }

                case FileMode.Open:
                    {
                        if (!directory.Exists)
                            return DokanResult.PathNotFound;

                        info.Context = new FileContext(directory);
                        return DokanResult.Success;
                    }

                default:
                    return DokanResult.AccessDenied;
            }
        }

        var file = _root.GetFile(filename);
        var pathExists = file.Exists;
        var pathIsDirectory = file.ExistsAsDifferentResource;

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (mode)
        {
            case FileMode.Open when !pathExists:
                return DokanResult.FileNotFound;

            // check if driver only wants to read attributes, security info, or open directory
            case FileMode.Open when (access & DataAccess) == 0 || pathIsDirectory:
                {
                    // It is a DeleteFile request on a directory
                    if (pathIsDirectory && (access & FileAccess.Delete) == FileAccess.Delete
                                        && (access & FileAccess.Synchronize) != FileAccess.Synchronize)
                    {
                        return DokanResult.AccessDenied;
                    }

                    // must set it to something if you return DokanError.Success
                    info.IsDirectory = pathIsDirectory;

                    info.Context = new FileContext(pathIsDirectory ? _root.GetDirectory(filename) : file);
                    return DokanResult.Success;
                }

            case FileMode.CreateNew when pathExists:
                return DokanResult.FileExists;

            case FileMode.Truncate when !pathExists:
                return DokanResult.FileNotFound;
        }

        if (pathIsDirectory)
            return DokanResult.Error;

        if (pathExists && mode is FileMode.OpenOrCreate or FileMode.Create)
        {
            info.Context = new FileContext(file);
            return DokanResult.AlreadyExists;
        }

        var isRead = (access & (FileAccess.ReadData | FileAccess.Execute | FileAccess.GenericExecute | FileAccess.GenericRead)) != 0;
        var isWrite = (access & (FileAccess.WriteData | FileAccess.AppendData | FileAccess.GenericWrite)) != 0;

        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (isRead && isWrite)
            info.Context = new FileContext(file, () => file.OpenReadWrite());
        else if (isWrite)
            info.Context = new FileContext(file, () => file.OpenWrite());
        else
            info.Context = new FileContext(file);

        return NtStatus.Success;
    }
    public void Cleanup(string filename, IDokanFileInfo info)
    {
        var context = info.Context as FileContext;
        CleanupContext(info);

        if (info.DeleteOnClose && context?.Link is { } link)
            link.Delete();
    }
    public void CloseFile(string filename, IDokanFileInfo info)
    {
        CleanupContext(info);
        info.Context = null;
    }
    private static void CleanupContext(IDokanFileInfo info)
    {
        if (info.Context is FileContext context)
            context.Dispose();
    }

    public NtStatus GetFileInformation(string filename, out FileInformation fileInfo, IDokanFileInfo info)
    {
        if (info.Context is FileContext context)
        {
            fileInfo = CreateFileInformation(context.Link, context);
            return DokanResult.Success;
        }

        var link = _root.GetExistingLink(filename);
        if (link == null)
        {
            fileInfo = new FileInformation();
            return DokanResult.PathNotFound;
        }

        fileInfo = CreateFileInformation(link);
        return DokanResult.Success;
    }
    public NtStatus FindFiles(string filename, out IList<FileInformation> files, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Link: DirectoryLink directory })
        {
            files = directory.GetLinks()
                .Select(x => CreateFileInformation(x))
                .ToList();
            return DokanResult.Success;
        }

        files = Array.Empty<FileInformation>();
        return DokanResult.Error;
    }
    public NtStatus FindFilesWithPattern(string filename, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
    {
        if (searchPattern == "*")
            return FindFiles(filename, out files, info);

        if (info.Context is FileContext { Link: DirectoryLink directory })
        {
            if (!searchPattern.Contains('*') && directory.GetExistingLink(searchPattern) is { } link)
            {
                files = new[] { CreateFileInformation(link) };
                return DokanResult.Success;
            }

            files = directory.GetLinks()
                .Where(x => DokanHelper.DokanIsNameInExpression(searchPattern, x.Name, true))
                .Select(x => CreateFileInformation(x))
                .ToList();
            return DokanResult.Success;
        }

        files = Array.Empty<FileInformation>();
        return DokanResult.NotImplemented;
    }
    private static FileInformation CreateFileInformation(FileSystemStructureLink link, FileContext? context = null)
    {
        var fileInfo = new FileInformation
        {
            FileName = link.Name,
            CreationTime = link.CreationTime,
            LastWriteTime = context?.PendingMetadata?.LastWriteTime ?? link.ModifyTime,
            LastAccessTime = DateTime.Now
        };

        if (context?.PendingMetadata?.IsReadOnly == true || link.IsReadOnly)
            fileInfo.Attributes |= FileAttributes.ReadOnly;

        if (context?.PendingMetadata?.IsHidden == true || link.IsHidden)
            fileInfo.Attributes |= FileAttributes.Hidden;

        switch (link)
        {
            case DirectoryLink:
                fileInfo.Attributes |= FileAttributes.Directory;
                break;

            case FileLink file:
                fileInfo.Length = file.Length;
                break;
        }

        return fileInfo;
    }

    public NtStatus ReadFile(string filename, byte[] buffer, out int readBytes, long offset, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Stream: { } stream })
        {
            lock (stream)
            {
                stream.Position = offset;
                readBytes = stream.Read(buffer, 0, buffer.Length);
            }
        }
        else
        {
            var file = _root.GetFile(filename);

            using (stream = file.OpenRead())
            {
                stream.Position = offset;
                readBytes = stream.Read(buffer, 0, buffer.Length);

                if (stream.Length != file.Length && readBytes == 0 && stream.Position == stream.Length)
                {
                    Debug.WriteLine($"!!!!!!!!! Length mismatch s: {stream.Length}, f: {file.Length}.");
                    return DokanResult.Unsuccessful;
                }
            }
        }

        return DokanResult.Success;
    }
    public NtStatus WriteFile(string filename, byte[] buffer, out int writtenBytes, long offset, IDokanFileInfo info)
    {
        lock (info)
        {
            if (info.Context is FileContext { Stream: { } stream })
            {
                lock (stream)
                {
                    stream.Position = offset == -1
                        ? stream.Length
                        : offset;

                    var bytesToCopy = GetNumOfBytesToCopy(buffer.Length, offset, info, stream);
                    stream.Write(buffer, 0, bytesToCopy);
                    writtenBytes = bytesToCopy;
                }
            }
            else
            {
                var file = _root.GetFile(filename);

                if (!file.Exists || file.ExistsAsDifferentResource)
                {
                    writtenBytes = 0;
                    return DokanResult.FileNotFound;
                }

                using (stream = file.OpenWrite())
                {
                    stream.Position = offset == -1
                        ? stream.Length
                        : offset;

                    var bytesToCopy = GetNumOfBytesToCopy(buffer.Length, offset, info, stream);
                    stream.Write(buffer, 0, bytesToCopy);
                    writtenBytes = bytesToCopy;
                }
            }
        }

        return DokanResult.Success;
    }
    public NtStatus SetAllocationSize(string filename, long length, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Stream: { } stream })
            stream.SetLength(length);

        return DokanResult.Success;
    }
    public NtStatus SetEndOfFile(string filename, long length, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Stream: { } stream })
            stream.SetLength(length);

        return DokanResult.Success;
    }
    public NtStatus FlushFileBuffers(string filename, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Stream: { } stream })
            stream.Flush();

        return DokanResult.Success;
    }
    public NtStatus SetFileAttributes(string filename, FileAttributes attr, IDokanFileInfo info)
    {
        if (info.Context is FileContext context)
            lock (context)
            {
                context.PendingMetadata ??= new FileSystemLinkMetadata();
                context.PendingMetadata.IsReadOnly = (attr & FileAttributes.ReadOnly) != 0;
                context.PendingMetadata.IsHidden = (attr & FileAttributes.Hidden) != 0;

                return DokanResult.Success;
            }

        return DokanResult.Error;
    }
    public NtStatus SetFileTime(string filename, DateTime? ctime, DateTime? atime, DateTime? mtime, IDokanFileInfo info)
    {
        if (info.Context is FileContext context)
            lock (context)
            {
                context.PendingMetadata ??= new FileSystemLinkMetadata();

                if (mtime.HasValue)
                    context.PendingMetadata.LastWriteTime = mtime;

                return DokanResult.Success;
            }

        return DokanResult.Error;
    }

    public NtStatus MoveFile(string filename, string newname, bool replace, IDokanFileInfo info)
    {
        if (info.IsDirectory)
        {
            var source = _root.GetDirectory(filename);
            var destination = _root.GetDirectory(newname);

            if (replace)
                return DokanResult.AccessDenied;

            if (destination.Exists)
                return DokanResult.FileExists;

            if (!source.Exists || source.ExistsAsDifferentResource)
                return DokanResult.FileNotFound;

            source.Move(destination);
        }
        else
        {
            var source = _root.GetFile(filename);
            var destination = _root.GetFile(newname);

            if (!source.Exists || source.ExistsAsDifferentResource)
                return DokanResult.FileNotFound;

            if (destination.Exists && !replace)
                return DokanResult.FileExists;

            source.Move(destination, replace);
        }

        return DokanResult.Success;
    }
    public NtStatus DeleteFile(string filename, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Link: FileLink file })
        {
            if (file.ExistsAsDifferentResource)
                return DokanResult.AccessDenied;

            return DokanResult.Success;
        }

        return DokanResult.Error;
    }
    public NtStatus DeleteDirectory(string filename, IDokanFileInfo info)
    {
        if (info.Context is FileContext { Link: DirectoryLink directory })
        {
            if (directory.ExistsAsDifferentResource)
                return DokanResult.AccessDenied;

            return directory.GetLinks().Any()
                ? DokanResult.DirectoryNotEmpty
                : DokanResult.Success;
        }

        return DokanResult.Error;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalBytes, out long totalFreeBytes, IDokanFileInfo info)
    {
        freeBytesAvailable = 0;
        totalBytes = 0;
        totalFreeBytes = 0;
        return DokanResult.NotImplemented;
    }
    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = (Name ?? _root.Name).Replace("/", "") is var n && !string.IsNullOrEmpty(n) ? n : "Virtual Disk";
        fileSystemName = "NTFS";
        maximumComponentLength = 256;

        features = FileSystemFeatures.None;

        return DokanResult.Success;
    }

    private static int GetNumOfBytesToCopy(int bufferLength, long offset, IDokanFileInfo info, Stream stream)
    {
        if (info.PagingIo)
        {
            var longDistanceToEnd = stream.Length - offset;
            var isDistanceToEndMoreThanInt = longDistanceToEnd > int.MaxValue;
            if (isDistanceToEndMoreThanInt)
                return bufferLength;

            var distanceToEnd = (int)longDistanceToEnd;
            return distanceToEnd < bufferLength ? distanceToEnd : bufferLength;
        }

        return bufferLength;
    }

    NtStatus IDokanOperations.FindStreams(string filename, out IList<FileInformation> streams, IDokanFileInfo info) { streams = Array.Empty<FileInformation>(); return DokanResult.NotImplemented; }
    NtStatus IDokanOperations.GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info) { security = null; return DokanResult.NotImplemented; }
    NtStatus IDokanOperations.SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info) => DokanResult.NotImplemented;
    NtStatus IDokanOperations.LockFile(string filename, long offset, long length, IDokanFileInfo info) => DokanResult.NotImplemented;
    NtStatus IDokanOperations.UnlockFile(string filename, long offset, long length, IDokanFileInfo info) => DokanResult.NotImplemented;
}
