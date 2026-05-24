using System.Threading.Channels;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class GenreBackfillJobChannel
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        });

    private int _running;

    public bool TryEnqueue()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            return false;

        if (!_channel.Writer.TryWrite(true))
        {
            Interlocked.Exchange(ref _running, 0);
            return false;
        }

        return true;
    }

    public IAsyncEnumerable<bool> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    internal void MarkComplete() => Interlocked.Exchange(ref _running, 0);
}
