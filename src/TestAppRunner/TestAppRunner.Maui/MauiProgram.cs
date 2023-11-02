using MSTestX;

namespace TestAppRunner.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var preserve = typeof(UnitTests.AttachmentTests).Assembly;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseTestApp<App>((testOptions) =>
            {
                // configure default timeout
                testOptions.SettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                    "<RunSettings>" +
                    "<!--MSTest adapter-->" +
                    "<MSTestV2><!--If no timeout is specified on a test, use this as default-->" +
                    "<TestTimeout>30000</TestTimeout>" +
                    "</MSTestV2>" +
                    "</RunSettings>";
                return testOptions;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}
