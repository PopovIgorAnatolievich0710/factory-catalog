namespace Tenengroup.Infrastructure.Common.Paging;

public class ListFilter
{
    public string[]? Values { get; set; }

    public string? Operator { get; set; }

    public string? Value { get; set; }

    public string? ValueTo { get; set; }
}
