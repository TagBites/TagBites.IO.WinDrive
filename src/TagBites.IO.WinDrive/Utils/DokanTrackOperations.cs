using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace TagBites.IO.Utils;

internal class DokanTrackOperations : IDokanOperations
{
    private readonly IDokanOperations _operations;

    private readonly HashSet<string> _skipTrack = new() { "GetDiskFreeSpace", "GetVolumeInformation" };
    private readonly ProcessNameCache _processNameCache = ProcessNameCache.Instance;

    public DokanTrackOperations(IDokanOperations operations) => _operations = operations;


    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        using var track = Track(info);
        return track.Result(_operations.Mounted(mountPoint, info));
    }
    public NtStatus Unmounted(IDokanFileInfo info)
    {
        using var track = Track(info);
        return track.Result(_operations.Unmounted(info));
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
    {
        using var track = Track(info);
        return track.Result(_operations.GetDiskFreeSpace(out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes, info));
    }
    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        using var track = Track(info);
        return track.Result(_operations.GetVolumeInformation(out volumeLabel, out features, out fileSystemName, out maximumComponentLength, info));
    }

    public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        var processName = _processNameCache.GetName(info.ProcessId);
        using var track = TrackArgs(info, $"{processName} - mode {mode} - share {share} - access {access} - {fileName}");
        return track.Result(_operations.CreateFile(fileName, access, share, mode, options, attributes, info));
    }
    public void Cleanup(string fileName, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        _operations.Cleanup(fileName, info);
        track.Result(NtStatus.Success);
    }
    public void CloseFile(string fileName, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        _operations.CloseFile(fileName, info);
        track.Result(NtStatus.Success);
    }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.GetFileInformation(fileName, out fileInfo, info));
    }
    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.FindFiles(fileName, out files, info));
    }
    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.FindFilesWithPattern(fileName, searchPattern, out files, info));
    }

    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.ReadFile(fileName, buffer, out bytesRead, offset, info));
    }
    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.WriteFile(fileName, buffer, out bytesWritten, offset, info));
    }
    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.FlushFileBuffers(fileName, info));
    }
    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.SetEndOfFile(fileName, length, info));
    }
    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.SetAllocationSize(fileName, length, info));
    }
    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.SetFileAttributes(fileName, attributes, info));
    }
    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.SetFileTime(fileName, creationTime, lastAccessTime, lastWriteTime, info));
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        using var track = TrackFile(info, $"{oldName} => {newName}");
        return track.Result(_operations.MoveFile(oldName, newName, replace, info));
    }
    public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.DeleteFile(fileName, info));
    }
    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.DeleteDirectory(fileName, info));
    }

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.LockFile(fileName, offset, length, info));
    }
    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.UnlockFile(fileName, offset, length, info));
    }
    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.GetFileSecurity(fileName, out security, sections, info));
    }
    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.SetFileSecurity(fileName, security, sections, info));
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        using var track = TrackFile(info, fileName);
        return track.Result(_operations.FindStreams(fileName, out streams, info));
    }

    private TrackStruct Track(IDokanFileInfo info, [CallerMemberName] string? method = null) => new(this, info, method!, null, false);
    private TrackStruct TrackFile(IDokanFileInfo info, string fileName, [CallerMemberName] string? method = null) => new(this, info, method!, fileName, true);
    private TrackStruct TrackArgs(IDokanFileInfo info, string args, [CallerMemberName] string? method = null) => new(this, info, method!, args, false);

    private struct TrackStruct : IDisposable
    {
        private readonly DokanTrackOperations _owner;
        private readonly IDokanFileInfo _info;
        private readonly FileContext? _context;
        private readonly string _method;
        private readonly string? _arguments;
        private readonly bool _isFile;
        private NtStatus _result;

        public TrackStruct(DokanTrackOperations owner, IDokanFileInfo info, string method, string? arguments, bool isFile)
        {
            _owner = owner;
            _info = info;
            _context = info.Context as FileContext;
            _method = method;
            _arguments = arguments;
            _isFile = isFile;
            _result = (NtStatus)(-1L);
        }


        public NtStatus Result(NtStatus result) => _result = result;

        public void Dispose()
        {
            if (_result == NtStatus.NotImplemented || _owner._skipTrack.Contains(_method))
                return;

            if (_arguments?.EndsWith("\\") == true
                || _method == "CreateFile" && _result == NtStatus.AccessDenied)
            {
                return;
            }

            var sb = new StringBuilder();

            if (_isFile)
                sb.Append("   ");

            var context = _context ?? _info.Context as FileContext;
            if (context != null)
                sb.Append($"[{context.Id}] ");
            else if (_method != "CreateFile")
                sb.Append("[NO CONTEXT] ");

            sb.Append(_method);

            if (!string.IsNullOrEmpty(_arguments))
            {
                sb.Append(' ');
                sb.Append('-');
                sb.Append(' ');
                sb.Append(_arguments);
            }

            if (_result != NtStatus.Success)
            {
                sb.Append(':');
                sb.Append(' ');

                if ((long)_result == -1L)
                    sb.Append(" EXCEPTION");
                else
                    sb.Append(_result);
            }

            Debug.WriteLine(sb.ToString());
        }
    }
}
