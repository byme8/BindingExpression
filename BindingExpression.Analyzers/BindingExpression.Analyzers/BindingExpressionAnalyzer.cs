using System;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace BindingExpression.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BindingExpressionAnalyzer : DiagnosticAnalyzer

    {
        public const string BindingExpressionAnalyzerDescriptionId = nameof(BindingExpressionAnalyzerDescriptionId);

        public static readonly DiagnosticDescriptor BindingExpressionAnalyzerDescription
            = new DiagnosticDescriptor(
                BindingExpressionAnalyzerDescriptionId,
                "Binding expression doesn't allow such expressions",
                "Binding expression doesn't allow such expressions",
                "BindingExpression",
                DiagnosticSeverity.Error,
                true);


        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                                   GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterOperationAction(o => Execute(o), OperationKind.Invocation);
        }

        private void Execute(OperationAnalysisContext context)
        {
            if (context.Operation is IInvocationOperation invocation)
            {
                var bindingExpressionAttribute =
                    context.Compilation.GetTypeByMetadataName("BindingExpression.BindingExpressionAttribute");

                var methodWithBindingExpressions = invocation.TargetMethod.Parameters
                    .Any(o =>
                        o.GetAttributes()
                            .Any(oo => oo?.AttributeClass?.Equals(bindingExpressionAttribute) ?? false));

                if (!methodWithBindingExpressions)
                {
                    return;
                }


                foreach (var argument in invocation.Arguments)
                {
                    var parameter = argument.Parameter;
                    if (!parameter
                        .GetAttributes()
                        .Any(o => o?.AttributeClass?.Equals(bindingExpressionAttribute) ?? false))
                    {
                        continue;
                    }

                    if (argument.Syntax is ArgumentSyntax argumentSyntax &&
                        argumentSyntax.Expression is ParenthesizedLambdaExpressionSyntax lambda)
                    {
                        switch (lambda.ExpressionBody)
                        {
                            case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
                                continue;
                            default:
                                context.ReportDiagnostic(
                                    Diagnostic.Create(BindingExpressionAnalyzerDescription,
                                        argumentSyntax.GetLocation()));
                                break;
                        }
                    }
                }
            }
        }


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(BindingExpressionAnalyzerDescription);
    }
}