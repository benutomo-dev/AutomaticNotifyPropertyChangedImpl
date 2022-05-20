using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    [Generator]
    public partial class AutomaticGenerator : ISourceGenerator
    {
        internal const string AttributeDefinedNameSpace = "Benutomo";

        internal const string AutomaticNotifyPropertyChangedImplAttributeCoreName = "AutomaticNotifyPropertyChangedImpl";
        internal const string AutomaticNotifyPropertyChangedImplAttributeName = "AutomaticNotifyPropertyChangedImplAttribute";
        internal const string AutomaticNotifyPropertyChangedImplAttributeFullyQualifiedMetadataName = "Benutomo.AutomaticNotifyPropertyChangedImplAttribute";
        private const string AutomaticNotifyPropertyChangedImplAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class AutomaticNotifyPropertyChangedImplAttribute : Attribute
    {
    }
}
";

        internal const string EnableAutomaticNotifyAttributeName = "EnableAutomaticNotifyAttribute";
        internal const string EnableAutomaticNotifyAttributeFullyQualifiedMetadataName = "Benutomo.EnableAutomaticNotifyAttribute";
        private const string EnableAutomaticNotifyAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class EnableAutomaticNotifyAttribute : Attribute
    {
    }
}
";

        internal const string DisableAutomaticNotifyAttributeName = "DisableAutomaticNotifyAttribute";
        internal const string DisableAutomaticNotifyAttributeFullyQualifiedMetadataName = "Benutomo.DisableAutomaticNotifyAttribute";
        private const string DisableAutomaticNotifyAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class DisableAutomaticNotifyAttribute : Attribute
    {
    }
}
";
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(PostInitialization);
            context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
        }
        void PostInitialization(GeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{AutomaticNotifyPropertyChangedImplAttributeName}.cs", AutomaticNotifyPropertyChangedImplAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{EnableAutomaticNotifyAttributeName}.cs", EnableAutomaticNotifyAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{DisableAutomaticNotifyAttributeName}.cs", DisableAutomaticNotifyAttributeSource);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxContextReciever)
            {
                return;
            }

            var automaticDisposeImplAttributeSymbol = context.Compilation.GetTypeByMetadataName(AutomaticNotifyPropertyChangedImplAttributeFullyQualifiedMetadataName);

            foreach (var anotatedClassDeclaration in syntaxContextReciever.AnotatedClassDeclarations)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!anotatedClassDeclaration.syntaxNode.Modifiers.Any(modifier => modifier.ValueText == "partial"))
                {
                    // AnalyzerでSG0001の報告を実装
                    continue;
                }

                if (!IsAssignableToINotifyPropertyChanged(anotatedClassDeclaration.symbol))
                {
                    // AnalyzerでSG0002の報告を実装
                    continue;
                }

                var automaticDisposeAttributeData = anotatedClassDeclaration.symbol.GetAttributes().SingleOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, automaticDisposeImplAttributeSymbol));
                if (automaticDisposeAttributeData is null)
                {
                    continue;
                }

                var sourceBuilder = new SourceBuilder(context, anotatedClassDeclaration.symbol, automaticDisposeAttributeData);

                sourceBuilder.Build();

                context.AddSource(sourceBuilder.HintName, sourceBuilder.SourceText);
            }
        }



        private static bool IsXSymbolImpl(ITypeSymbol? typeSymbol, string ns1, string typeName)
        {
            Debug.Assert(!ns1.Contains("."));
            Debug.Assert(!typeName.Contains("."));

            if (typeSymbol is null) return false;

            if (typeSymbol.Name != typeName) return false;

            var containingNamespaceSymbol = typeSymbol.ContainingNamespace;

            if (containingNamespaceSymbol is null) return false;

            if (containingNamespaceSymbol.Name != ns1) return false;

            if (containingNamespaceSymbol.ContainingNamespace is null) return false;

            if (!containingNamespaceSymbol.ContainingNamespace.IsGlobalNamespace) return false;

            return true;
        }
        private static bool IsXSymbolImpl(ITypeSymbol? typeSymbol, string ns1, string ns2, string typeName)
        {
            Debug.Assert(!ns1.Contains("."));
            Debug.Assert(!typeName.Contains("."));

            if (typeSymbol is null) return false;

            if (typeSymbol.Name != typeName) return false;

            var containingNamespaceSymbol = typeSymbol.ContainingNamespace;

            if (containingNamespaceSymbol is null) return false;

            if (containingNamespaceSymbol.Name != ns2) return false;

            if (containingNamespaceSymbol.ContainingNamespace is null) return false;

            if (containingNamespaceSymbol.ContainingNamespace.Name != ns1) return false;

            if (containingNamespaceSymbol.ContainingNamespace.ContainingNamespace is null) return false;

            if (!containingNamespaceSymbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace) return false;

            return true;
        }


        private static bool IsAssignableToIXImpl(ITypeSymbol? typeSymbol, Func<ITypeSymbol, bool> isXTypeFunc, Func<ITypeSymbol, bool> isAssignableToXFunc)
        {
            if (typeSymbol is null) return false;

            if (isXTypeFunc(typeSymbol)) return true;

            if (typeSymbol.AllInterfaces.Any((Func<INamedTypeSymbol, bool>)isXTypeFunc)) return true;

            // ジェネリック型の型パラメータの場合は型パラメータの制約を再帰的に確認
            if (typeSymbol is ITypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ConstraintTypes.Any(isAssignableToXFunc))
            {
                return true;
            }

            return false;
        }

        private static bool IsXAttributedMemberImpl(ISymbol? symbol, Func<INamedTypeSymbol, bool> isXAttributeSymbol)
        {
            if (symbol is null) return false;

            foreach (var attributeData in symbol.GetAttributes())
            {
                if (attributeData.AttributeClass is not null && isXAttributeSymbol(attributeData.AttributeClass))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsAutomaticNotifyPropertyChangedImplAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, AutomaticNotifyPropertyChangedImplAttributeName);

        internal static bool IsEnableAutomaticNotifyAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, EnableAutomaticNotifyAttributeName);

        internal static bool IsDisableAutomaticNotifyAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, DisableAutomaticNotifyAttributeName);


        internal static bool IsINotifyPropertyChanged(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, "System", "ComponentModel", "INotifyPropertyChanged");

        internal static bool IsAssignableToINotifyPropertyChanged(ITypeSymbol? typeSymbol) => IsAssignableToIXImpl(typeSymbol, IsINotifyPropertyChanged, IsAssignableToINotifyPropertyChanged);


        class SyntaxContextReceiver : ISyntaxContextReceiver
        {
#pragma warning disable RS1024 // シンボルを正しく比較する
            /// <summary>
            /// コンパイル対象全体から作られる型のシンボルと構文木内でその型を定義しているClassDeclarationSyntaxの対応テーブル
            /// </summary>
            public Dictionary<ISymbol, List<ClassDeclarationSyntax>> ClassDeclarationTable { get; } = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // シンボルを正しく比較する

            public List<(ClassDeclarationSyntax syntaxNode, INamedTypeSymbol symbol)> AnotatedClassDeclarations { get; } = new();


            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
                {
                    return;
                }

                if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol namedTypeSymbol)
                {
                    return;
                }

                if (!ClassDeclarationTable.TryGetValue(namedTypeSymbol, out var classDeclarationSyntaxes))
                {
                    classDeclarationSyntaxes = new List<ClassDeclarationSyntax>();
                    ClassDeclarationTable.Add(namedTypeSymbol, classDeclarationSyntaxes);
                }

                classDeclarationSyntaxes.Add(classDeclarationSyntax);

                bool isAutomaticDisposeImplAnnotationedDeclaration = false;

                foreach (var attributeList in classDeclarationSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (IsAutomaticNotifyPropertyChangedImplAttribute(context.SemanticModel.GetTypeInfo(attribute).Type))
                        {
                            isAutomaticDisposeImplAnnotationedDeclaration = true;
                            goto LOOP_END_isAutomaticDisposeImplAnnotationedDeclaration;
                        }
                    }
                }
            LOOP_END_isAutomaticDisposeImplAnnotationedDeclaration:

                if (isAutomaticDisposeImplAnnotationedDeclaration)
                {
                    AnotatedClassDeclarations.Add((classDeclarationSyntax, namedTypeSymbol));
                }
            }
        }
    }
}
