using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace BindingExpression.Analyzers.Utils
{
    public static class RoslynAnalyzerExtensions
    {
        public static async Task<ImmutableArray<Diagnostic>> ApplyAnalyzer(this Project project, DiagnosticAnalyzer analyzer)
        {
            var compilation = await project.GetCompilationAsync();
            var newCompilation = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = await newCompilation.GetAllDiagnosticsAsync();

            return diagnostics;
        }

        public static async Task<Project> ApplyCodeFix(this Project project, Diagnostic diagnostic, CodeFixProvider fix)
        {
            var document = project.Solution.GetDocument(diagnostic.Location.SourceTree);
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);

            await fix.RegisterCodeFixesAsync(context);

            var operations = await actions.First().GetOperationsAsync(CancellationToken.None);
            var changeSolution = operations.OfType<ApplyChangesOperation>().First().ChangedSolution;
            var newProject = changeSolution.Projects.First();

            return newProject;
        }

        public static async Task<Project> ApplyAnalyzerWithCodeFix(this Project project, DiagnosticAnalyzer analyzer, CodeFixProvider fix)
        {
            var diagnostics = await project.ApplyAnalyzer(analyzer);
            var diagnosticsToFix = diagnostics.Where(o => fix.FixableDiagnosticIds.Contains(o.Id));
            foreach (var diagnostic in diagnosticsToFix)
            {
                project = await project.ApplyCodeFix(diagnostic, fix);
            }

            return project;
        }

        public static async Task<Diagnostic> GetErrorOrNull(this Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var diagnosticsAfterCompilation = compilation.GetDiagnostics();

            var error = diagnosticsAfterCompilation.FirstOrDefault(o => o.Severity == DiagnosticSeverity.Error);

            return error;
        }

        public static void SetValueToPropertyViaReflection(this object @object, string propertyName, object value)
        {
            var property = @object.GetType().GetProperty(propertyName);
            property.SetValue(@object, value);
        }

        public static TResult CallMethodViaReflection<TResult>(this object @object, string methodName, params object[] args)
        {
            var types = args.Select(o => o.GetType()).ToArray();
            var method = @object.GetType()
                .GetMethods()
                .First(o => o.Name == methodName && 
                       o.GetParameters()
                           .Select(oo => oo.ParameterType.GUID)
                           .SequenceEqual(types.Select(ooo => ooo.GUID)));
            
            return (TResult)method.Invoke(@object, args);
        }

        public static TResult CallGenericMethodViaReflection<TResult>(this object @object, string methodName, Type[] generecArgs, params object[] args)
        {
            var method = @object
                .GetType()
                .GetMethods()
                .First(o => o.Name == methodName && o.IsGenericMethod)
                .MakeGenericMethod(generecArgs);

            return (TResult)method.Invoke(@object, args);
        }

        public static async Task<Assembly> CompileToRealAssembly(this Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var error = compilation.GetDiagnostics().FirstOrDefault(o => o.Severity == DiagnosticSeverity.Error);
            if (error != null)
            {
                throw new Exception(error.GetMessage());
            }

            using (var memoryStream = new MemoryStream())
            {
                compilation.Emit(memoryStream);
                var bytes = memoryStream.ToArray();
                var assembly = Assembly.Load(bytes);

                return assembly;
            }
        }

        public static Project ReplaceDocument(this Project project, string documentName, string sourceCode)
        {
            return project.Documents
                .First(o => o.Name == documentName)
                .WithText(SourceText.From(sourceCode))
                .Project;
        }

        public static async Task<Project> ReplacePartOfDocumentAsync(this Project project, string documentName, string textToReplace, string newText)
        {
            var document = project.Documents.First(o => o.Name == documentName);
            var text = await document.GetTextAsync();
            return document
                .WithText(SourceText.From(text.ToString().Replace(textToReplace, newText)))
                .Project;
        }
    }
}
