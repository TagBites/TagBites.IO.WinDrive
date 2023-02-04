namespace TagBites.IO;

/// <summary>
/// Provides set of files blocked by file system.
/// </summary>
[PublicAPI]
public class WinDriveAccessDenyFileCollection : HashSet<string>
{
    /// <summary>
    /// Gets or sets a value indicating whether check file name with case sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; set; }


    /// <summary>Adds the specified element to a set.</summary>
    /// <param name="item">The element to add to the set.</param>
    public new WinDriveAccessDenyFileCollection Add(string item)
    {
        base.Add(item);
        return this;
    }

    /// <summary>
    /// Add files specific for Windows: AutoRun.inf, Thumbs.db 
    /// and files specific for explorer.exe: folder.gif, folder.jpg, desktop.ini, Desktop.ini, AutoRun.inf, Thumbs.db.
    /// </summary>
    public WinDriveAccessDenyFileCollection AddWindowsFiles()
    {
        Add("AutoRun.inf");
        Add("Thumbs.db");

        Add("folder.gif");
        Add("folder.jpg");
        Add("desktop.ini");
        Add("Desktop.ini");
        return this;
    }

    /// <summary>
    /// Add files for applications listed below.
    /// Git: .git, .gitignore, locale.alias, messages.mo, HEAD, 
    /// Code: browser_init, browser_init.js, browser_init.json, browser_init.node.
    /// </summary>
    public WinDriveAccessDenyFileCollection AddCodeFiles()
    {
        // Git
        Add(".git");
        Add(".gitignore");
        Add("locale.alias");
        Add("messages.mo");
        Add("HEAD");

        // Code
        Add("browser_init");
        Add("browser_init.js");
        Add("browser_init.json");
        Add("browser_init.node");

        return this;
    }

    internal bool AllowAccess(string name)
    {
        if (!IsCaseSensitive)
            name = name.ToLower();

        return !Contains(name);
    }

    internal WinDriveAccessDenyFileCollection ToReadOnly()
    {
        var items = new WinDriveAccessDenyFileCollection { IsCaseSensitive = IsCaseSensitive };

        if (!IsCaseSensitive)
            foreach (var item in this)
                items.Add(item.ToLower());

        return items;
    }
}
