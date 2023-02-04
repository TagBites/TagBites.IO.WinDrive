using System.Runtime.InteropServices;
using DokanNet;

namespace TagBites.IO;

/// <summary>
/// Allows to safely mount Windows drive, based on directory in any file system.
/// </summary>
[PublicAPI]
public sealed class WinDrive : IDisposable
{
    private readonly DirectoryLink _directory;
    private Dokan? _dokan;
    private DokanInstance? _dokanInstance;

    /// <summary>
    /// Drive name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Mount point as a driver letter like "V:\" or a folder path "C:\MyDrive" on a NTFS partition.
    /// By default the letter "V" is used if available, if not then first free letter, starting from "Z".
    /// </summary>
    public string? MountPoint { get; set; }

    /// <summary>
    /// Indicates whether this drive will be read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Use a single thread to process events. This is highly not recommended as can easily create a bottleneck.
    /// </summary>
    public bool IsSingleThread { get; set; }
    /// <summary>
    /// Is mount as network drive.
    /// </summary>
    public bool IsNetworkDrive { get; set; }

    /// <summary>
    /// Provides set of files blocked by file system.
    /// </summary>
    public WinDriveAccessDenyFileCollection AccessDenyFiles { get; } = new();
    /// <summary>
    /// Provides set of processes blocked by file system.
    /// </summary>
    public WinDriveAccessDenyProcessCollection AccessDenyProcesses { get; } = new();

    /// <summary>
    /// Get or sets whether the drive is mounted.  
    /// </summary>
    public bool IsMounted
    {
        get => _dokanInstance != null;
        set
        {
            if (value)
                Mount();
            else
                Dismount();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WinDrive"/> class for a specified directory.
    /// </summary>
    /// <param name="directory">A directory used as drive content.</param>
    /// <exception cref="ArgumentNullException" />
    public WinDrive(DirectoryLink directory) => _directory = directory ?? throw new ArgumentNullException(nameof(directory));


    /// <summary>
    /// Mounts the drive.
    /// </summary>
    public void Mount()
    {
        if (IsMounted)
            return;

        if (string.IsNullOrEmpty(MountPoint))
            MountPoint = GetDefaultMountPoint();

        try
        {
            // HACK to additional error in ~Dokan (constructor failed, _dokan is null)
            DokanInit();
            // --

            _dokan = new Dokan(null);

            var operations = (IDokanOperations)new DokanOperations(_directory)
            {
                Name = Name,
                AccessDenyFiles = AccessDenyFiles.Count > 0 ? AccessDenyFiles.ToReadOnly() : null,
                AccessDenyProcesses = AccessDenyProcesses.Count > 0 ? AccessDenyProcesses.ToReadOnly() : null,
            };
            //operations = new Mirror("D:\\TestFiles");
            var dokanBuilder = new DokanInstanceBuilder(_dokan)
                .ConfigureOptions(options =>
                {
                    if (IsReadOnly)
                        options.Options |= DokanOptions.WriteProtection;
                    if (IsNetworkDrive)
                        options.Options |= DokanOptions.NetworkDrive;

                    options.Options &= ~DokanOptions.CaseSensitive;

                    options.SingleThread = IsSingleThread;
                    options.MountPoint = MountPoint;
                });

#if DEBUG
            operations = new TagBites.IO.Utils.DokanTrackOperations(operations);
#endif

            _dokanInstance = dokanBuilder.Build(operations);
        }
        catch
        {
            Dismount();
            throw;
        }
    }
    /// <summary>
    /// Dismount the drive.
    /// </summary>
    public void Dismount()
    {
        if (_dokanInstance != null)
            try { _dokanInstance?.Dispose(); }
            catch { /* ignored */ }
            finally { _dokanInstance = null; }

        if (_dokan != null)
            try { _dokan?.Dispose(); }
            catch { /* ignored */ }
            finally { _dokan = null; }
    }

    void IDisposable.Dispose() => Dismount();

    private static string GetDefaultMountPoint()
    {
        var letter = DriveInfo.GetDrives().Where(x => x.Name.Length == 3).Select(x => char.ToUpper(x.Name[0])).ToHashSet();
        if (!letter.Contains('V'))
            return "V:\\";

        for (var i = 'Z'; i > 'A'; i--)
            if (!letter.Contains(i))
                return $"{i}:\\";

        throw new IOException("No driver letter is available.");
    }

    [DllImport("dokan2.dll", ExactSpelling = true)]
    private static extern void DokanInit();
}