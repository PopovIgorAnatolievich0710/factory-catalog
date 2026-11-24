using Microsoft.Extensions.Configuration;

namespace Tenengroup.Infrastructure.Common;

public static class DbConnectionString
{
    public static string Build(IConfiguration configuration)
    {
        return $"Server={configuration[Constants.ParameterNames.Db.Host]};" +
               $"Port={configuration[Constants.ParameterNames.Db.Port]};" +
               $"Database={configuration[Constants.ParameterNames.Db.DbName]};" +
               $"User Id={configuration[Constants.ParameterNames.Db.Username]};" +
               $"Password={configuration[Constants.ParameterNames.Db.Password]};" +
               "Include Error Detail=True;";
    }
}
