using Serilog;

namespace LogicMonitor.Provisioning;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		try
		{
			var builder = new HostBuilder()
				.ConfigureAppConfiguration(config =>
				{
					var filePath = args.Length == 0 ? "appsettings.jsonc" : args[0];
					var fileInfo = new FileInfo(filePath);
					if (!fileInfo.Exists)
					{
						throw new FileNotFoundException($"File not found: {filePath}");
					}

					config.AddJsonFile(fileInfo.FullName, optional: false);
					config.AddEnvironmentVariables();

					if (args != null)
					{
						config.AddCommandLine(args);
					}
				})
				.ConfigureServices((hostContext, services) =>
				{
					services.AddOptions();
					services.Configure<Configuration>(hostContext.Configuration.GetSection("Configuration"));

					services.AddSingleton<IHostedService, Application>();
				})
				.UseSerilog((hostContext, loggerConfiguration) =>
				{
					loggerConfiguration
					.ReadFrom.Configuration(hostContext.Configuration)
					.Enrich.FromLogContext()
					.WriteTo.Console();
				})
				.UseConsoleLifetime()
					;

			await builder
			 .RunConsoleAsync()
			 .ConfigureAwait(false);
			return 0;
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			return 1;
		}
	}
}
