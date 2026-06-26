using ascent_app.Data;
using ascent_app.Models;
using ascent_app.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AscentDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AscentDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IDifficultyService, DifficultyService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IGeoService, GeoService>();
builder.Services.AddScoped<IImageStorage, ImageStorage>();
builder.Services.AddHttpClient<IExternalTrailSource, OverpassTrailSource>();
builder.Services.AddHttpClient<ITrailPhotoSource, OpenverseTrailPhotoSource>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await DbInitializer.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
