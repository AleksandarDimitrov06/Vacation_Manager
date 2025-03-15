using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vacation_Manager.Data;
using Vacation_Manager.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();



builder.Services.AddRazorPages();



builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    
    app.UseHsts();
}


//������������ ����� (CEO), ����� �� ���� �� ����� ������� �� �������� �����������
//��� ���� ��� ����������� �� ���������� ����������� �� �� ������ ���� "Unassigned" ������ �� ��� ���� ���� CEO, ����� �� ������ ����
async Task CreateAdmin(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        string adminEmail = "admin@admin.com";
        string adminPassword = "Admin@123";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new User { FirstName = "FirstName", LastName = "LastName", UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(admin, "CEO");
                if (!roleResult.Succeeded)
                {
                    logger.LogError($"Failed to assign Admin role to user {adminEmail}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogError($"Failed to create admin user {adminEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await CreateAdmin(app);

app.Run();
