using TagBites.IO.Utils;

namespace TagBites.IO;

/// <summary>
/// Provides set of processes blocked by file system.
/// </summary>
[PublicAPI]
public class WinDriveAccessDenyProcessCollection : HashSet<string>
{
    /// <summary>
    /// Gets or sets a value indicating whether check process name with case sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; set; }


    /// <summary>Adds the specified element to a set.</summary>
    /// <param name="item">The element to add to the set.</param>
    public new WinDriveAccessDenyProcessCollection Add(string item)
    {
        base.Add(item);
        return this;
    }

    internal bool AllowAccess(int processId)
    {
        if (ProcessNameCache.Instance.GetName(processId) is { } processName)
        {
            if (!IsCaseSensitive)
                processName = processName.ToLower();

            return !Contains(processName);
        }

        return true;
    }

    internal WinDriveAccessDenyProcessCollection ToReadOnly()
    {
        var items = new WinDriveAccessDenyProcessCollection { IsCaseSensitive = IsCaseSensitive };

        if (!IsCaseSensitive)
            foreach (var item in this)
                items.Add(item.ToLower());

        return items;
    }
}
