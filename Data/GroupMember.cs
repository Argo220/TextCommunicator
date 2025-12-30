namespace TextCommunicator.Data;

public class GroupMember
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
}
