using Microsoft.AspNetCore.Identity;

namespace MemberSummary.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FullName { get; set; } // FullName (length: 150)
        public int? CH_MEMBER_ID { get; set; } // CH_MEMBER_ID
        public int? PhaDetailId { get; set; } // PhaDetailId
        public bool IsEnabled { get; set; } // IsEnabled
    }
}
