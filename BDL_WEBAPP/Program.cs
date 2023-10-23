using BDL_WEBAPP;
using BDL_WEBAPP.Data;
using BDL_WEBAPP.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<DataContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// User endpoints:

// for this simple application, we can remove a lot of complexity
// by having only a single MapPost that will return the current user
// if they already exist, or create a new user if they don't exist.

// If this was a real application, we would want to have a separate
// MapPost for creating a new user, and a separate MapGet for getting
// a user with a specific email. This would allow us to have more flexibility.

app.MapPost(
    "/api/users",
    async (DataContext context, User user) => 
    {
        user.Email = System.Net.WebUtility.UrlDecode(user.Email);  // decode email
        // check if the user already exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (existingUser != null) return Results.Ok(existingUser);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return Results.Ok(user);
    });

// Favorites endpoints:

app.MapGet(
    "/api/favorites",
    async (DataContext context, int userId) => 
    {
        var favorites = await context.Favorites.Where(f => f.UserId == userId).ToListAsync();
        return Results.Ok(favorites);
    });

app.MapPost(
    "/api/favorites/create",
    async (DataContext context, Favorite favorite) => 
    {
        context.Favorites.Add(favorite);
        await context.SaveChangesAsync();
        return Results.Ok(favorite);
    });

app.MapDelete(
    "/api/favorites/delete",
    async (DataContext context, int id) => 
    {
        var favorite = await context.Favorites.FirstOrDefaultAsync(f => f.Id == id);
        if (favorite == null) return Results.NotFound();
        context.Favorites.Remove(favorite);
        await context.SaveChangesAsync();
        return Results.Ok(favorite);
    });

// Endpoint to query the NBA API:
app.MapGet(
    "/api/bdl/total_stats",
    async (DataContext context, int id) =>
    {
        // hit https://www.balldontlie.io/api/v1/stats?seasons[]=2022&per_page=100&&player_ids[]=1
        int previousYear = DateTime.Now.Year - 1;
        string apiUrl = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear +
                        "&per_page=100&&player_ids[]=" + id;
        var client = new System.Net.Http.HttpClient();
        var response = await client.GetAsync(apiUrl);
        var responseData = await response.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(responseData);
        JToken[] data = jsonObject["data"]!.Children().ToArray();
        JToken meta = jsonObject["meta"]!;
        // until next_page is !== null, keep hitting the endpoint and adding to data
        while (meta["next_page"]!.ToObject<int?>() != null)
        {
            // hit the endpoint again
            apiUrl = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear +
                     "&per_page=100&&player_ids[]=" + id + "&page=" + meta["next_page"]!.ToObject<int>();
            response = await client.GetAsync(apiUrl);
            responseData = await response.Content.ReadAsStringAsync();
            jsonObject = JObject.Parse(responseData);
            data = data.Concat(jsonObject["data"]!).ToArray();
            meta = jsonObject["meta"]!;
        }
        // data is an array of objects. Sum pts, ast, reb, fg_pct
        int totalPoints = 0;
        int totalAssists = 0;
        int totalRebounds = 0;
        string playerName = "";
        foreach (var datapoint in data)
        {
            totalPoints += datapoint["pts"]!.ToObject<int>();
            totalAssists += datapoint["ast"]!.ToObject<int>();
            totalRebounds += datapoint["reb"]!.ToObject<int>();
            playerName = datapoint["player"]?["first_name"] + " " + datapoint["player"]?["last_name"];
        }
        // return the sum of pts, ast, reb, fg_pct
        return Results.Ok(new
        {
            playerName,
            totalPoints,
            totalAssists,
            totalRebounds
        });
    });

// Endpoint to query the NBA API per page & current season
// https://www.balldontlie.io/api/v1/stats?seasons[]=2022&per_page=100&page=20
app.MapGet(
    "/api/bdl/stats",
    async (DataContext context, int page) =>
    {
        // hit https://www.balldontlie.io/api/v1/stats?seasons[]=2022&per_page=100&&player_ids[]=1
        int previousYear = DateTime.Now.Year - 1;
        string apiUrl = "https://www.balldontlie.io/api/v1/stats?seasons[]=" + previousYear +
                        "&per_page=100&page=" + page;
        var client = new System.Net.Http.HttpClient();
        var response = await client.GetAsync(apiUrl);
        var responseData = await response.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(responseData);
        JToken[] data = jsonObject["data"]!.Children().ToArray();
        // compose array of objects with player_id, player_name, pts, ast, reb, fg_pct
        List<Player> composedData = new List<Player>();
        foreach (var datapoint in data)
        {
            composedData.Add(
                new Player
                {
                    Id = datapoint["player"]?["id"]?.ToObject<int>(),
                    PlayerName = datapoint["player"]?["first_name"] + " " + datapoint["player"]?["last_name"],
                    PointsScored = datapoint["pts"]!.ToObject<int>(),
                    Assists = datapoint["ast"]!.ToObject<int>(),
                    Rebounds = datapoint["reb"]!.ToObject<int>(),
                    CurrentPage = page,
                    GameDate = datapoint["game"]?["date"]?.ToObject<string>()?.Split("T")[0]
                });
        }
        // return the sum of pts, ast, reb, fg_pct if the player_id is the same
        composedData = composedData.GroupBy(p => p.Id)
            .Select(g => new Player
            {
                Id = g.Key,
                PlayerName = g.First().PlayerName,
                PointsScored = g.Sum(p => p.PointsScored),
                Assists = g.Sum(p => p.Assists),
                Rebounds = g.Sum(p => p.Rebounds),
                CurrentPage = page,
                GameDate = g.First().GameDate
            }).ToList();

        return Results.Ok(composedData);
    });
    

app.Run();