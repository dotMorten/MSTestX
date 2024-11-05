# MSTestX

A cross-platform .NET Standard compilation of the MS Test Adapter, a .NET MAUI Unit Test Runner, and a console app to automate testing on devices.

This isn't "just another test framework". This is all based on the Microsoft MSTest Framework, so that your unit tests will run and behave the exact same way as your .NETFramework and UWP Unit Tests. I was fed up with having to deal with different test frameworks all doing things slightly different, and spending too much time abstracting those differences away, and dealing with different report formats.

### Features

- Uses the same asserts and test attributes from [MSTest.Framework NuGet package](https://www.nuget.org/packages/MSTest.TestFramework/)
- Supports automation from commandline
- Supports generating a TRX Report identical to those `VSTest.Console.exe` generates for easier integration into existing reporting systems.

## Usage

1. Inside your solution, create a new blank .NET MAUI Project.
2. Add "MSTestX.UnitTestRunner" NuGet package.
3. Delete `AppShell.xaml`, `MainPage.xaml` `App.xaml` and their code-behind files.
4. In `MauiProgram.cs` delete `.UseMauiApp<App>()` with `.UseTestApp(config => { /*your code to configure the TestOptions})`, as the following content:

```cs
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiCommunityToolkit() // If you don't want to init here, you can disable the MCT analyzer.
			.UseTestApp(config =>
			{
				config.TestAssemblies = [typeof(MauiProgram).Assembly];
				return config;
			})
			// rest of builder configuration


		return builder.Build();
	}
```

5. Add a unit test class with the following content:

```cs
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyUnitTestApp
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        [TestCategory("Simple Tests")]
        public void MyTest()
        {
            Assert.IsTrue(true);
        }
    }
}
```

### Notes
- Tests in other referenced projects in the same solution will be found as well. This is useful if you want to also run the tests outside .NET MAUI in a normal unit test project for instance.
- if you put your tests in a class library, the iOS app, will need to reference one of the types in the AppDelegate, or the compiler will strip out the unit test DLL (this isn't an issue if you use a shared project with tests).
- This is not a fork of MSTest. The submodules literally uses the code as-is from TestFX but compiled so it can run and be referenced by a .NET MAUI app.

## Automation
`MSTestX.Console` is a dotnet tool that helps with deploying and running monitoring the unit test application, while outputting a TRX test report to the host machine.

### Android (Windows and MacOS)
Deploy and run the application:
```
dotnet build myproject.csproj -f net8.0-android
dotnet tool install --global MSTestX.Console --version 0.36.0
MSTestX.Console -apkpath path-to-app-signed.apk
```
or connect and launch an already installed app
```
MSTestX.Console -apkid [package id] -activity [activity name]
```

### iOS (MacOS only)
```
dotnet build myproject.csproj -f net8.0-ios -r ios-arm64
dotnet tool install --global MSTestX.Console --version 0.36.0
MSTestX.Console -apppath [path-to-generated .app application] 
```

### Mac-Catalyst (MacOS only)
With MacCatalyst you simply launch the app and connect to local-host using the `-remoteIp` parameter pointing to localhost, which will also work with any remote device running the unit test app.
```
dotnet build myproject.csproj -f net8.0-maccatalyst -r maccatalyst-arm64
dotnet tool install --global MSTestX.Console --version 0.36.0
open [path-to-generated .app application]
MSTestX.Console -remoteIp 127.0.0.1:38300
```
### Other parameters
 - `-logFileName <path to file>` : The path of the TRX file that gets generated (defaults to current date/time).
 - `-settings <path to file>` : Path to an XML runsettings file. See [Configure unit tests by using a .runsettings file](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2022) for details.
 - `-deviceid <Android Device Serial Number>`    Android: If more than one device is connected, specifies which device to use
 - `-device <uuid|ecid|serial_number|udid|name|dns_name>`   iOS: The identifier, ECID, serial number, UDID, user-provided name, or DNS name of the device, if more than one device is connected.

run `MSTestX.Console` to get a list of all parameters.

### Screenshots

![image](https://user-images.githubusercontent.com/1378165/43662635-757007ee-971b-11e8-9b10-63c1d2983385.png)

![image](https://user-images.githubusercontent.com/1378165/43662619-65fa0a4e-971b-11e8-9059-51c86522103d.png)

![image](https://user-images.githubusercontent.com/1378165/43662682-9514fbb8-971b-11e8-9c67-a46ff7290e0d.png)
