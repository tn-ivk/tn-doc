using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using TN_Doc.Extensions;
using TN_Doc.Models.Services;
using TN_DocGeneral.Services;
using TN.Doc;

namespace TN_Doc
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
			services.AddDirectoryService(Configuration);
			services.AddPrinters();
			services.AddPrinterService();
			services.AddControllersWithViews();
			services.AddDbContext<DocGeneral>();
			services.AddLogging(builder =>
			{
				var logLevel = Configuration.GetValue<string>("Logging:LogLevel:Default");
				LogManager.Configuration.Variables["logLevel"] = logLevel;
				builder.ClearProviders();
				builder.AddNLog();
			});
			
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

			app.UseStaticFiles();
			app.UseRouting();

			app.UseCors("CorsPolicy");

			app.UseEndpoints(endpoints => { endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); });
		}
	}
}