using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
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

            var assembly = CreateAssembly(File.ReadAllText(filename));


            assembly.GetType("TestPlayground.TestClass").GetMethod("Run").Invoke(null, new object[] { });



            /*       

            Encoding encoding = Encoding.UTF8;

            var buffer = encoding.GetBytes(File.ReadAllText(filename));

            SourceText sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var tree = CSharpSyntaxTree.ParseText(sourceText, new CSharpParseOptions(), path: filename);

            var syntaxRootNode = tree.GetRoot() as CSharpSyntaxNode;
            var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, filename, encoding);


            string dllFileName = "generated.dll";
            var pdbFileName = Path.ChangeExtension(dllFileName, "pdb");

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.Location != string.Empty)
                .Select(a => MetadataReference.CreateFromFile(a.Location)); 

            var compilation = CSharpCompilation.Create(dllFileName)
              .WithOptions(
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                  .WithOptimizationLevel(OptimizationLevel.Debug)
                  .WithPlatform(Platform.AnyCpu))
              .AddReferences(references)
              .AddSyntaxTrees(tree);

            var emitOptions = new EmitOptions(
               debugInformationFormat: DebugInformationFormat.PortablePdb,
               pdbFilePath: pdbFileName
              // runtimeMetadataVersion: "1.0"
              );

            var embeddedTexts = new List<EmbeddedText>
            {
                EmbeddedText.FromSource(filename, sourceText),
            };

            string path = Path.Combine(Directory.GetCurrentDirectory(), dllFileName);
            string pdbPath = Path.Combine(Directory.GetCurrentDirectory(), pdbFileName);

            using (Stream fsDll = File.OpenWrite(path))
            using (Stream fsPdb = File.OpenWrite(pdbPath))
            {
                EmitResult compilationResult = compilation.Emit(
                    options: emitOptions,
                    peStream: fsDll,
                    pdbStream: fsPdb,
                    embeddedTexts: embeddedTexts
                    );
            }


            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            asm.GetType("TestPlayground.TestClass").GetMethod("Run").Invoke(null, new object[] { });
            */
        }


        //https://stackoverflow.com/questions/50649795/how-to-debug-dll-generated-from-roslyn-compilation

        public static Assembly CreateAssembly(string code)
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
              .Where(a => !a.IsDynamic && a.Location != string.Empty)
              .Select(a => MetadataReference.CreateFromFile(a.Location));


            var encoding = Encoding.UTF8;

            var assemblyName = Path.GetRandomFileName();
            var symbolsName = Path.ChangeExtension(assemblyName, "pdb");
            var sourceCodePath = "generated.cs";

            var buffer = encoding.GetBytes(code);
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                new CSharpParseOptions(),
                path: sourceCodePath);

            var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
            var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, sourceCodePath, encoding);

            var optimizationLevel = OptimizationLevel.Debug;

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { encoded },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(optimizationLevel)
                    .WithPlatform(Platform.AnyCpu)
            );

            using (var assemblyStream = new MemoryStream())
            using (var symbolsStream = new MemoryStream())
            {
                var emitOptions = new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: symbolsName);

                var embeddedTexts = new List<EmbeddedText>
                {
                    EmbeddedText.FromSource(sourceCodePath, sourceText),
                };

                EmitResult result = compilation.Emit(
                    peStream: assemblyStream,
                    pdbStream: symbolsStream,
                    embeddedTexts: embeddedTexts,
                    options: emitOptions);

                if (!result.Success)
                {
                    var errors = new List<string>();

                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                        errors.Add($"{diagnostic.Id}: {diagnostic.GetMessage()}");

                    throw new Exception(String.Join("\n", errors));
                }

               // Console.WriteLine(code);

                assemblyStream.Seek(0, SeekOrigin.Begin);
                symbolsStream?.Seek(0, SeekOrigin.Begin);

                var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream, symbolsStream);
                return assembly;
            }
        }




    }
}