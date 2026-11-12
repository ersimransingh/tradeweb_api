using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using QuestPDF.Drawing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using TradeWeb.API.Controllers;
using TradeWeb.API.Filters;
using TradeWeb.API.QuestPdfServicesClass;
using TradeWeb.API.Repository;
using TradeWeb.API.Service;

namespace TradeWeb.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;

            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "cour.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "courbd.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "couri.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "courbi.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "calibri.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(env.ContentRootPath, "Fonts", "calibrib.ttf")));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDbContext<DataContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Register as singleton
            services.AddScoped(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var decrypted = GetDecryptedConnection(
                    cfg.GetConnectionString("DefaultConnection")
                );
                return new ConnectionString(decrypted);
            });

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
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ITradeWebRepository, TradeWebRepository>();
            services.AddScoped<IWhatsAppRepository, WhatsAppRepository>();
            services.AddSingleton<TokenStore>();
            services.AddSingleton<E2EKeyService>();
            services.AddScoped<MainController.EncryptResponseAttribute>();
            services.AddScoped<QuestPdfServiceClass>();
            services.AddScoped<E2EDecryptFilter>();
            services.AddScoped<QuestPdfCommonService>();
            //services.AddScoped<ValidateUser>();
            services.AddScoped<DigitalEmailService>();                 // scoped, not singleton
            services.AddSingleton(Channel.CreateUnbounded<DigitalEmailJob>());
            services.AddHostedService<DigitalEmailWorker>();

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
                //options.KnownNetworks.Clear();
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            //// End dependancy
            services.AddControllers();
            services.AddMvcCore().AddApiExplorer();

            #region old code swagger
            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TradeWeb.API", Version = "v1" });
            //    var security = new Dictionary<string, IEnumerable<string>>
            //    {
            //        {"Bearer", new string[] { }},
            //    };
            //    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            //    {
            //        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            //        Name = "Authorization",
            //        In = ParameterLocation.Header,
            //        Type = SecuritySchemeType.ApiKey,
            //        BearerFormat = "JWT",
            //    });
            //    /*c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //    {
            //        {
            //              new OpenApiSecurityScheme
            //                {
            //                    Reference = new OpenApiReference
            //                    {
            //                        Type = ReferenceType.SecurityScheme,
            //                        Id = "Bearer"
            //                    }
            //                },
            //                new string[] {}
            //        }
            //    });*/
            //    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            //    {
            //        {
            //            new OpenApiSecuritySchemeReference("Bearer"),
            //            new List<string>()
            //        }
            //    });
            //});
            #endregion

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TradeWeb.API",
                    Version = "v1"
                });

                //c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                //{
                //    Name = "Authorization",
                //    Type = SecuritySchemeType.Http,
                //    Scheme = "bearer",
                //    BearerFormat = "JWT",
                //    In = ParameterLocation.Header,
                //    Description = "Enter: Bearer {token}"
                //});

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {token}"
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
                using (var sqlCon = new SqlConnection(GetConnectionStr(Configuration.GetConnectionString("DefaultConnection"))))
                {
                    sqlCon.Open();

                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM sysobjects WHERE name = 'WebParameter'", sqlCon))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            cmd.CommandText = "SELECT sp_sysvalue FROM WebParameter WITH (NOLOCK) WHERE sp_parmcd = 'OriginAllowed'";

                            object result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                orginAllowed = result.ToString();
                            }
                        }
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
                               .AllowAnyHeader()
                               .WithExposedHeaders(E2EEncryptionHelper.PublicKeyHeader);
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
                        .AllowAnyHeader()
                        .WithExposedHeaders(E2EEncryptionHelper.PublicKeyHeader));
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

            services.AddHostedService<SchedulerService>();

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
                context.Response.OnStarting(() =>
                {
                    if (context.Request.IsHttps)
                    {
                        context.Response.Headers["Strict-Transport-Security"] =
                            "max-age=31536000; includeSubDomains; preload";
                    }

                    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
                    context.Response.Headers["Referrer-Policy"] = "no-referrer";
                    context.Response.Headers["Server"] = "unknown";
                    context.Response.Headers["Feature-Policy"] = "accelerometer 'none'; camera 'none'; microphone 'none';";
                    context.Response.Headers["Permissions-Policy"] = "accelerometer=(), camera=(), microphone=(), geolocation=()";

                    return Task.CompletedTask;
                });

                context.Request.Headers.Remove("Origin");

                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                await next();
            });
            var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(options.Value);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public static string Decrypt(string strenc)
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
                catch (Exception) { }
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
            catch (Exception) { }

            return "";
        }

        private static string GetDecryptedConnection(string connection)
        {
            if (connection.Contains("EPWD"))
            {
                //UtilityCommon utilityCommon = new UtilityCommon();

                string epwd = connection.Split("EPWD=")[1].Split(";")[0];

                if (!string.IsNullOrWhiteSpace(epwd))
                {
                    string pwd = Decrypt(epwd);
                    connection = connection.Replace(epwd, pwd);
                }

                connection = connection.Replace("EPWD", "Pwd");
            }
            /*if (!connection.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
                connection += ";TrustServerCertificate=True";*/

            return connection;
        }

        public class ConnectionString
        {
            public string Default { get; }
            public ConnectionString(string defaultConn)
            {
                Default = defaultConn;
            }
        }
    }
}
