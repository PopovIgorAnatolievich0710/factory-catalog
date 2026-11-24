namespace Tenengroup.Infrastructure.Common.Excel;

public record ExcelColumn<T>(string Key, string Header, Func<T, object?> Value);
