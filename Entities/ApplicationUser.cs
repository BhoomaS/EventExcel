using System;

namespace Entities
{
    public class ApplicationUser
    {
        // Example properties, make sure to add the actual ones you need.
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        // If you need the implicit conversion from MemberSummary.Models.ApplicationUser, implement it here:
        public static implicit operator ApplicationUser(MemberSummary.Models.ApplicationUser v)
        {
            return new ApplicationUser
            {
                Id = v.Id,
                UserName = v.UserName,
                Email = v.Email
                // Map other properties as needed
            };
        }
    }
}
