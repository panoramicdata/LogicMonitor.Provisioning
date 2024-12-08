using FluentAssertions;
using LogicMonitor.Provisioning.GoogleSheets;

namespace LogicMonitor.Provisioning.Test;

public class GoogleSheetsTests
{
	[Fact]
	public async Task DownloadSucceeds()
	{
		var googleSheetLoader = new GoogleFileDownloader("1uqwtDMMh1zqTkmJHXf1Y_fS0ZUWr6OHr|Install Order");

		// Get a temporary fileInfo
		var tempFileInfo = new FileInfo(Path.GetTempFileName() + ".xlsx");

		try
		{
			var fileAndSheetInfo = await googleSheetLoader
				.DownloadAsync(tempFileInfo, CancellationToken.None);

			// Check the file exists
			tempFileInfo.Exists.Should().BeTrue();

			// Check the sheet name
			fileAndSheetInfo.SheetName.Should().Be("Install Order");
		}
		finally
		{
			// Clean up
			tempFileInfo.Delete();
		}
	}
}