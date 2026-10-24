namespace Zapper.ApiCommon.Security;

public static class PermissionClaim
{
    // the claim each handler grants, and the one an operation's permission policy requires
    public const string Type = "permission";
}
