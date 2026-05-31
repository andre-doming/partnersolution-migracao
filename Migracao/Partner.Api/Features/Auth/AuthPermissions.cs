namespace Partner.Api.Features.Auth;

public static class AuthPermissions
{
    public const string UsersView = "funcUsuarios";
    public const string UsersInsert = "funcUsuariosIns";
    public const string UsersUpdate = "funcUsuariosUpd";
    public const string UsersDelete = "funcUsuariosDel";
    public const string UsersMfaAdmin = "funcUsuariosMfaAdmin";

    public const string CompaniesView = "funcEmpresas";
    public const string CompaniesInsert = "funcEmpresasIns";
    public const string CompaniesUpdate = "funcEmpresasUpd";
    public const string CompaniesDelete = "funcEmpresasDel";

    public const string ClientsView = "funcClientes";
    public const string ClientsInsert = "funcClientesIns";
    public const string ClientsUpdate = "funcClientesUpd";
    public const string ClientsDelete = "funcClientesDel";

    public const string Import = "funcImportar";
}


