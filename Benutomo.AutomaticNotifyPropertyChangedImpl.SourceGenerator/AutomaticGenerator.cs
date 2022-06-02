using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    [Generator]
    public partial class AutomaticGenerator : IIncrementalGenerator
    {
        internal const string AttributeDefinedNameSpace = "Benutomo";

        internal const string EventToObservableName = "EventToObservable";
        internal const string EventToObservableFullyQualifiedMetadataName = "Benutomo.Internal.EventToObservable";
        private const string EventToObservableSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo.Internal
{
    internal class EventToObservable : IObservable<object?>
    {
        Action<EventHandler> _addHandler;
        Action<EventHandler> _removeHandler;

        public EventToObservable(Action<EventHandler> addHandler, Action<EventHandler> removeHandler)
        {
            _addHandler = addHandler ?? throw new ArgumentNullException(nameof(addHandler));
            _removeHandler = removeHandler ?? throw new ArgumentNullException(nameof(removeHandler));
        }

        public IDisposable Subscribe(IObserver<object?> observer) => new Proxy(_addHandler, _removeHandler, observer);

        private class Proxy : IDisposable
        {
            Action<EventHandler>? _removeHandler;
            IObserver<object?>? _observer;

            public Proxy(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, IObserver<object?> observer)
            {
                addHandler(EventHandler);
                _removeHandler = removeHandler;
                _observer = observer;
            }

            public void Dispose()
            {
                var removeHandler = Interlocked.Exchange(ref _removeHandler, null);
                removeHandler?.Invoke(EventHandler);

                var observer = Interlocked.Exchange(ref _observer, null);
                observer?.OnCompleted();
            }

            void EventHandler(object? source, EventArgs args) => _observer?.OnNext(source);
        }
    }
}
";

        internal const string EnableNotificationSupportAttributeName = "EnableNotificationSupportAttribute";
        internal const string EnableNotificationSupportAttributeFullyQualifiedMetadataName = "Benutomo.EnableNotificationSupportAttribute";
        private const string EnableNotificationSupportAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class EnableNotificationSupportAttribute : Attribute
    {
        public bool EventArgsOnly { get; set; } = false;
    }
}
";

        internal const string ChangedEventAttributeName = "ChangedEventAttribute";
        internal const string ChangedEventAttributeFullyQualifiedMetadataName = "Benutomo.ChangedEventAttribute";
        private const string ChangedEventAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class ChangedEventAttribute : Attribute
    {
        public NotificationAccessibility Accessibility { get; } = NotificationAccessibility.Public;

        public ChangedEventAttribute() {}

        public ChangedEventAttribute(NotificationAccessibility Accessibility) {}
    }
}
";

        internal const string ChangingEventAttributeName = "ChangingEventAttribute";
        internal const string ChangingEventAttributeFullyQualifiedMetadataName = "Benutomo.ChangingEventAttribute";
        private const string ChangingEventAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class ChangingEventAttribute : Attribute
    {
        public NotificationAccessibility Accessibility { get; } = NotificationAccessibility.Public;

        public ChangingEventAttribute() {}

        public ChangingEventAttribute(NotificationAccessibility Accessibility) {}
    }
}
";

        internal const string ChangedObservableAttributeName = "ChangedObservableAttribute";
        internal const string ChangedObservableAttributeFullyQualifiedMetadataName = "Benutomo.ChangedObservableAttribute";
        private const string ChangedObservableAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class ChangedObservableAttribute : Attribute
    {
        public NotificationAccessibility Accessibility { get; } = NotificationAccessibility.Public;

        public ChangedObservableAttribute() {}

        public ChangedObservableAttribute(NotificationAccessibility Accessibility) {}
    }
}
";

        internal const string ChangingObservableAttributeName = "ChangingObservableAttribute";
        internal const string ChangingObservableAttributeFullyQualifiedMetadataName = "Benutomo.ChangingObservableAttribute";
        private const string ChangingObservableAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class ChangingObservableAttribute : Attribute
    {
        public NotificationAccessibility Accessibility { get; } = NotificationAccessibility.Public;

        public ChangingObservableAttribute() {}

        public ChangingObservableAttribute(NotificationAccessibility Accessibility) {}
    }
}
";

        internal const string NotificationAccessibilityName = "NotificationAccessibility";
        internal const string NotificationAccessibilityFullyQualifiedMetadataName = "Benutomo.NotificationAccessibility";
        internal const int NotificationAccessibilityPublic = 0;
        internal const int NotificationAccessibilityProtected = 1;
        internal const int NotificationAccessibilityInternal = 2;
        internal const int NotificationAccessibilityInternalProtected = 3;
        private const string NotificationAccessibilitySource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// Todo
    /// </summary>
    internal enum NotificationAccessibility : int
    {
        Public,
        Protected,
        Internal,
        InternalProtected,
    }
}
";

        record struct UsingSymbols(
            INamedTypeSymbol EnableNotificationSupportAttribute,
            INamedTypeSymbol ChangedEvent,
            INamedTypeSymbol ChangingEvent,
            INamedTypeSymbol ChangedObservable,
            INamedTypeSymbol ChantingObservable,
            INamedTypeSymbol NotifyPropertyChanged,
            INamedTypeSymbol NotifyPropertyChanging
            );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(PostInitialization);

            var enableNotificationSupportAttributeSymbol = context.CompilationProvider
                .Select((compilation, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var enableNotificationSupportAttributeSymbol = compilation.GetTypeByMetadataName(EnableNotificationSupportAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                    var changedEventAttributeSymbol = compilation.GetTypeByMetadataName(ChangedEventAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                    var changingEventAttributeSymbol = compilation.GetTypeByMetadataName(ChangingEventAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                    var changedObservableAttributeSymbol = compilation.GetTypeByMetadataName(ChangedObservableAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                    var changingObservableAttributeSymbol = compilation.GetTypeByMetadataName(ChangingObservableAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                    var notifyPropertyChangedSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged") ?? throw new InvalidOperationException();
                    var notifyPropertyChangingSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanging") ?? throw new InvalidOperationException();

                    return new UsingSymbols(
                        enableNotificationSupportAttributeSymbol,
                        changedEventAttributeSymbol,
                        changingEventAttributeSymbol,
                        changedObservableAttributeSymbol,
                        changingObservableAttributeSymbol,
                        notifyPropertyChangedSymbol,
                        notifyPropertyChangingSymbol
                    );
                });

            var anotatedClasses = context.SyntaxProvider
                .CreateSyntaxProvider(Predicate, Transform)
                .Where(v => v is not null)
                .Combine(enableNotificationSupportAttributeSymbol)
                .Select((v, ct) => (propertySymbol: v.Left, usingSymbols: v.Right));

            context.RegisterSourceOutput(anotatedClasses, Generate);
        }

        void PostInitialization(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{EventToObservableName}.cs", EventToObservableSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{EnableNotificationSupportAttributeName}.cs", EnableNotificationSupportAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{ChangingEventAttributeName}.cs", ChangingEventAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{ChangedEventAttributeName}.cs", ChangedEventAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{ChangingObservableAttributeName}.cs", ChangingObservableAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{ChangedObservableAttributeName}.cs", ChangedObservableAttributeSource);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"{NotificationAccessibilityName}.cs", NotificationAccessibilitySource);
        }

        
        bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return node is PropertyDeclarationSyntax
            {
                AttributeLists.Count: > 0
            };
        }

        IPropertySymbol Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken) as IPropertySymbol;

            return propertySymbol!;
        }

        void Generate(SourceProductionContext context, (IPropertySymbol propertySymbol, UsingSymbols usingSymbols) args)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            
            var enableNotificationSupportAttributeData = args.propertySymbol.GetAttributes().SingleOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, args.usingSymbols.EnableNotificationSupportAttribute));
            if (enableNotificationSupportAttributeData is null)
            {
                return;
            }

            var sourceBuilder = new SourceBuilder(context, args.propertySymbol, args.usingSymbols, enableNotificationSupportAttributeData);

            sourceBuilder.Build();

            context.AddSource(sourceBuilder.HintName, sourceBuilder.SourceText);
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


        internal static bool IsEnableAutomaticNotifyAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, EnableNotificationSupportAttributeName);

        internal static bool IsINotifyPropertyChanged(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, "System", "ComponentModel", "INotifyPropertyChanged");

        internal static bool IsAssignableToINotifyPropertyChanged(ITypeSymbol? typeSymbol) => IsAssignableToIXImpl(typeSymbol, IsINotifyPropertyChanged, IsAssignableToINotifyPropertyChanged);
    }
}
