namespace BDL_WEBAPP.Models;

public class Player
{
    public int? Id { get; set; }
    public string? PlayerName { get; set; }
    public int PointsScored { get; set; }
    public int Assists { get; set; }
    public int Rebounds { get; set; }
    public double FieldGoalPercentage { get; set; }
}