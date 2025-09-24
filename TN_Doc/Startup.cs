using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using TN_Doc.Extensions;
using TN_Doc.Services;
using TN.Doc;
using TN_DocGeneral.Services;

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
			LogManager.Configuration.Variables["logDirectory"] = LoggingPathService.GetLogDirectory();

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
		#if RELEASE
		services.ConfigAppDirectory();
		#endif
		services.AddAppInfoProvider();
		services.AddPrinters();
		services.AddPrinterService();
		services.AddSingleton<IReportBuffer, ReportBuffer>();
		services.AddSingleton<IAppConfigService>(sp => AppConfigService.GetInstance(Configuration));
		services.AddSingleton<IDbSchemaCache, DbSchemaCache>();
		services.AddMemoryCache(); // Добавляем кэширование в памяти для статусов
		services.AddStatusServices(); // Добавляем сервисы мониторинга статусов
		services.AddControllersWithViews();
		services.AddDbContext<DocGeneral>();
		services.AddSingleton<IDocModuleLoader, DocModuleLoader>();
	}

	// This method gets called by the runtime. Use this method to configure
	// the HTTP request pipeline.
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
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
		app.UseEndpoints(endpoints => { endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); });
	}
}