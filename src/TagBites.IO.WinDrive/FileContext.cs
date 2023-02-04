namespace TagBites.IO;

internal class FileContext : IDisposable
{
    private static int s_nextId;
    private Stream? _stream;

    public int Id { get; } = Interlocked.Increment(ref s_nextId);
    public FileSystemStructureLink Link { get; }
    public Stream? Stream
    {
        get
        {
            if (_stream == null && StreamProvider != null)
                lock (this)
                    if (StreamProvider != null)
                    {
                        _stream = StreamProvider();
                        StreamProvider = null;
                    }

            return _stream;
        }
    }
    public Func<Stream>? StreamProvider { get; private set; }
    public FileSystemLinkMetadata? PendingMetadata { get; set; }

    public FileContext(FileSystemStructureLink link, Func<Stream>? streamProvider = null)
    {
        Link = link;
        StreamProvider = streamProvider;
    }


    public void Dispose()
    {
        if (_stream is { } stream)
        {
            _stream = null;

            stream.Dispose();
        }

        if (PendingMetadata is { } metadata)
        {
            PendingMetadata = null;

            Link.UpdateMetadata(metadata);
        }
    }
}


