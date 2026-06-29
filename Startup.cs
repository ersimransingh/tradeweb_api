using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TradeWeb.API.Data;
using TradeWeb.API.Repository;
using AspNetCoreRateLimit;
using TradeWeb.API.Controllers;
using Microsoft.AspNetCore.HttpOverrides;
using TradeWeb.API.QuestPdfServicesClass;

namespace TradeWeb.API
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
            services.AddDbContext<DataContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitingPolicies"));

            services.AddMemoryCache();

            // Add required services for IP Rate Limiting
            services.AddInMemoryRateLimiting();

            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Register the IP Rate Limiting middleware
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            //// TODO : Added dependancy here
            services.AddScoped<UtilityCommon>();
            services.AddScoped<ModelTradeWebCommon>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ITradeWebRepository, TradeWebRepository>();
            services.AddScoped<ICrossWebRepository, CrossWebRepository>();
            services.AddScoped<IEstroWebRepository, EstroWebRepository>();
            services.AddScoped<ITradeNetRepository, TradeNetRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<ITradeMobileRepository, TradeMobileRepository>();
            services.AddScoped<ICrossNetRepository, CrossNetRepository>();
            services.AddScoped<IEstroNetRepository, EstroNetRepository>();
            services.AddScoped<IWhatsAppRepository, WhatsAppRepository>();
            services.AddSingleton<TokenStore>();
            services.AddScoped<MainController.EncryptResponseAttribute>();
            services.AddScoped<QuestPdfServiceClass>();
            services.AddScoped<ClientFundLedgerQuestService>();
            //services.AddScoped<ValidateUser>();

            services.Configure<RequestLocalizationOptions>(
                options =>
                {
                    var supportedCultures = new List<CultureInfo>
                        {
                            new CultureInfo("en-GB"),
                        };

                    options.DefaultRequestCulture = new RequestCulture(culture: "en-GB", uiCulture: "en-GB");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                // For IIS / reverse proxy
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            //// End dependancy
            services.AddControllers();
            services.AddMvcCore().AddApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TradeWeb.API", Version = "v1" });
                var security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                    }
                });
            });
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "TradeAPI.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            string orginAllowed = "";
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(GetConnectionStr(Configuration.GetConnectionString("DefaultConnection"))))
                {
                    sqlCon.Open();
                    UtilityCommon utilityCommon = new UtilityCommon();
                    if (utilityCommon.fnchkTable("Webparameter"))
                    {
                        orginAllowed = utilityCommon.GetWebParameter("OriginAllowed");
                    }
                }
            }
            catch (Exception)
            {
                // Fallback when SQL connection cannot be established during startup (e.g. globalization invariant mode).
            }
            if (orginAllowed == "")
            {
                orginAllowed = Configuration["AllowedOrigins"];
            }
            if (!string.IsNullOrEmpty(orginAllowed))
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowBackOffice", builder =>
                    {
                        builder.WithOrigins(orginAllowed.Split(","))
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                        // .AllowCredentials(); // only if you're sending cookies or auth headers
                    });
                    //options.AddDefaultPolicy(builder =>
                    //    builder.WithOrigins(orginAllowed.Split(","))
                    //    .AllowAnyMethod()
                    //    .AllowAnyHeader());
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowBackOffice", builder =>
                         builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
                });
            }
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Decrypt(Configuration["Jwt:Key"])))
                    };
                });

            services.AddMvc();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            app.UseIpRateLimiting();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../swagger/v1/swagger.json", "TradeWeb.API V1");
            });
            app.UseRouting();

            //app.UseCors(); //// Allowing CORS policy
            app.UseCors("AllowBackOffice");

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-XSS-Protection", "1; mode = block"); //// ClickJacking 
                context.Response.Headers.Add("X-Frame-Options", "DENY");  //// Missing securing Headers
                //context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                context.Response.Headers.Add("Server", "unknown"); //// Server version desclosure
                context.Response.Headers.Add("Feature-Policy", "accelerometer 'none'; camera 'none'; microphone 'none';");
                context.Request.Headers.Remove("Origin");
                if (context.Request.Method == "OPTIONS") //// Option method desable
                {
                    context.Response.StatusCode = 405;
                }
                await next.Invoke();
            });
            var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(options.Value);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public string Decrypt(string strenc)
        {
            int m = 0;
            string strEncKey = null;
            string gsEcDc = null;
            string gsFinal = null;
            string gsCompare = null;
            int glNumber = 0;
            strEncKey = "ASHOKKHE";
            gsFinal = "";
            glNumber = Strings.Len(Strings.Trim(strenc));
            for (m = 1; m <= Math.Round((decimal)glNumber / 8) + 1; m++)
            {
                strEncKey = strEncKey + strEncKey;
            }
            m = 0;
            for (m = 1; m <= glNumber; m++)
            {
                try
                {
                    gsEcDc = Strings.Mid(strenc, m, 1);
                    gsCompare = Strings.Mid(strEncKey, m, 1);
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    gsFinal = gsFinal + Strings.Chr(Strings.Asc(gsEcDc) - Strings.Asc(gsCompare) - 13);
                }
                catch (Exception e) { }
            }
            return gsFinal;
        }

        public string GetConnectionStr(string StrConn)
        {
            try
            {
                if (StrConn.Contains("EPWD"))
                {
                    string epwd = StrConn.Split("EPWD=")[1].Split(";")[0];
                    string pwd = "";
                    if (!string.IsNullOrWhiteSpace(epwd))
                    {
                        pwd = Decrypt(epwd);
                        StrConn = StrConn.Replace(epwd, pwd);
                    }
                    StrConn = StrConn.Replace("EPWD", "Pwd");
                }
                return StrConn;
            }
            catch (Exception ex) { }

            return "";
        }
    }
}
