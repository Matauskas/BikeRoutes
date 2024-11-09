using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.OpenApi.Models;
using System.Security.Policy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using web_backend.Repositories;
using System.Text.Json.Serialization;

namespace web_backend
{
    public class Program
    {
        public static void PreSerializeSwagger(OpenApiDocument doc, HttpRequest req)
        {
            doc.Servers.Add(new OpenApiServer
            {
                Url = "http://localhost:5253/api",
                Description = "localhost HTTP"
            });
            doc.Servers.Add(new OpenApiServer
            {
                Url = "https://localhost:7085/api",
                Description = "localhost HTTPS"
            });
            doc.Servers.Add(new OpenApiServer
            {
                Url = "https://komanda-x.rpuzonas.com/api",
                Description = "production"
            });
        }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Configuration["JWTKey"] == null)
            {
                var jwtKey = new byte[64];
                var rng = new Random();
                rng.NextBytes(jwtKey);
                Console.WriteLine(jwtKey);
                builder.Configuration["JWTKey"] = Convert.ToBase64String(jwtKey);
            }

            var configuration = builder.Configuration;
            builder.Services.AddDbContext<BaseDbContext>(options => options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<RoutesRepository>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<FriendsRepository>();
            builder.Services.AddScoped<PointsRepository>();
            builder.Services.AddScoped<ReviewsRepository>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
               .AddCookie(options =>
               {
                   options.Cookie.Name = "token";
               })
               .AddJwtBearer(options =>
               {
                   options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                   options.SaveToken = true;
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuerSigningKey = false,
                       IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(builder.Configuration["JWTKey"])), // TODO: Move key to environment variable
                       ValidateIssuer = false,
                       ValidateAudience = false,
                   };
                   options.Events = new JwtBearerEvents
                   {
                       OnMessageReceived = context =>
                       {
                           context.Token = context.Request.Cookies["token"];
                           return Task.CompletedTask;
                       },
                       OnTokenValidated = context =>
                       {
                           var userIdClaim = context.Principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier);
                           context.HttpContext.Items["userId"] = int.Parse(userIdClaim.Value);
                           return Task.CompletedTask;
                       }
                   };
               });
            builder.Services.AddAuthorization();

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddScoped<DbSeeder>();
            }

            var app = builder.Build();


            var uploadsFolder = "uploads";

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Duomenu bazes migracija
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BaseDbContext>();
                dbContext.Database.Migrate();
                dbContext.Initialize();

                if (builder.Environment.IsDevelopment())
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                    seeder.Initialize();
                }
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();


            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UsePathBase("/api");
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger(options =>
                {
                    options.PreSerializeFilters.Add(PreSerializeSwagger);
                });
                app.UseSwaggerUI();
            }

            /*app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 404; // Not Found
                    return;
                }

                await next();
            });*/
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
