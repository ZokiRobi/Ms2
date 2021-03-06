﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Movies.API.Data;
using Movies.API.Extensions;
using Movies.API.Helpers;
namespace Movies.API
{
    public class Startup
    {
        private readonly IHostingEnvironment _appEnv;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _appEnv = env;
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var key = Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value);
            services.AddCors();
            services.AddDbContext<MoviesContext>(x => 
            x.UseMySql("Server=localhost;Database=moviesstore;Uid=appuser;Pwd=flatron")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning)));
            services.AddMvc().AddJsonOptions(opt =>
            {
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            }); ;
            services.AddTransient<Seed>();
            services.AddAutoMapper();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }
        // public void ConfigureDevelopmentServices(IServiceCollection services)
        // {
        //     var key = Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value);
        //     services.AddCors();
        //     services.AddMvc().AddJsonOptions(opt =>
        //     {
        //         opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        //     }); ;
        //     services.AddTransient<Seed>();
        //     services.AddAutoMapper();
        //     services.AddScoped<IAuthRepository, AuthRepository>();
        //     services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
        //     services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options =>
        //     {
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuerSigningKey = true,
        //             IssuerSigningKey = new SymmetricSecurityKey(key),
        //             ValidateIssuer = false,
        //             ValidateAudience = false
        //         };
        //     });
        // }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {


                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        context.Response.AddApplicationError(error.Error.Message);
                        if (error != null)
                        {
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            seeder.SeedAll();
            app.UseCors(x =>
            x.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowCredentials());

            app.UseAuthentication();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc(routes => {
                routes.MapSpaFallbackRoute(
                    name:"spa-fallback",
                    defaults:new {controller = "Fallback", action = "Index"}
                );
            });
        }
    }
}
