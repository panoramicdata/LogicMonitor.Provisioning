namespace LogicMonitor.Provisioning.Extensions;
internal static class ConsoleExtensions
{
	internal static async Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken cancellationToken)
	{
		while (!Console.KeyAvailable)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(50, cancellationToken);
		}

		return Console.ReadKey(true);
	}
}
