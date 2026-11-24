namespace Tenengroup.Infrastructure.Common;

public static class Constants
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    public static class ParameterNames
    {
        public const string ApiServiceUsername = "API_SERVICE_USERNAME";
        public const string ApiServiceUserPassword = "API_SERVICE_USER_PASSWORD";
        public const string GroupPermissionsUrl = "GROUP_PERMISSIONS_URL";
        public const string CorsAllowedHosts = "CORS_ALLOWED_HOSTS";

        public static class Db
        {
            public const string Host = "DATABASE_HOST";
            public const string Port = "DATABASE_PORT";
            public const string DbName = "DATABASE_NAME";
            public const string Username = "DATABASE_USERNAME";
            public const string Password = "DATABASE_PASSWORD";
        }
    }
}
