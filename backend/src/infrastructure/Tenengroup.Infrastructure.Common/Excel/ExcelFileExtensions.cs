using Microsoft.AspNetCore.Mvc;

namespace Tenengroup.Infrastructure.Common.Excel;

public static class ExcelFileExtensions
{
    public static FileResult ToFileResult(this ExcelFile file)
    {
        return new FileContentResult(file.Content, ExcelFile.ContentType)
        {
            FileDownloadName = file.FileName
        };
    }
}
