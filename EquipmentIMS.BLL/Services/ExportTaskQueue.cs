using System.Threading.Channels;
using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Services;

public class ExportTaskItem
{
    public Guid TaskId { get; set; }
    public StockTransactionQueryDto Query { get; set; } = null!;
    public long UserId { get; set; }
}

public interface IExportTaskQueue
{
    ValueTask EnqueueAsync(ExportTaskItem item);
    IAsyncEnumerable<ExportTaskItem> DequeueAsync(CancellationToken ct);
}

public class ExportTaskQueue : IExportTaskQueue
{
    private readonly Channel<ExportTaskItem> _channel = Channel.CreateUnbounded<ExportTaskItem>();

    public ValueTask EnqueueAsync(ExportTaskItem item) => _channel.Writer.WriteAsync(item);

    public IAsyncEnumerable<ExportTaskItem> DequeueAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
