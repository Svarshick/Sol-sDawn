using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SolsDawn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DesignerBoundaryAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor UsingDirective = new(
        "SD0001",
        "Using directive in designer code",
        "Designer code must not declare using directives; the allowed imports are the <Using> items of the project file",
        "DesignerBoundary",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForbiddenSymbol = new(
        "SD0002",
        "Symbol outside the designer API",
        "'{0}' is not part of the designer API; use GameplayAPI",
        "DesignerBoundary",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UsingDirective, ForbiddenSymbol);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var allowlist = Allowlist.From(start.Compilation);
            start.RegisterSyntaxNodeAction(
                c => c.ReportDiagnostic(Diagnostic.Create(UsingDirective, c.Node.GetLocation())),
                SyntaxKind.UsingDirective);
            start.RegisterSyntaxNodeAction(
                c => CheckName(c, allowlist),
                SyntaxKind.IdentifierName,
                SyntaxKind.GenericName);
        });
    }

    private static void CheckName(SyntaxNodeAnalysisContext context, Allowlist allowlist)
    {
        var name = (SimpleNameSyntax)context.Node;
        if (name is IdentifierNameSyntax { IsVar: true })
            return;
        if (name.Ancestors().Any(a => a is UsingDirectiveSyntax))
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
        if (symbol is null || allowlist.Allows(symbol))
            return;

        if (name.Parent is MemberAccessExpressionSyntax access && access.Name == name &&
            context.SemanticModel.GetSymbolInfo(access.Expression, context.CancellationToken).Symbol is ITypeSymbol owner &&
            !allowlist.Allows(owner))
            return;

        context.ReportDiagnostic(Diagnostic.Create(ForbiddenSymbol, name.GetLocation(), symbol.ToDisplayString()));
    }

    private sealed class Allowlist
    {
        private readonly IAssemblySymbol _own;
        private readonly HashSet<INamespaceSymbol> _namespaces = new(SymbolEqualityComparer.Default);
        private readonly HashSet<ITypeSymbol> _types = new(SymbolEqualityComparer.Default);

        private Allowlist(IAssemblySymbol own) => _own = own;

        public static Allowlist From(Compilation compilation)
        {
            var allowlist = new Allowlist(compilation.Assembly);
            foreach (var tree in compilation.SyntaxTrees)
            {
                if (!tree.FilePath.EndsWith(".GlobalUsings.g.cs"))
                    continue;

                var model = compilation.GetSemanticModel(tree);
                foreach (var directive in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
                {
                    if (directive.Alias is not null)
                    {
                        if (model.GetDeclaredSymbol(directive) is IAliasSymbol { Target: ITypeSymbol aliased })
                            allowlist._types.Add(aliased.OriginalDefinition);
                        continue;
                    }

                    switch (model.GetSymbolInfo(directive.NamespaceOrType).Symbol)
                    {
                        case INamespaceSymbol ns:
                            allowlist._namespaces.Add(ns);
                            break;
                        case ITypeSymbol type:
                            allowlist._types.Add(type.OriginalDefinition);
                            break;
                    }
                }
            }

            return allowlist;
        }

        public bool Allows(ISymbol symbol) => symbol switch
        {
            INamespaceSymbol => true,
            ITypeParameterSymbol => true,
            ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or ILabelSymbol or IDiscardSymbol => true,
            ITypeSymbol type => AllowsType(type),
            IMethodSymbol { ReducedFrom: { } extension } => AllowsType(extension.ContainingType),
            _ => symbol.ContainingType is null || AllowsType(symbol.ContainingType),
        };

        private bool AllowsType(ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    return AllowsType(array.ElementType);
                case IPointerTypeSymbol:
                case IFunctionPointerTypeSymbol:
                    return false;
                case ITypeParameterSymbol:
                case IDynamicTypeSymbol:
                case IErrorTypeSymbol:
                    return true;
            }

            if (type.SpecialType != SpecialType.None || type.IsTupleType)
                return true;

            var definition = type.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(definition.ContainingAssembly, _own))
                return true;
            if (_types.Contains(definition))
                return true;
            if (definition.ContainingType is { } outer)
                return AllowsType(outer);
            return definition.ContainingNamespace is { } ns && _namespaces.Contains(ns);
        }
    }
}
