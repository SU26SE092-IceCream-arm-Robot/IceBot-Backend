using Application.Identity.InternalAccounts.Requests;

namespace Application.Identity.InternalAccounts;

internal static class InternalAccountRequestValidator
{
    public static string? ValidateRequest(CreateInternalAccountRequest request)
    {
        /* TARGET FLOW - temporarily disabled for direct credential delivery.
         * if (!request.LocalLoginEnabled && !request.GoogleLoginEnabled)
         *     return "At least one authentication method must be enabled.";
         * if (request.CreateInvitation && !string.IsNullOrWhiteSpace(request.InitialPassword))
         *     return "Initial password is not allowed when creating an invitation.";
         * if (request.LocalLoginEnabled && !request.CreateInvitation &&
         *     string.IsNullOrWhiteSpace(request.InitialPassword))
         *     return "Initial password is required when local login is enabled and no invitation is created.";
         */
        if (request.GoogleLoginEnabled && string.IsNullOrWhiteSpace(request.GoogleEmail))
        {
            return "Google email is required when Google login is enabled.";
        }

        if (request.Roles.Count == 0)
        {
            return "At least one role scope is required.";
        }

        return null;
    }
}
