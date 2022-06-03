using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    [Generator]
    public partial class AutomaticGenerator : IIncrementalGenerator
    {
#if DEBUG
        static StreamWriter _streamWriter;
        static AutomaticGenerator()
        {
            Directory.CreateDirectory(@"c:\var\log\AutomaticNotifyPropertyChangedImpl");
            var proc = Process.GetCurrentProcess();
            _streamWriter = new StreamWriter($@"c:\var\log\AutomaticNotifyPropertyChangedImpl\{DateTime.Now:yyyyMMddHHmmss}_{proc.Id}.txt");
            _streamWriter.WriteLine(proc);
        }

        [Conditional("DEBUG")]
        static void WriteLogLine(string line)
        {
            lock (_streamWriter)
            {
                _streamWriter.WriteLine(line);
                _streamWriter.Flush();
            }
        }
#else
        [Conditional("DEBUG")]
        static void WriteLogLine(string line)
        {
        }
#endif

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
        internal const int NotificationAccessibilityPrivate = 4;
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
        Private,
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
            )
        {
            public bool Equals(UsingSymbols other)
            {
                var result =
                    SymbolEqualityComparer.IncludeNullability.Equals(EnableNotificationSupportAttribute, other.EnableNotificationSupportAttribute) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(ChangedEvent, other.ChangedEvent) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(ChangingEvent, other.ChangingEvent) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(ChangedObservable, other.ChangedObservable) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(ChantingObservable, other.ChantingObservable) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(NotifyPropertyChanged, other.NotifyPropertyChanged) &&
                    SymbolEqualityComparer.IncludeNullability.Equals(NotifyPropertyChanging, other.NotifyPropertyChanging);

                WriteLogLine($"UsingSymbols.Equals() => {result}");

                return result;
            }

            public override int GetHashCode()
            {
                return
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(EnableNotificationSupportAttribute) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(ChangedEvent) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(ChangingEvent) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(ChangedObservable) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(ChantingObservable) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(NotifyPropertyChanged) ^
                    SymbolEqualityComparer.IncludeNullability.GetHashCode(NotifyPropertyChanging);
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            WriteLogLine("Begin Initialize");

            context.RegisterPostInitializationOutput(PostInitialization);

            var enableNotificationSupportAttributeSymbol = context.CompilationProvider
                .Select((compilation, cancellationToken) =>
                {
                    WriteLogLine("Begin GetTypeByMetadataName");
                    
                    try
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
                    }
                    catch (OperationCanceledException)
                    {
                        WriteLogLine("Canceled GetTypeByMetadataName");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteLogLine("Exception GetTypeByMetadataName");
                        WriteLogLine(ex.ToString());
                        throw;
                    }
                });

            // Where句を使用しない。
            // https://github.com/dotnet/roslyn/issues/57991
            // 今は、Where句を使用するとSource GeneratorがVSでインクリメンタルに実行されたときに
            // 対象のコードの状態や編集内容などによって突然内部状態が壊れて機能しなくなる問題がおきる。

            var anotatedClasses = context.SyntaxProvider
                .CreateSyntaxProvider(Predicate, Transform)
                //.Where(v => v is not null)
                .Combine(enableNotificationSupportAttributeSymbol)
                .Select(PostTransform)
                ;//.Where(v => v is not null);

            context.RegisterSourceOutput(anotatedClasses, Generate);

            WriteLogLine("End Initialize");
        }

        void PostInitialization(IncrementalGeneratorPostInitializationContext context)
        {
            WriteLogLine("Begin PostInitialization");

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

            WriteLogLine("End PostInitialization");
        }

        
        bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
        {
            //WriteLogLine("Predicate");

            return node is PropertyDeclarationSyntax
            {
                AttributeLists.Count: > 0
            };
        }

        IPropertySymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            WriteLogLine("Begin Transform");
            try
            {
                var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken) as IPropertySymbol;

                WriteLogLine($"End Transform ({propertySymbol?.ContainingType?.Name}.{propertySymbol?.Name})");

                return propertySymbol;
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled Transform");
                throw;
            }
        }

        SourceBuildInputs? PostTransform((IPropertySymbol? Left, UsingSymbols Right) v, CancellationToken ct)
        {
            var propertySymbol = v.Left;
            var usingSymbols = v.Right;

            if (propertySymbol is null) return null;

            WriteLogLine($"Begin PostTransform ({propertySymbol.ContainingType?.Name}.{propertySymbol.Name})");

            try
            {
                var enableNotificationSupportAttributeData = propertySymbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, usingSymbols.EnableNotificationSupportAttribute));
                if (enableNotificationSupportAttributeData is null)
                {
                    return null;
                }

                var result = new SourceBuildInputs(propertySymbol, usingSymbols, enableNotificationSupportAttributeData);

                WriteLogLine($"End PostTransform ({propertySymbol.ContainingType?.Name}.{propertySymbol.Name})");

                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled PostTransform ({propertySymbol.ContainingType?.Name}.{propertySymbol.Name})");
                throw;
            }
            catch (Exception ex)
            {
                WriteLogLine($"Exception PostTransform ({propertySymbol.ContainingType?.Name}.{propertySymbol.Name})");
                WriteLogLine(ex.ToString());
                throw;
            }
        }

        void Generate(SourceProductionContext context, SourceBuildInputs? sourceBuildInputs)
        {
            if (sourceBuildInputs is null) return;

            WriteLogLine($"Begin Generate ({sourceBuildInputs.ContainingTypeInfo.Name}.{sourceBuildInputs.PropertyName})");

            try
            {
                var sourceBuilder = new SourceBuilder(context, sourceBuildInputs);

                sourceBuilder.Build();

                context.AddSource(sourceBuilder.HintName, sourceBuilder.SourceText);

                WriteLogLine($"End Generate ({sourceBuildInputs.ContainingTypeInfo.Name}.{sourceBuildInputs.PropertyName}) => {sourceBuilder.HintName}");
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled Generate ({sourceBuildInputs.ContainingTypeInfo.Name}.{sourceBuildInputs.PropertyName})");
                throw;
            }
            catch (Exception ex)
            {
                WriteLogLine($"Exception in Generate ({sourceBuildInputs.ContainingTypeInfo.Name}.{sourceBuildInputs.PropertyName})");
                WriteLogLine(ex.ToString());
                throw;
            }
        }
    }
}
