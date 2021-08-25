using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;
using MSTestX;
[assembly: XamlCompilationAttribute(XamlCompilationOptions.Compile)]

namespace TestAppRunner.Maui
{
	public class Startup : IStartup
	{
		public void Configure(IAppHostBuilder appBuilder)
		{
			appBuilder
				.UseMauiApp<RunnerApp>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});
			var preserve = typeof(UnitTests.AttachmentTests).Assembly;
		}
	}
}