namespace ascent_app.Models;

public class TrailTag
{
    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
