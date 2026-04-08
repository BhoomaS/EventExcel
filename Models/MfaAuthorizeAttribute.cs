using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MemberSummary.Models
{
    public class MfaAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated ||
                !user.HasClaim(c => c.Type == "MfaCompleted" && c.Value == "true"))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null); // Adjust controller/action as needed
            }
        }
    }
}
