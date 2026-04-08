using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MemberSummary.Models
{
    public static class IdentityClaimKeys
    {
        public static readonly string UserId = "UserId";
        public static readonly string ChMemberId = "CH_MEMBER_ID";
    }

    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _accessor;


        public IdentityService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private string GetIdentityClaim(string claimKey)
        {
            var identity = _accessor.HttpContext?.User?.Identity as ClaimsIdentity;
            if (identity != null && identity.IsAuthenticated)
            {
                var claim = identity.Claims.FirstOrDefault(o => o.Type == claimKey);
                if (claim != null)
                    return claim.Value;
            }
            return null;
        }

        public int? UserId
        {
            get
            {
                string claim = GetIdentityClaim(IdentityClaimKeys.UserId);
                if (!string.IsNullOrWhiteSpace(claim))
                {
                    return int.Parse(claim);
                }
                return null;
            }
        }

        public int? ChMemberId
        {
            get
            {
                string claim = GetIdentityClaim(IdentityClaimKeys.ChMemberId);
                if (!string.IsNullOrWhiteSpace(claim))
                {
                    return int.Parse(claim);
                }
                return null;
            }
        }

    }
}
