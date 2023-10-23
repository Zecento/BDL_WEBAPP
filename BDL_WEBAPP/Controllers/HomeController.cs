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
        using (HttpClient client = new HttpClient())
        {
            int previousYear = DateTime.Now.Year - 1;
            // let's pick a random page number that is high enough to only get the meta object
            // we will use this object to get the total number of pages.
            // TEST LATENCY: It doesn't actually make a difference, because the latency is the same,
            // so we could just as well use page 1.
            
            string apiUrlGetPage = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear +
                                   "&per_page=100&page=999999";
            HttpResponseMessage responseGetPage = await client.GetAsync(apiUrlGetPage);
            if (!responseGetPage.IsSuccessStatusCode) return View(players);
            // extract the meta object
            string responseDataGetPage = await responseGetPage.Content.ReadAsStringAsync();
            JObject jsonObjectGetPage = JObject.Parse(responseDataGetPage);
            JToken meta = jsonObjectGetPage["meta"]!;
            // extract the total number of pages
            int pageToLoad = meta["total_pages"]!.ToObject<int>();
        

            // get the last page
            string apiUrl = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear +
                            "&per_page=100&page=" + pageToLoad;
            
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseData);

                // Extract the players who scored the highest number of points in the current season
                JToken[] topScorers = jsonObject["data"]!.Children().ToArray();

                foreach (var scorer in topScorers)
                {
                        players.Add(new Player
                        {
                            Id = scorer["player"]?["id"]?.ToObject<int>(),
                            PlayerName = scorer["player"]?["first_name"] + " " + scorer["player"]?["last_name"],
                            PointsScored = scorer["pts"]!.ToObject<int>(),
                            Assists = scorer["ast"]!.ToObject<int>(),
                            Rebounds = scorer["reb"]!.ToObject<int>(),
                            CurrentPage = pageToLoad,
                            GameDate = scorer["game"]?["date"]?.ToObject<string>()?.Split("T")[0]
                        });
                }
                // sum together the points scored by each player if two have the same id
                
                players = players.GroupBy(p => p.Id)
                    .Select(g => new Player
                    {
                        Id = g.Key,
                        PlayerName = g.First().PlayerName,
                        PointsScored = g.Sum(p => p.PointsScored),
                        Assists = g.Sum(p => p.Assists),
                        Rebounds = g.Sum(p => p.Rebounds),
                        CurrentPage = pageToLoad,
                        GameDate = g.First().GameDate
                    }).ToList();

                // sort the list by points scored (so far. We still have to calculate the total points scored)
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
                    CurrentPage = pageToLoad,
                    GameDate = "Error"
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