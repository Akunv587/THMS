using System.Collections.Concurrent;
using EquipmentIMS.Models.DTOs.Reports;

namespace EquipmentIMS.BLL.Services;

/// <summary>导出任务状态内存跟踪器（单例）</summary>
public interface IExportTaskStatusTracker
{
    void Create(Guid taskId, string taskName);
    void SetProcessing(Guid taskId);
    void SetCompleted(Guid taskId, string fileName, string downloadUrl);
    void SetFailed(Guid taskId, string errorMessage);
    ExportTaskDto? Get(Guid taskId);
}

public class ExportTaskStatusTracker : IExportTaskStatusTracker
{
    private readonly ConcurrentDictionary<Guid, ExportTaskDto> _tasks = new();

    public void Create(Guid taskId, string taskName)
    {
        _tasks[taskId] = new ExportTaskDto
        {
            TaskId = taskId.ToString("N")[..8],
            TaskName = taskName,
            Status = ExportTaskStatus.Pending,
            CreatedAt = DateTime.Now
        };
    }

    public void SetProcessing(Guid taskId)
    {
        if (_tasks.TryGetValue(taskId, out var t))
            t.Status = ExportTaskStatus.Processing;
    }

    public void SetCompleted(Guid taskId, string fileName, string downloadUrl)
    {
        if (_tasks.TryGetValue(taskId, out var t))
        {
            t.Status = ExportTaskStatus.Completed;
            t.Progress = 100;
            t.FileName = fileName;
            t.DownloadUrl = downloadUrl;
        }
    }

    public void SetFailed(Guid taskId, string errorMessage)
    {
        if (_tasks.TryGetValue(taskId, out var t))
        {
            t.Status = ExportTaskStatus.Failed;
            t.ErrorMessage = errorMessage;
        }
    }

    public ExportTaskDto? Get(Guid taskId) =>
        _tasks.TryGetValue(taskId, out var t) ? t : null;
}
