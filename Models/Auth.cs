using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using TypeLite;

namespace MemberSummary.Models
{
    [TsClass]
    public class Login
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [TsClass] // <-- Use [TsClass] instead of missing [TypescriptInclude]
    public class MFA
    {
        public string CodeDigit { get; set; }
        public string UserUniqueKey { get; set; }
        public string UserName { get; internal set; }
    }

    [TsClass] // <-- Again use [TsClass]
    public class MFAResult : SaveResult<MFA>
    {
        public bool Succeeded { get; set; }
        public bool IsValidTwoFactorAuthentication { get; set; }
    }

    public class LoginResult : SaveResult<Login>
    {
        public bool Succeeded { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public DateTime? ValidTo { get; set; }
        public string SetupCode { get; set; }
        public string UserUniqueKey { get; set; }
        public string BarcodeImageUrl { get; set; }
        public int Id { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public Login Detail { get; set; }
        public object ModelErrors { get; internal set; }
    }



    public enum ApplicationRoleId
    {
        /// <summary>Management Portal User</summary>
        ManagementPortalUser = 1,
        /// <summary>MyPHA Member User</summary>
        MyPhaMemberUser = 2,
        /// <summary>Management Portal Admin</summary>
        ManagementPortalAdmin = 3,
    }

    [TsClass]
    public class SaveResult<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


    }







}
