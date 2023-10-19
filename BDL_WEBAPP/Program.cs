using BDL_WEBAPP;
using BDL_WEBAPP.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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
        // decode email
        user.Email = System.Net.WebUtility.UrlDecode(user.Email);
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

app.Run();