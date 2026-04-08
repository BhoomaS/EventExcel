namespace MemberSummary.Models
{
    public interface IIdentityService
    {
        int? UserId { get; }
        int? ChMemberId { get; }
    }
}
