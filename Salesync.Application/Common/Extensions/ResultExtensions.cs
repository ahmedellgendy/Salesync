using Microsoft.AspNetCore.Identity;

namespace Salesync.Application.Common.Extensions
{
    public static class ResultExtensions
    {
        public static void EnsureSuccess(this IdentityResult result)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException(errors);
            }
        }
    }
}
