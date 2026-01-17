namespace TextCommunicator.Data;

public class GroupMessage
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public string SenderId { get; set; } = default!;
    public ApplicationUser Sender { get; set; } = default!;

    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
