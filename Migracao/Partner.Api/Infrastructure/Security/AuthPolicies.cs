namespace Partner.Api.Infrastructure.Security;

public static class AuthPolicies
{
    public const string Users = "UsersPolicy";
    public const string UsersInsert = "UsersInsertPolicy";
    public const string UsersUpdate = "UsersUpdatePolicy";
    public const string UsersDelete = "UsersDeletePolicy";

    public const string Companies = "CompaniesPolicy";
    public const string CompaniesInsert = "CompaniesInsertPolicy";
    public const string CompaniesUpdate = "CompaniesUpdatePolicy";
    public const string CompaniesDelete = "CompaniesDeletePolicy";

    public const string Clients = "ClientsPolicy";
    public const string ClientsInsert = "ClientsInsertPolicy";
    public const string ClientsUpdate = "ClientsUpdatePolicy";
    public const string ClientsDelete = "ClientsDeletePolicy";

    public const string Import = "ImportPolicy";
}

