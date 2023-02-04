#nullable enable
using TagBites.IO.Virtual;

namespace TagBites.IO;

internal static class Program
{
    private static void Main()
    {
        var directory1 = FileSystem.Local.GetDirectory("D:\\TestFiles");
        var directory2 = FileSystem.Local.GetDirectory("D:\\Vendo Pliki");

        var virtualDirectory = new VirtualDirectory
        {
            Entries =
            {
                new VirtualDirectoryEntry(directory1, "a", "aa1", "aa2"),
                new VirtualDirectoryEntry("b")
                {
                    Entries =
                    {
                        new VirtualDirectoryEntry(directory1, "a1"),
                        new VirtualDirectoryEntry(directory2, "b1")
                    }
                }
            }
        };

        var drive = new WinDrive(virtualDirectory.ToDirectory())
        {
            Name = "Test Drive",
            IsSingleThread = false,
            IsNetworkDrive = false
        };
        drive.AccessDenyProcesses
            .Add("dropbox");
        drive.AccessDenyFiles
            .AddWindowsFiles()
            .AddCodeFiles();

        drive.Mount();
        Console.ReadLine();
    }
}
