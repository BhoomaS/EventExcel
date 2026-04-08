using DocumentFormat.OpenXml.InkML;
using MemberSummary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using MemberSummary.Controllers;
using Microsoft.Extensions.Logging;

namespace MemberSummary.Services
{
    public interface IAuthManager
    {
        Task<LoginResult> LoginAsync(Login model, ApplicationRoleId roleId, string UserUniqueKey, string SetupCode, string BarcodeImageUrl);
        Task<LoginResult> LoginAsync(Login model, int roleId, string UserUniqueKey, string SetupCode, string BarcodeImageUrl);
        
        Task<MFAResult> MFAAsync(MFA mfaModel);
        Task<bool> ValidateUserAsync(string username, string password);
        Task<bool> VerifyTwoFactorCodeAsync(string userId, string code);

    }
}
