using System.Threading.Channels;
using EquipmentIMS.Models.DTOs.Reports;

namespace EquipmentIMS.BLL.Services;

public interface IReportExportTaskQueue
{
    ValueTask EnqueueAsync(ReportExportTaskItem item);
    IAsyncEnumerable<ReportExportTaskItem> DequeueAsync(CancellationToken ct);
}

public class ReportExportTaskQueue : IReportExportTaskQueue
{
    private readonly Channel<ReportExportTaskItem> _channel = Channel.CreateUnbounded<ReportExportTaskItem>();

    public ValueTask EnqueueAsync(ReportExportTaskItem item) => _channel.Writer.WriteAsync(item);
    public IAsyncEnumerable<ReportExportTaskItem> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
