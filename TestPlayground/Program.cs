using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace TestPlayground
{
    public static class EmitDemo
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Use roslyn? (y/n)");

            string line = Console.ReadLine().Trim();

            if (line == "y")
            {
                UseRoslyn();
            }
            else
            {
                TestClass.Run();
            }
        }

        private static void UseRoslyn()
        {
            string filename = Path.GetFullPath(@"..\..\..\Code.cs");

            Console.WriteLine(filename);

            var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.OpenRead(filename)), path: filename);

            string dllFileName = "generated.dll";

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.Location != string.Empty)
                .Select(a => MetadataReference.CreateFromFile(a.Location)); ;

            var compilation = CSharpCompilation.Create(dllFileName)
              .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
              .AddReferences(references)
              .AddSyntaxTrees(tree);

            string path = Path.Combine(Directory.GetCurrentDirectory(), dllFileName);
            EmitResult compilationResult = compilation.Emit(path);

            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            asm.GetType("TestPlayground.TestClass").GetMethod("Run").Invoke(null, new object[] { });
        }
    }
}