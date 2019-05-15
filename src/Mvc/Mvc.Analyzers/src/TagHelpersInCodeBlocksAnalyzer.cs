// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TagHelpersInCodeBlocksAnalyzer : ViewFeatureAnalyzerBase
    {
        public TagHelpersInCodeBlocksAnalyzer()
            : base(DiagnosticDescriptors.MVC1006_FunctionsContainingTagHelpersMustBeAsyncAndReturnTask)
        {
        }

        protected override void InitializeWorker(ViewFeaturesAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSyntaxNodeAction(context =>
            {
                var invocationExpression = (InvocationExpressionSyntax)context.Node;
                var symbol = context.SemanticModel.GetSymbolInfo(invocationExpression, context.CancellationToken).Symbol;
                if (symbol == null || symbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                if (!IsCreateTagHelperMethod(method))
                {
                    return;
                }

                var containingFunction = context.Node.FirstAncestorOrSelf<SyntaxNode>(node =>
                    node.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                    node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                    node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                    node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                    node.IsKind(SyntaxKind.MethodDeclaration));

                //Debugger.Launch();
                switch (containingFunction)
                {
                    case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                        var lambdaSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(parenthesizedLambda).Symbol;
                        if (!lambdaSymbol.IsAsync ||
                            !analyzerContext.TaskType.IsAssignableFrom(lambdaSymbol.ReturnType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                SupportedDiagnostic,
                                parenthesizedLambda.ParameterList.GetLocation(),
                                new[] { "lambda" }));
                        }

                        break;
                    case SimpleLambdaExpressionSyntax simpleLambda:
                        var simpleLambdaSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(simpleLambda).Symbol;
                        if (!simpleLambdaSymbol.IsAsync ||
                            !analyzerContext.TaskType.IsAssignableFrom(simpleLambdaSymbol.ReturnType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                SupportedDiagnostic,
                                simpleLambda.Parameter.GetLocation(),
                                new[] { "lambda" }));
                        }

                        break;
                    case AnonymousMethodExpressionSyntax anonymousMethod:
                        var anonymousMethodSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(anonymousMethod).Symbol;
                        if (!anonymousMethodSymbol.IsAsync ||
                            !analyzerContext.TaskType.IsAssignableFrom(anonymousMethodSymbol.ReturnType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                SupportedDiagnostic,
                                anonymousMethod.DelegateKeyword.GetLocation(),
                                new[] { "method" }));
                        }

                        break;
                    case LocalFunctionStatementSyntax localFunction:
                        var localFunctionReturnType = (INamedTypeSymbol)context.SemanticModel.GetSymbolInfo(localFunction.ReturnType).Symbol;
                        if (!analyzerContext.TaskType.IsAssignableFrom(localFunctionReturnType) ||
                            localFunction.Modifiers.IndexOf(SyntaxKind.AsyncKeyword) == -1)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                SupportedDiagnostic,
                                localFunction.Identifier.GetLocation(),
                                new[] { "local function" }));
                        }
                        break;
                    case MethodDeclarationSyntax methodDeclaration:
                        var methodDeclarationReturnType = (INamedTypeSymbol)context.SemanticModel.GetSymbolInfo(methodDeclaration.ReturnType).Symbol;
                        if (!analyzerContext.TaskType.IsAssignableFrom(methodDeclarationReturnType) ||
                            methodDeclaration.Modifiers.IndexOf(SyntaxKind.AsyncKeyword) == -1)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                SupportedDiagnostic,
                                methodDeclaration.Identifier.GetLocation(),
                                new[] { "method" }));
                        }
                        break;
                }

            }, SyntaxKind.InvocationExpression);
        }

        private bool IsCreateTagHelperMethod(IMethodSymbol method)
        {
            if (!method.IsGenericMethod)
            {
                return false;
            }

            if (!string.Equals(SymbolNames.CreateTagHelperMethod, method.Name, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        internal readonly struct SymbolCache
        {
            public SymbolCache(Compilation compilation)
            {
                Task = compilation.GetTypeByMetadataName(SymbolNames.TaskTypeName);
            }

            public INamedTypeSymbol Task { get; }
        }
    }
}
