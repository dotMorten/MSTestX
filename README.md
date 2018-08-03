# MSTestX

A cross-platform .NET Standard compilation of the MS Test Adapter, and a Xamarin Test Runner.

This isn't "just another test framework". This is all based on the Microsoft MSTest Framework, so that your unit tests will run and behave the exact same way as your .NETFramework and UWP Unit Tests. I was fed up with having to deal with different test frameworks all doing things slightly different, and spending too much time abstracting those differences away, and dealing with different report formats.

### Features

- Uses the same asserts and test attributes from [MSTest.Framework NuGet package](https://www.nuget.org/packages/MSTest.TestFramework/)
- Supports automation from commandline
- Supports generating a TRX Report identical to those `VSTest.Console.exe` generates for easier integration into existing reporting systems.

## Usage

1. Create a new blank Xamarin.Forms Project targeting iOS and Android (shared or .NET Standard)
2. Add a Nuget reference to [`MSTestX.UnitTestRunner`](https://www.nuget.org/packages/MSTestX.UnitTestRunner)
3. Delete `MainPage.xaml` and `App.xaml`
4. In Android's MainActivity.cs file change the `LoadApplication(new App())` call to `LoadApplication(new TestAppRunner.App());`
5. In iOS' AppDelegate.cs file, make the same change to the `LoadApplication` call
6. In the common project add a unit test class with the following content:

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

Note if you put your tests in a class library, the iOS app, will need to reference one of the types in the AppDelegate, or the compiler will strip out the unit test DLL (this isn't an issue if you use a shared project with tests).

Note: This is not a fork. The submodule literally uses the code as-is from TestFX but compiled so it can run and be referenced by a Xamarin App.


### Screenshots

![image](https://user-images.githubusercontent.com/1378165/43662635-757007ee-971b-11e8-9b10-63c1d2983385.png)

![image](https://user-images.githubusercontent.com/1378165/43662619-65fa0a4e-971b-11e8-9059-51c86522103d.png)

![image](https://user-images.githubusercontent.com/1378165/43662682-9514fbb8-971b-11e8-9c67-a46ff7290e0d.png)
