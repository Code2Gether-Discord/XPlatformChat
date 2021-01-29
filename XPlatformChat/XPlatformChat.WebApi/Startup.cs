using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using XPlatformChat.Lib.Models;
using XPlatformChat.WebApi.Data;
using XPlatformChat.WebApi.Hubs;

namespace XPlatformChat.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            SetEFDataProvider(services);

            services.AddIdentity<ChatUser, IdentityRole>()
                .AddEntityFrameworkStores<ChatDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(ConfigureAuthentication).AddJwtBearer(ConfigureJwtBearer);
            services.AddSwaggerGen();
            services.AddSignalR();
        }

        private static void ConfigureAuthentication(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private void ConfigureJwtBearer(JwtBearerOptions options)
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = Configuration["JWT:ValidAudience"],
                ValidIssuer = Configuration["JWT:ValidIssuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))
            };
        }

        private void SetEFDataProvider(IServiceCollection services)
        {
            string environment = Configuration.GetValue<string>("Environment");

            if (environment == "Production")
            {
                NpgsqlConnectionStringBuilder builder = new(Configuration.GetConnectionString("PostgresIdentityStores"));
                builder.Password = Configuration["Password"];
                string connStr = builder.ConnectionString;

                services.AddDbContext<ChatDbContext>(options => options.UseNpgsql(connStr));
            }
            else
            {
                string connStr = Configuration.GetConnectionString("IdentityStores");
                services.AddDbContext<ChatDbContext>(options => options.UseSqlServer(connStr));
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();


            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "XPlatformChat");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/Chat");
            });
        }
    }
}
