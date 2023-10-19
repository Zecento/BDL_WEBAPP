namespace BDL_WEBAPP;

// This class is used for the user's favorite players
public class Favorite
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string? PlayerFirstName { get; set; }
    public string? PlayerLastName { get; set; }
    public int UserId { get; set; }
    // We don't want to store any more information about the player in this table
    // because it can change over time (seasons, points...). The data would become stale.
    // Eventually, we could implement a fetch from the API to get the latest information by player id
}