using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using TN_Doc.Extensions;
using TN_Doc.Middleware;
using TN_Doc.Services;
using TN.Doc;
using TN_DocGeneral.Services;
using TN.Utils.Helpers;

namespace TN_Doc;

public class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	private IConfiguration Configuration { get; }

	// This method gets called by the runtime. Use this method to add services to the container.
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddLogging(builder =>
		{
			var logLevel = Configuration.GetValue<string>("Logging:LogLevel:Default");
			LogManager.Configuration.Variables["logLevel"] = logLevel;
			LogManager.Configuration.Variables["logDirectory"] = LoggingPathService.GetLogDirectory("TN_Doc");

			builder.ClearProviders();
			builder.AddNLog();
		});

		services.AddCors(options => options.AddPolicy("CorsPolicy",
			builder =>
			{
				builder.AllowAnyHeader()
					.AllowAnyMethod()
					//.AllowAnyOrigin()
					.SetIsOriginAllowed((host) => true)
					.AllowCredentials();
			}));
		services.ConfigAppDirectory();
		services.AddAppInfoProvider();
		services.AddPrinters();
		services.AddPrinterService();
		services.AddSingleton<IReportBuffer, ReportBuffer>();
		services.AddSingleton<IAppConfigService>(sp => AppConfigService.GetInstance(Configuration));
		services.AddSingleton<IDbSchemaCache, DbSchemaCache>();
		services.AddSingleton<IConfigurationCacheService, ConfigurationCacheService>();
		services.AddScoped<IConfigurationService, ConfigurationService>();
		services.AddSingleton<AppClientTracker>();
		services.AddControllersWithViews();
		services.AddDbContext<DocGeneral>();
		services.AddSingleton<IDocModuleLoader, CachedDocModuleLoader>();

		// Status Bar сервисы
		services.AddSignalR();
		services.AddMemoryCache();

		// HTTP клиенты для проверки внешних сервисов
		services.AddHttpClient("MessagingService", client =>
		{
			client.BaseAddress = new System.Uri("http://localhost:5010");
			client.Timeout = System.TimeSpan.FromSeconds(2);
			client.DefaultRequestHeaders.Add("User-Agent", "TN_Doc-StatusChecker/1.4.2");
		});

		services.AddHttpClient("Elis", client =>
		{
			client.Timeout = System.TimeSpan.FromSeconds(5);
			client.DefaultRequestHeaders.Add("User-Agent", "TN_Doc-StatusChecker/1.4.2");
		});

		services.AddScoped<IStatusProvider, StatusProvider>();
		services.AddHostedService<StatusMonitoringService>();
	}

	// This method gets called by the runtime. Use this method to configure
	// the HTTP request pipeline.
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		// Регистрируем обработчик инвалидации кэша для CfgFileRW
		var configCache = app.ApplicationServices.GetService<IConfigurationCacheService>();
		if (configCache != null)
		{
			CfgFileRW.RegisterCacheInvalidator(filePath => configCache.ClearCache(filePath));
		}

		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.UseExceptionHandler("/Home/Error");
			app.UseHsts();
		}

		// Интерцептор для /PDF/PDF.pdf до статических файлов, чтобы отдавать PDF из памяти
		app.Use(async (context, next) =>
		{
			if (context.Request.Path.Value?.Equals("/PDF/PDF.pdf", System.StringComparison.OrdinalIgnoreCase) == true)
			{
				var buffer = context.RequestServices.GetService(typeof(IReportBuffer)) as IReportBuffer;
				var bytes = buffer?.GetPdfBytes();
				if (bytes != null && bytes.Length > 0)
				{
					context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
					context.Response.Headers["Pragma"] = "no-cache";
					context.Response.Headers["Expires"] = "0";
					context.Response.ContentType = "application/pdf";
					await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
					return;
				}
			}
			await next();
		});

		app.UseStaticFiles();
		app.UseRouting();
		app.UseCors("CorsPolicy");
		app.UseMiddleware<AppClientTrackingMiddleware>();
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
			endpoints.MapHub<TN_Doc.Hubs.StatusHub>("/statusHub");
			endpoints.MapFallbackToFile("/document-editor/{*path}", "document-editor/index.html");
		});
	}
}