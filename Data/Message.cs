namespace TextCommunicator.Data;

public class Message
{
    public int Id { get; set; }

    public string SenderId { get; set; } = default!;
    public ApplicationUser Sender { get; set; } = default!;

    public string RecipientId { get; set; } = default!;
    public ApplicationUser Recipient { get; set; } = default!;

    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
