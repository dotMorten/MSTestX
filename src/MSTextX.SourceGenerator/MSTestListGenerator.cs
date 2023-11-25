using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace MSTextX.SourceGenerator
{
    [Generator]
    public class MSTestListGenerator : ISourceGenerator
    {
        private static bool IsAttributeType(INamedTypeSymbol? symbol, INamedTypeSymbol? attributeType)
        {
            if (symbol == null || attributeType == null)
                return false;
            if (SymbolEqualityComparer.Default.Equals(symbol, attributeType))
                return true;
            return IsAttributeType(symbol.BaseType, attributeType);
        }
        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;

            //Debugger.Launch();
            INamedTypeSymbol? testMethodAttributeSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
            INamedTypeSymbol? dataRowAttributeSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute");
            INamedTypeSymbol? testClassAttributeSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute");
            INamedTypeSymbol? categoryAttributeSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.AssemblyInitializeAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.AssemblyCleanupAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ExpectedExceptionAttribute");
            // INamedTypeSymbol? Symbol = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataSourceAttribute");


            StringBuilder sb = new StringBuilder(FileHeader);
            StringBuilder classList = new StringBuilder(@"
        public List<Type> TestClasses { get; } = new List<Type>()
        {
");
            foreach (var symbol in receiver.Methods.GroupBy<IMethodSymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default).OrderBy(m => m.Key.ToDisplayString()))
            {
                var assemblyName = symbol.Key.ContainingAssembly.Name;
                string className = symbol.Key.ToDisplayString();

                if (!symbol.Key.GetAttributes().Any(ad => IsAttributeType(ad.AttributeClass, testClassAttributeSymbol!)))
                    continue;
                foreach (var method in symbol)
                {


                    var location = method.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? method.Name;
                    var locationLine = method.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line ?? 0;
                    string name = method.Name;
                    StringBuilder methodName = new StringBuilder(name);
                    if (method.Parameters.Any())
                    {
                        methodName.Append("(");
                        bool first = true;
                        foreach (var p in method.Parameters)
                        {
                            if (!first)
                                methodName.Append(",");
                            first = false;
                            var typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            typeName = typeName switch
                            {
                                "string" => "System.String",
                                "float" => "System.Single",
                                "double" => "System.Double",
                                "decimal" => "System.Decimal",
                                "bool" => "System.Boolean",
                                "byte" => "System.Byte",
                                "sbyte" => "System.SByte",
                                "short" => "System.Int16",
                                "ushort" => "System.UInt16",
                                "uint" => "System.UInt32",
                                "long" => "System.Int64",
                                "ulong" => "System.UInt64",
                                "char" => "System.Char",
                                "int" => "System.Int32",
                                _ => typeName
                            };
                            methodName.Append(typeName);
                        }
                        methodName.Append(")");
                    }
                    string isAsync = "false";
                    if (method.ReturnType is INamedTypeSymbol returnType && returnType.Name == "Task") // || method.IsAsync)
                    {
                        isAsync = "true";
                    }
                    var categories = method.GetAttributes().Where(a => IsAttributeType(a.AttributeClass, categoryAttributeSymbol)).SelectMany(c => c.ConstructorArguments.Select(c=>c.Value).OfType<string>());
                    var categoriesStr = categories.Any() ? ("new string[] {" + string.Join(",",categories.Select(c=>$"\"{c}\"")) + "}") : "null";
                    var datarows = method.GetAttributes().Where(a => IsAttributeType(a.AttributeClass, dataRowAttributeSymbol));
                    if (datarows.Any())
                    {
                        foreach (var parameter in datarows)
                        {
                            List<string> parameters = new List<string>();
                            foreach (var p in parameter.ConstructorArguments)
                            {
                                if (p.Kind == TypedConstantKind.Array)
                                {
                                    foreach (var v in p.Values)
                                        if (v.IsNull)
                                            parameters.Add("null");
                                        else if (v.Value is string)
                                            parameters.Add($"\"{v.Value}\"");
                                        else
                                            parameters.Add(v.Value!.ToString());
                                }
                                else
                                {
                                    if (p.IsNull)
                                        parameters.Add("null");
                                    else if (p.Value is string)
                                        parameters.Add($"\"{p.Value}\"");
                                    else
                                        parameters.Add(p.Value!.ToString());
                                }
                            }
                            var parameterArguments = string.Join(",", parameters);
                            string invoker = $"({className} instance) => instance.{name}({parameterArguments})";
                            sb.AppendLine($"            Create(\"{assemblyName}\", \"{className}\",\"{methodName}\",\"{name} ({parameterArguments.Replace("\"", "")})\", @\"{location}\",{locationLine}, null, {categoriesStr}, {isAsync}, {invoker}),");
                        }
                    }
                    else
                    {
                        string invoker = $"({className} instance) => instance.{methodName}()";
                        sb.AppendLine($"            Create(\"{assemblyName}\", \"{className}\",\"{methodName}\",\"{name}\", @\"{location}\",{locationLine}, null, {categoriesStr}, {isAsync}, {invoker}),");
                    }
                }
                classList.AppendLine($"            typeof({symbol.Key.ToDisplayString()}),");
            }
            sb.AppendLine("        };");
            classList.AppendLine("        };");
            sb.AppendLine(classList.ToString());
            sb.AppendLine("    }");
            sb.AppendLine("}");
            context.AddSource($"MSTestXTestList.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                INamedTypeSymbol? testMethodAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
                INamedTypeSymbol? dataMethodAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute");

                if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax
                    && methodDeclarationSyntax.AttributeLists.Count > 0)
                {
                    var method = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);

                    if (method != null && (method.GetAttributes().Any(ad => IsAttributeType(ad.AttributeClass, testMethodAttributeSymbol) || IsAttributeType(ad.AttributeClass, dataMethodAttributeSymbol))))
                    {
                        Methods.Add(method);
                    }
                }

            }
        }

        private const string FileHeader = @"
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
namespace MSTestX
{
    public class MSTestXTestList : IMSTestXTestList
    {
        private readonly static Uri _uri = new System.Uri(""executor://mstestadapter/v2"");

        internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
            id: ""TestCase.ManagedType"",
            label: ""ManagedType"",
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));

        internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
            id: ""TestCase.ManagedMethod"",
            label: ""ManagedMethod"",
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));
        internal static readonly TestProperty TestClassNameProperty = TestProperty.Register(""MSTestDiscoverer.TestClassName"", ""ClassName"", typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

        internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
            id: ""TestCase.Hierarchy"",
            label: ""Hierarchy"",
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string[]),
            validateValueCallback: null,
            attributes: TestPropertyAttributes.Immutable,
            owner: typeof(TestCase));

        internal static readonly TestProperty AsyncTestProperty = TestProperty.Register(""MSTestDiscoverer.IsAsync"", ""IsAsync"", typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));
        internal static readonly TestProperty TestCategoryProperty = TestProperty.Register(""MSTestDiscoverer.TestCategory"", ""TestCategory"", typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));

        private static TestCase Create(string assemblyName, string className, string methodName, string name, string codeFilePath, int lineNumber, IEnumerable<KeyValuePair<string,string>> traits, string[] categories, bool isAsync, object extensionData)
        {
            var testMethodName = methodName;
            var idx = testMethodName.IndexOf('(');
            if(idx > 0)
            {
                testMethodName = testMethodName.Substring(0, idx);
            }
            var testcase = new TestCase($""{className}.{testMethodName}"", _uri, assemblyName + "".dll"")
            {
                CodeFilePath = codeFilePath,
                LineNumber = lineNumber,
                DisplayName = name,
                LocalExtensionData = extensionData
            };
            testcase.Id = GuidFromString($""{testcase.ExecutorUri?.ToString()}{codeFilePath}{className}{methodName}{(methodName!=name?name:string.Empty)}"");
            testcase.SetPropertyValue(ManagedTypeProperty, className);
            testcase.SetPropertyValue(ManagedMethodProperty, methodName);
            testcase.SetPropertyValue(TestClassNameProperty, className);
            string[] hierarchy = new string[] { assemblyName }.Concat(className.Split('.')).Concat(new string[] { methodName }).ToArray();
            testcase.SetPropertyValue(HierarchyProperty, hierarchy);
            //testcase.SetPropertyValue(AsyncTestProperty, isAsync);
            if (categories != null)
                testcase.SetPropertyValue(TestCategoryProperty, categories);
             if (traits != null)
                 testcase.Traits.AddRange(traits.Select(t => new Trait(t.Key, t.Value)));
            return testcase;
        }

        private static Guid GuidFromString(string data)
        {
            using var hashAlgorithm = System.Security.Cryptography.SHA1.Create();
            byte[] sourceArray = hashAlgorithm.ComputeHash(System.Text.Encoding.Unicode.GetBytes(data));
            byte[] array = new byte[16];
            Array.Copy(sourceArray, array, 16);
            return new Guid(array);
        }
        public List<TestCase> TestCases { get; } = new List<TestCase>()
        {
";
    }
}
