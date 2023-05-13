namespace EveRAT.Models;

public partial class History
{
    public long HistoryId { get; set; }

    public long HistorySystemId { get; set; }

    public DateTime HistoryDateTime { get; set; }

    public long? HistoryNpckills { get; set; }

    public double? HistoryAdm { get; set; }
}