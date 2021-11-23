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
					config.AddJsonFile("appsettings.json", optional: true);
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
				.ConfigureLogging((hostContext, loggingBuilder) =>
					loggingBuilder
					.ClearProviders()
					.AddConfiguration(hostContext.Configuration.GetSection("Logging"))
					.AddConsole()
				);

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
