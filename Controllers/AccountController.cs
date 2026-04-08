using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Google.Authenticator;
using MemberSummary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using MemberSummary.Services;
using System.Linq;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.IdentityModel.Tokens;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MemberSummary.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthManager _authManager;
        private readonly IEmailService _emailService;
        private const string _cannotAuthErrorMsg = "Unable to authenticate with the email and password provided";

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuthManager authManager)
        {
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _authManager = authManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        public ActionResult MFA()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login loginModel)
        {
            ApplicationUser user = null;
            var result = new LoginResult();
            IList<string> userRoles = new List<string>();

            if (loginModel != null && !string.IsNullOrWhiteSpace(loginModel.UserName))
            {
                try
                {
                    user = await _userManager.FindByNameAsync(loginModel.UserName);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error retrieving users: " + ex.Message);
                }
                //user = await _userManager.FindByNameAsync(loginModel.UserName);
            }

            if (user != null)
            {
                userRoles = await _userManager.GetRolesAsync(user);
            }

            if (userRoles != null && userRoles.Count > 0)
            {
                try
                {
                    string googleAuthKey = _configuration.GetValue<string>("GoogleAuthKey");
                    string userUniqueKey = user.UserName + googleAuthKey;

                    TwoFactorAuthenticator twoFacAuth = new TwoFactorAuthenticator();
                    var setupInfo = twoFacAuth.GenerateSetupCode("localhost:44348", user.UserName,
                        ConvertSecretToBytes(userUniqueKey, false), 300);
                    string barcodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                    string setupCode = setupInfo.ManualEntryKey;

                    foreach (var userRole in userRoles)
                    {
                        if (userRole == "Management Portal Admin")
                        {
                            result = await _authManager.LoginAsync(loginModel,
                                ApplicationRoleId.ManagementPortalAdmin, userUniqueKey, setupCode, barcodeImageUrl);
                        }
                        else if (userRole == "Management Portal User")
                        {
                            result = await _authManager.LoginAsync(loginModel,
                                ApplicationRoleId.ManagementPortalUser, userUniqueKey, setupCode, barcodeImageUrl);
                        }

                        if (result.Succeeded)
                        {
                            return Ok(new
                            {
                                setupCode = result.SetupCode,
                                token = result.Token,
                                userUniqueKey = result.UserUniqueKey,
                                barcodeImageUrl = result.BarcodeImageUrl,
                                id = result.Id,
                                isTwoFactorEnabled = result.IsTwoFactorEnabled
                            });
                        }

                        return BadRequest(new { message = result.Message });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception during login: " + ex.Message);
                }
            }

            return BadRequest(new { message = result.Message ?? "Login failed" });
        }

        [HttpPost("[action]")]
        [Route("Account/TwoFactorAuthenticate")]
        public async Task<IActionResult> TwoFactorAuthenticate([FromBody] MFA mfaAuthModel)
        {
            var result = new MFAResult();
            mfaAuthModel.UserName = User.Identity?.Name;

            if (mfaAuthModel != null && !string.IsNullOrWhiteSpace(mfaAuthModel.UserName))
            {
                try
                {
                    var user = await _userManager.FindByNameAsync(mfaAuthModel.UserName);
                    if (user == null)
                    {
                        return BadRequest(new { message = "User not found" });
                    }

                    var googleAuthKey = _configuration.GetValue<string>("GoogleAuthKey:Key");
                    mfaAuthModel.UserUniqueKey = user.UserName + googleAuthKey;

                    if (!string.IsNullOrWhiteSpace(mfaAuthModel.CodeDigit))
                    {
                        result = await _authManager.MFAAsync(mfaAuthModel);
                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Error during MFA", error = ex.Message });
                }
            }

            return BadRequest(new { message = result.Message ?? "Invalid MFA input" });
        }




        [HttpPost]
        [Route("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Account"); // or Home if you prefer
        }

        // Utility: convert base32 key string to byte[]
        private static byte[] ConvertSecretToBytes(string secret, bool secretIsBase32) =>
            secretIsBase32 ? Base32Encoding.ToBytes(secret) : Encoding.UTF8.GetBytes(secret);
    }
}
