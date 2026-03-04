using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SNYC_Transport.Data;
using SNYC_Transport.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<ITransportRequestService, InMemoryTransportRequestService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    const string adminRole = "Admin";
    const string demoEmail = "admin@local.test";
    const string demoPassword = "Password123!";

    // demo user account

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }

    var existingUser = await userManager.FindByEmailAsync(demoEmail);
    if (existingUser is null)
    {
        var user = new IdentityUser
        {
            UserName = demoEmail,
            Email = demoEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, demoPassword);
        existingUser = user;
    }

    if (existingUser is not null && !await userManager.IsInRoleAsync(existingUser, adminRole))
    {
        await userManager.AddToRoleAsync(existingUser, adminRole);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<IdentityUser> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "on";
    var returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.LocalRedirect("/auth?error=Please+enter+email+and+password");
    }

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        return Results.LocalRedirect("/auth?error=Invalid+email+or+password");
    }

    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

app.MapPost("/auth/logout", async (HttpContext httpContext, SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();

    var form = await httpContext.Request.ReadFormAsync();
    var returnUrl = form["returnUrl"].ToString();
    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/auth" : returnUrl);
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();