using System.Collections.Concurrent;
using System.Diagnostics;

namespace TagBites.IO.Utils;

internal class ProcessNameCache
{
    private static ProcessNameCache? s_instance;

    public static ProcessNameCache Instance => s_instance ??= new ProcessNameCache();

    private readonly ConcurrentDictionary<int, string> _processes = new();

    private ProcessNameCache()
    { }


    public string? GetName(int id)
    {
        if (_processes.TryGetValue(id, out var name))
            return name;

        try
        {
            var process = Process.GetProcessById(id);
            var processName = process.ProcessName;

            if (_processes.TryAdd(id, processName))
                process.Exited += OnProcessOnExited;

            return processName;
        }
        catch
        {
            return null;
        }
    }
    private void OnProcessOnExited(object? sender, EventArgs eventArgs) => _processes.TryRemove(((Process)sender!).Id, out _);
}
