using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Components;

[ApiController]
[Route("v1/component-stock-levels")]
[ApiExplorerSettings(GroupName = "Components")]
[AzureAuthorize(Permission.ComponentsView)]
public class ComponentStockLevelsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("search")]
    public Task<ListResult<ComponentStockLevelDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchComponentStockLevelsQuery, ListResult<ComponentStockLevelDto>>(
            new SearchComponentStockLevelsQuery(request), cancellationToken);
    }

    [HttpPost("export")]
    public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportComponentStockLevelsQuery, ExcelFile>(
                new ExportComponentStockLevelsQuery(request), cancellationToken))
            .ToFileResult();
    }
}
