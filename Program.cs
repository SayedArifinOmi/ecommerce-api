using AuthApi.Data;
using AuthApi.Middleware;
using AuthApi.Options;
using AuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JwtOptions from config
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();
//Options
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<ClientOptions>(builder.Configuration.GetSection("Client"));
// Services
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<CartService>();

// JWT authentication
var jwt = builder.Configuration.GetSection("Jwt");

var key = jwt["Key"] ?? throw new Exception("Jwt:Key not set");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwt["Issuer"]),
            ValidateAudience = !string.IsNullOrEmpty(jwt["Audience"]),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

// CORS
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new string[0];
builder.Services.AddCors(o => o.AddPolicy("AllowAngular", p =>
{
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Use full namespace for schema names to avoid conflicts
    c.CustomSchemaIds(type => type.FullName);
});



var app = builder.Build();

// Auto apply migrations (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
     db.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.UseDeveloperExceptionPage();

app.Run();
