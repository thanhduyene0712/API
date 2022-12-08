using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Reso.Core.Extension;
using System.Text;
using System.Text.Json.Serialization;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.SERVICES.App_Start;
using UPOD.SERVICES.Handlers;
using UPOP.SERVICES.App_Start;

#region Cors Policy
var MyAllowSpecificOrigins = "_myAllowSpecificOrigns";
#endregion
var builder = WebApplication.CreateBuilder(args);

#region authenticate
var tokenValidationParams = new TokenValidationParameters
{
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = true,
    RequireExpirationTime = false,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secretkey"]))
};

builder.Services.AddSingleton(tokenValidationParams);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.SaveToken = true;
                    opt.TokenValidationParameters = tokenValidationParams;
                });
builder.Services.AddMvc(opt =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddHttpContextAccessor();
#endregion

#region Cors Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                      });
});
#endregion
// Add services to the container.

//builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<Database_UPODContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();

//Authenticate
#region authenticae
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oath2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                    new List<string>()
                    }
                });

});
#endregion

builder.Services.ConfigureFilter<ErrorHandlingFilter>();
builder.Services.JsonFormatConfig();
builder.Services.ConfigureAutoMapper();
builder.Services.ConfigureDI();
builder.Services.ConfigureServiceWorkers();
builder.Services.ConfigDataProtection();
builder.Services.AddSignalR();


//var port = Environment.GetEnvironmentVariable("PORT");
//builder.WebHost.UseUrls("http://*:" + port);

#region hangfire_schedule
builder.Services.AddHangfire(x => x.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
#endregion
var app = builder.Build();
// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();

#region hangfire_schedule
app.UseHangfireDashboard("/hangfire_schedule", new DashboardOptions
{
    DashboardTitle = "Auto update schedule jobs",
    Authorization = new[]
                {
                    new HangfireAuthorizationFilter("admin")
                }
});
#endregion
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);
#region authenticate
app.UseAuthentication(); //authenticate
#endregion

app.UseAuthorization();

app.MapHub<NotifyHub>("/notifyHub");

//app.UseEndpoints(endpoint =>
//{
//    endpoint.MapHub<NotifyHub>("/notifyHub");
//});

app.MapControllers();

app.Run();