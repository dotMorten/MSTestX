# MSTestX

A cross-platform .NET Standard compilation of the MS Test Adapter, and a Xamarin Test Runner.

This isn't "just another test framework". This is all based on the Microsoft MSTest Framework, so that your unit tests will run and behave the exact same way as your .NETFramework and UWP Unit Tests. I was fed up with having to deal with different test frameworks all doing things slightly different, and spending too much time abstracting those differences away, and dealing with different report formats.

### Features

- Uses the same asserts and test attributes from [MSTest.Framework NuGet package](https://www.nuget.org/packages/MSTest.TestFramework/)
- Supports automation from commandline
- Supports generating a TRX Report identical to those `VSTest.Console.exe` generates for easier integration into existing reporting systems.




Note: This is not a fork. The submodule literally uses the code as-is from TestFX but compiled so it can run and be referenced by a Xamarin App.
