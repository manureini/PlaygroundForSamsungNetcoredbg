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
            UseRoslyn();
        }

        private static void UseRoslyn()
        {
            string filename = Path.GetFullPath(@"..\..\..\..\Code.cs");

            Console.WriteLine(filename);

            Encoding encoding = Encoding.UTF8;

            var buffer = encoding.GetBytes(File.ReadAllText(filename));

            SourceText sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var tree = CSharpSyntaxTree.ParseText(sourceText, new CSharpParseOptions(), path: filename);

            var syntaxRootNode = tree.GetRoot() as CSharpSyntaxNode;
            var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, filename, encoding);


            string dllFileName = "generated.dll";
            var pdbFileName = Path.GetFullPath(Path.ChangeExtension(dllFileName, "pdb"));

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
        }

    }
}