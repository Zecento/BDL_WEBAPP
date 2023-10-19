namespace BDL_WEBAPP;

// This class is used for the user's personal information (just email for now)
public class User
{
    public int Id { get; set; }
    public required string Email { get; set; } // Email is required
    // We can add optional parameters for a more complex use-case
}