namespace Catalog.Api;

public static class AppConstants
{
    public const string TesterRoleName = "tester";
    public const string AdministrationRoles = $"manager,admin,{TesterRoleName}";
}