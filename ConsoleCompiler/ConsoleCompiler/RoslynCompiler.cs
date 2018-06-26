/*************************************************************************************
对Roslyn编译器进行简单封装, 提供一个按照文件进行编译的接口
*************************************************************************************/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Reflection;
using ConsoleCompiler.AssemblyLoader;
using System;

namespace ConsoleCompiler
{
    /// <summary>
    /// Roslyn compiler service<br/>
    /// 基于Roslyn的编译服务<br/>
    /// </summary>
    internal class RoslynCompilerService
    {
        /// <summary>
        /// Target platform name, net or netstandard<br/>
        /// 目标平台的名称, net或netstandard<br/>
        /// </summary>
		public string TargetPlatform { get { return "netstandard"; } }

        /// <summary>
        /// Loaded namespaces, for reducing load time<br/>
        /// 已加载的命名空间, 用于减少加载时间<br/>
        /// </summary>
        protected HashSet<string> LoadedNamespaces { get; set; }

        /// <summary>
        /// Initialize<br/>
        /// 初始化<br/>
        /// </summary>
        public RoslynCompilerService()
        {
            LoadedNamespaces = new HashSet<string>();
        }

        /// <summary>
        /// Find all using directive<br/>
        /// And try to load the namespace as assembly<br/>
        /// 寻找源代码中的所有using指令<br/>
        /// 并尝试加载命名空间对应的程序集<br/>
        /// </summary>
        /// <param name="syntaxTrees">Syntax trees</param>
        protected void LoadAssembliesFromUsings(List<SyntaxTree> syntaxTrees)
        {
            // Find all using directive
            var assemblyLoader = new NetAssemblyLoader();
            foreach (var tree in syntaxTrees)
            {
                foreach (var usingSyntax in ((CompilationUnitSyntax)tree.GetRoot()).Usings)
                {
                    var name = usingSyntax.Name;
                    var names = new List<string>();
                    while (name != null)
                    {
                        // The type is "IdentifierNameSyntax" if it's single identifier
                        // eg: System
                        // The type is "QualifiedNameSyntax" if it's contains more than one identifier
                        // eg: System.Threading
                        if (name is QualifiedNameSyntax qualifiedName)
                        {
                            var identifierName = (IdentifierNameSyntax)qualifiedName.Right;
                            names.Add(identifierName.Identifier.Text);
                            name = qualifiedName.Left;
                        }
                        else if (name is IdentifierNameSyntax identifierName)
                        {
                            names.Add(identifierName.Identifier.Text);
                            name = null;
                        }
                    }
                    if (names.Contains("src"))
                    {
                        // Ignore if it looks like a namespace from plugin 
                        continue;
                    }
                    names.Reverse();
                    for (int c = 1; c <= names.Count; ++c)
                    {
                        // Try to load the namespace as assembly
                        // eg: will try "System" and "System.Threading" from "System.Threading"
                        var usingName = string.Join(".", names.Take(c));
                        if (LoadedNamespaces.Contains(usingName))
                        {
                            continue;
                        }
                        try
                        {
                            assemblyLoader.Load(usingName);
                        }
                        catch
                        {
                            // Retry next name
                        }
                        LoadedNamespaces.Add(usingName);
                    }
                }
            }
        }

        /// <summary>
        /// Compile source files to assembly<br/>
        /// 编译源代码到程序集<br/>
        /// </summary>
        public void Compile(IList<string> sourceFiles,
            string assemblyName, string assemblyPath)
        {
            // Parse source files into syntax trees
            // Also define NETCORE for .Net Core
            var parseOptions = CSharpParseOptions.Default;
			parseOptions = parseOptions.WithPreprocessorSymbols("NETCORE");
            var syntaxTrees = sourceFiles
                .Select(path => CSharpSyntaxTree.ParseText(
                    File.ReadAllText(path), parseOptions, path, Encoding.UTF8))
                .ToList();

            LoadAssembliesFromUsings(syntaxTrees);
            // Add loaded assemblies to compile references
            var assemblyLoader = new NetAssemblyLoader();
            var references = assemblyLoader.GetLoadedAssemblies()
                .Select(assembly => assembly.Location)
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToList();

            var optimizationLevel = OptimizationLevel.Release;
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: optimizationLevel);

            // Compile to assembly, throw exception if error occurred
            Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(compilationOptions)
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTrees);
            Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(assemblyPath);
            if (!emitResult.Success)
            {
                throw new Exception(string.Join("\r\n",
                    emitResult.Diagnostics.Where(d => d.WarningLevel == 0)));
            }
        }
    }
}
