namespace Tenengroup.Infrastructure.Common.Paging;

public class ExportRequest : ListRequest
{
    public string[] Columns { get; set; } = [];
}
