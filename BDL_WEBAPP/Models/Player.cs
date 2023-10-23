namespace BDL_WEBAPP.Models;

public class Player
{
    public int? Id { get; set; }
    public string? PlayerName { get; set; }
    public int PointsScored { get; set; }
    public int Assists { get; set; }
    public int Rebounds { get; set; }
    
    // These are not technically part of the Player model
    // but we need them to optimize how we display the data in the view
    public int CurrentPage { get; set; }
    public string? GameDate { get; set; }
}