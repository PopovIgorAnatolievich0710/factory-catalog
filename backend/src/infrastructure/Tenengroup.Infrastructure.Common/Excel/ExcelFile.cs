namespace Tenengroup.Infrastructure.Common.Excel;

public record ExcelFile(byte[] Content, string FileName)
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
