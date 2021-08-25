#if MAUI
using Microsoft.Maui.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace MSTestX
{
    public class MauiExtensions
    {
        public static IAppHostBuilder UseMSTestX(IAppHostBuilder builder)
        {
            return builder;
            
        }
    }
}
#endif