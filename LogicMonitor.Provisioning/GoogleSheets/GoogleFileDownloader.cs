using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace LogicMonitor.Provisioning.GoogleSheets
{
	public class GoogleFileDownloader : IDisposable
	{
		private DriveService? service;
		private bool disposedValue;
		private readonly string _fileId;
		private readonly string _sheetName;

		public GoogleFileDownloader(string? evaluatedConfig)
		{
			if (string.IsNullOrWhiteSpace(evaluatedConfig))
			{
				throw new ConfigurationException("Repetition config for file should be in the form 'fileId|Sheetname'.");
			}

			var configDetails = evaluatedConfig.Split('|') ?? throw new InvalidOperationException("Config should have been validated.  Found missing Repetition.Config");
			if (configDetails.Length != 2)
			{
				throw new ConfigurationException("Repetition config for file should be in the form 'fileId|Sheetname'.");
			}

			_fileId = configDetails[0];
			_sheetName = configDetails[1];
		}

		private async Task<DriveService> GetServiceAsync(CancellationToken cancellationToken)
		{
			if (service != null)
			{
				return service;
			}

			UserCredential credential;

			await using var stream = new FileStream("googleSheets.json", FileMode.Open, FileAccess.Read);
			var secrets = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken);

			credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
				secrets.Secrets,
				[DriveService.Scope.Drive],
				"user",
				cancellationToken,
				new FileDataStore("token.json", true)
				);

			// Create Drive API service.
			return service = new DriveService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "LogicMonitor.Provisioning",
			});
		}

		internal async Task<FileAndSheetInfo> DownloadAsync(FileInfo fileInfo, CancellationToken cancellationToken)
		{
			var service = await GetServiceAsync(cancellationToken);
			await using var stream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write);
			await service
				.Files
				.Get(_fileId)
				.DownloadAsync(stream, cancellationToken);

			return new FileAndSheetInfo($"{fileInfo.FullName}|{_sheetName}");
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					service?.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put clean up code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
