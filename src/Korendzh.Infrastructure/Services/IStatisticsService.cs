namespace Korendzh.Infrastructure.Services;

public class StatBucket
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Hours { get; set; }
}

public interface IStatisticsService
{
    Task<List<StatBucket>> HoursByWorkerAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<List<StatBucket>> HoursByTaskAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<List<StatBucket>> HoursByCarAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
