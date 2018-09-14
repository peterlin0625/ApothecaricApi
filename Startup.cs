﻿using ApothecaricApi.Data;
using ApothecaricApi.Middleware;
using ApothecaricApi.Models.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ApothecaricApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApothecaricDbContext>(options =>
                        options.UseSqlServer(Configuration.GetConnectionString("AuthenticationConnection"),
                            b => b.MigrationsAssembly("ApothecaricApi")));

            services.AddIdentity<ApothecaricUser, IdentityRole>(cfg =>
            {
                cfg.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApothecaricDbContext>()
            .AddUserManager<ApothecaricUserManager>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        //ValidateAudience = true,
                        ValidIssuer = Configuration["Tokens:Issuer"],
                        ValidAudience = Configuration["Tokens:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"])),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });

            services.AddCors(options =>
            {
                options.AddPolicy("ChiroWareAppCors",
                    policy => policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });


            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("ChiroWareAppCors"));
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<TenantProviderMiddleware>();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
