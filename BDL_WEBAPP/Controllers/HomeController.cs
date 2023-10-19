using System.Diagnostics;
using BDL_WEBAPP.Data;
using Microsoft.AspNetCore.Mvc;
using BDL_WEBAPP.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BDL_WEBAPP.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _context;
    
    public HomeController(DataContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

        public IActionResult Login()
    {
        return View();
    }
    
    public async Task<IActionResult> Index()
    {
        // Ideally we would want to separate this logic into a separate service.
        // This would help reduce the loading time of the page, because we could
        // fetch the data from the API in the background, and then display the
        // data once it's ready. In the meantime, we could display a loading spinner.
        
        // But, for this simple application and to fulfill the "Calls to the players' API must be made using .NET (not Javascript)."
        // requirement, we will just keep it in the controller. Another option would be to use a Razor page instead of a controller.
        
        var players = new List<Player>();
        // append players to list after having hit the endpoint
        // https://www.balldontlie.io/api/v1/stats?seasons[]=2022&seasons[]=2023&postseason=true
        using (HttpClient client = new HttpClient())
        {
            int currentYear = DateTime.Now.Year;
            int previousYear = currentYear - 1;
            string apiUrl = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear + "&seasons[]=" + currentYear + "&postseason=true&per_page=100";
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseData);

                // Extract the players who scored the highest number of points in the current season
                JToken[] topScorers = jsonObject["data"]!.Children().ToArray();
                
                foreach (var scorer in topScorers)
                {
                    // if points scored !== 0 then add to list
                    // "the highest number of points in the current season."
                    // It's a relative constraint...
                    if (scorer["pts"]!.ToObject<int>() != 0)
                    {
                        players.Add(new Player
                        {
                            Id = scorer["player"]?["id"]?.ToObject<int>(),
                            PlayerName = scorer["player"]?["first_name"] + " " + scorer["player"]?["last_name"],
                            PointsScored = scorer["pts"]!.ToObject<int>(),
                            Assists = scorer["ast"]!.ToObject<int>(),
                            Rebounds = scorer["reb"]!.ToObject<int>(),
                            FieldGoalPercentage = scorer["fg_pct"]!.ToObject<double>()
                        });
                    }
                }
                // sort the list by points scored
                players.Sort((x, y) => y.PointsScored.CompareTo(x.PointsScored));
            }
            else
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");
                players.Add(new Player
                {
                    Id = 0,
                    PlayerName = "Error",
                    PointsScored = 0,
                    Assists = 0,
                    Rebounds = 0,
                    FieldGoalPercentage = 0
                });
            }
        }
        return await Task.FromResult<IActionResult>(View(players));
    }
    
    // Favorites
    public async Task<IActionResult> Favorites()
    {
        // reverse the list so that the most recent favorites are at the top (Last In, First Displayed)
        // + filter by currently logged user.
        string userId =  HttpContext.Request.Query["userId"]!;
        // ideally we would want more security here (confirm that the user is logged in and that they are the user they say they are)
        // but, for this simple application, we will just use the userId that is passed in the query string.
        return View(
            await _context.Favorites.Where(f => f.UserId == Convert.ToInt32(userId)).OrderByDescending(f => f.Id).ToListAsync()
        );
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}