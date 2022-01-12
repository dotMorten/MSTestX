using MSTestX;

namespace TestAppRunner.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var preserve = typeof(UnitTests.AttachmentTests).Assembly;

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<RunnerApp>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		return builder.Build();
	}
}
