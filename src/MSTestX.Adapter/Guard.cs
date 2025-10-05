using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter
{
    internal static class Guard
    {
        public static void NotNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(argument);
        }

        internal static void NotNullOrWhiteSpace(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) { throw new ArgumentNullException(nameof(value)); }
        }
    }
}
