// Program.cs - register SOMS context and service
var somsConn = Environment.GetEnvironmentVariable("SOMS_CONNECTION_STRING") ??
               builder.Configuration.GetConnectionString("SomsConnection");

if (string.IsNullOrWhiteSpace(somsConn))
{
    // Log a warning in development -- configuration required to read SOMS
    Console.WriteLine("Warning: SOMS_CONNECTION_STRING not configured. ServiceOrder endpoints will fail until configured.");
}

builder.Services.AddDbContext<SomsDbContext>(options =>
    options.UseSqlServer(somsConn));
builder.Services.AddScoped<IServiceOrderService, SomsServiceOrderService>();