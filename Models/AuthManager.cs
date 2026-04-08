using MemberSummary.Models; // Ensure using correct ApplicationUser
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MemberSummary.Controllers;
using Google.Authenticator;
using Microsoft.IdentityModel.Tokens;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MemberSummary.Services
{
    public class AuthManager : IAuthManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthManager> _logger;

        private const string _noAccountErrorMsg = "No account found for the email address provided";
        private const string _cannotAuthErrorMsg = "Unable to authenticate with the email and password provided";
        private const string _invalidPasswordErrorMsg = "Password does not meet the requirements";

        public AuthManager(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthManager> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }


        // Login method with int (previous signature)
        public async Task<LoginResult> LoginAsync(Login model, ApplicationRoleId roleId, string UserUniqueKey, string SetupCode, string BarcodeImageUrl)
        {
            var result = new LoginResult()
            {
                Detail = model,
                Succeeded = false,
                Token = string.Empty,
                Message = string.Empty,
                ValidTo = null,
                SetupCode = string.Empty,
                UserUniqueKey = string.Empty,
                BarcodeImageUrl = null,
                Id = 0,
                IsTwoFactorEnabled = false,
            };

            string password = result.Detail.Password;
            result.Detail.Password = string.Empty;

            ApplicationUser user = null;

            if (!string.IsNullOrWhiteSpace(model.UserName))
            {
                user = await _userManager.FindByNameAsync(model.UserName);
            }

            if (user == null)
            {
                result.Message = "No account found for the username provided";
                return result;
            }

            try
            {
                var signInResult = await _signInManager.PasswordSignInAsync(user, password, true, true);

                if (signInResult.Succeeded)
                {
                        var claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.NormalizedUserName),
                    new Claim(IdentityClaimKeys.UserId, user.Id.ToString())
                };
                    var jwtKey = MemberSummary.Models.Config.GetJwtTokenKey();
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)); // Use the key for signing
                    //var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        issuer: null,  // No issuer defined
                        audience: null,  // No audience defined
                        claims,
                        expires: DateTime.UtcNow.AddMinutes(60),  // Default expiration time (can be adjusted)
                        signingCredentials: creds);

                    result.Succeeded = true;
                    result.Token = new JwtSecurityTokenHandler().WriteToken(token);
                    result.Message = string.Empty;
                    result.ValidTo = token.ValidTo;
                    result.SetupCode = SetupCode;
                    result.UserUniqueKey = UserUniqueKey;
                    result.BarcodeImageUrl = BarcodeImageUrl;
                    result.Id = user.Id;
                    result.IsTwoFactorEnabled = user.TwoFactorEnabled;

                }
                else if (signInResult.IsLockedOut)
                {
                    result.Message = "The account provided is locked out. Please try again later.";
                }
                else
                {
                    result.Message = "Unable to authenticate with the provided credentials";
                }
            }
            catch (Exception ex)
            {
                result.Message = "An unexpected error occurred while authenticating";
                _logger.LogError($"Error during login: {ex.Message}");
            }

            return result;
        }



        // MFA method (still not implemented)
        public async Task<MFAResult> MFAAsync(MFA mfaAuthModel)
        {
            Action<MFAResult, string> addError = (res, msg) =>
            {
                res.Succeeded = false;
                res.Message = msg;
                res.IsValidTwoFactorAuthentication = false;
            };

            var result = new MFAResult()
            {

                Succeeded = false,
                Message = string.Empty,
                IsValidTwoFactorAuthentication = false
            };


            TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
            string UserUniqueKey = (mfaAuthModel.UserUniqueKey).ToString();
            bool isValid = TwoFacAuth.ValidateTwoFactorPIN(UserUniqueKey, mfaAuthModel.CodeDigit, false);

            if (isValid)
            {
                //HttpCookie TwoFCookie = new HttpCookie("TwoFCookie");
                //string UserCode = Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(UserUniqueKey)));

                result.Succeeded = true;
                result.Message = string.Empty;
                result.IsValidTwoFactorAuthentication = true;
                result.Success = true;
            }
            else
            {
                addError(result, "An unexpected error occurred while authenticating");
            }

            return result;

        }

        // Validate user method (still not implemented)
        public Task<bool> ValidateUserAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        // Verify two-factor code method (still not implemented)
        public Task<bool> VerifyTwoFactorCodeAsync(string userId, string code)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> LoginAsync(Login model, int roleId, string UserUniqueKey, string SetupCode, string BarcodeImageUrl)
        {
            throw new NotImplementedException();
        }
    }


}
