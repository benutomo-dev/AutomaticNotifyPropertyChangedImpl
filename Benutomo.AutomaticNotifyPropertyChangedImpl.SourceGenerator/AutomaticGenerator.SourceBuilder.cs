using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    public partial class AutomaticGenerator
    {
        interface ITypeContainer
        {
            string Name { get; }
        }

        class NameSpaceInfo : ITypeContainer, IEquatable<NameSpaceInfo?>
        {
            public string Name { get; }

            public NameSpaceInfo(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as NameSpaceInfo);
            }

            public bool Equals(NameSpaceInfo? other)
            {
                return other is not null &&
                       Name == other.Name;
            }

            public override int GetHashCode()
            {
                return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
            }
        }

        class TypeDefinitionInfo : ITypeContainer, IEquatable<TypeDefinitionInfo?>
        {
            public ITypeContainer Container { get; }

            public string Name { get; }

            public bool IsValueType { get; }

            public bool IsNullableAnoteted { get; }

            public ImmutableArray<string> GenericTypeArgs { get; }

            public TypeDefinitionInfo(ITypeContainer container, string name, bool isValueType, bool isNullableAnoteted, ImmutableArray<string> genericTypeArgs)
            {
                Container = container ?? throw new ArgumentNullException(nameof(container));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                IsValueType = isValueType;
                IsNullableAnoteted = isNullableAnoteted;
                GenericTypeArgs = genericTypeArgs;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as TypeDefinitionInfo);
            }

            public bool Equals(TypeDefinitionInfo? other)
            {
                return other is not null &&
                       EqualityComparer<ITypeContainer>.Default.Equals(Container, other.Container) &&
                       Name == other.Name &&
                       IsValueType == other.IsValueType &&
                       IsNullableAnoteted == other.IsNullableAnoteted &&
                       GenericTypeArgs.SequenceEqual(other.GenericTypeArgs);
            }

            public override int GetHashCode()
            {
                int hashCode = 319546947;
                hashCode = hashCode * -1521134295 + EqualityComparer<ITypeContainer>.Default.GetHashCode(Container);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + IsValueType.GetHashCode();
                hashCode = hashCode * -1521134295 + IsNullableAnoteted.GetHashCode();
                hashCode = hashCode * -1521134295 + GenericTypeArgs.Sum(v => v.GetHashCode());
                return hashCode;
            }
        }

        enum DeclareState
        {
            None,
            Private,
            Public,
            Protected,
            Internal,
            InternalProrected,
        }

        class SourceBuildInputs : IEquatable<SourceBuildInputs?>
        {
            public TypeDefinitionInfo ContainingTypeInfo;

            public string PropertyName;

            public string PropertyType;

            public bool PropertyTypeIsReferenceType;

            public NullableAnnotation PropertyTypeNullableAnnotation;

            public ImmutableArray<SyntaxReference> PropertyDeclaringSyntaxReferences;

            public bool IsEventArgsOnly;

            public bool EnabledNotifyPropertyChanging;

            public bool EnabledNotifyPropertyChanged;

            public DeclareState ChangedEventDeclareState;

            public DeclareState ChangingEventDeclareState;

            public DeclareState ChangedObservableDeclareState;

            public DeclareState ChangingObservableDeclareState;

            public SourceBuildInputs(IPropertySymbol propertySymbol, UsingSymbols usingSymbols, AttributeData enableNotificationSupportAttributeData)
            {
                WriteLogLine($"SourceBuildInputs ({propertySymbol.ContainingType.Name}.{propertySymbol.Name})");

                ContainingTypeInfo = BuildTypeDefinitionInfo(propertySymbol.ContainingType);

                PropertyName = propertySymbol.Name;

                StringBuilder typeNameBuilder = new StringBuilder();
                AppendFullTypeName(typeNameBuilder, propertySymbol.Type);
                PropertyType = typeNameBuilder.ToString();
                PropertyTypeIsReferenceType = propertySymbol.Type.IsReferenceType;
                PropertyTypeNullableAnnotation = propertySymbol.Type.NullableAnnotation;

                PropertyDeclaringSyntaxReferences = propertySymbol.DeclaringSyntaxReferences;

                IsEventArgsOnly = enableNotificationSupportAttributeData.NamedArguments.Where(v => v.Key == "EventArgsOnly").Select(v => (bool)(v.Value.Value ?? false)).FirstOrDefault();

                EnabledNotifyPropertyChanging = propertySymbol.ContainingType.AllInterfaces.Any(v => SymbolEqualityComparer.Default.Equals(v, usingSymbols.NotifyPropertyChanging));
                EnabledNotifyPropertyChanged = propertySymbol.ContainingType.AllInterfaces.Any(v => SymbolEqualityComparer.Default.Equals(v, usingSymbols.NotifyPropertyChanged));

                foreach (var attributeData in propertySymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangedEvent))
                    {
                        ChangedEventDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangingEvent))
                    {
                        ChangingEventDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangedObservable))
                    {
                        ChangedObservableDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChantingObservable))
                    {
                        ChangingObservableDeclareState = GetDeclareState(attributeData);
                    }
                }

                if (ChangedObservableDeclareState != DeclareState.None && ChangedEventDeclareState == DeclareState.None)
                {
                    // Observableはイベントを変換する形で実装するのでprivateでイベントを用意する。
                    ChangedEventDeclareState = DeclareState.Private;
                }

                if (ChangingObservableDeclareState != DeclareState.None && ChangingEventDeclareState == DeclareState.None)
                {
                    // Observableはイベントを変換する形で実装するのでprivateでイベントを用意する。
                    ChangingEventDeclareState = DeclareState.Private;
                }

                return;



                static DeclareState GetDeclareState(AttributeData attributeData)
                {
                    if (attributeData.ConstructorArguments.Length == 0) return DeclareState.Public;

                    if (attributeData.ConstructorArguments.Length > 1) throw new InvalidOperationException();

                    return attributeData.ConstructorArguments[0].Value switch
                    {
                        NotificationAccessibilityPublic => DeclareState.Public,
                        NotificationAccessibilityInternal => DeclareState.Internal,
                        NotificationAccessibilityProtected => DeclareState.Protected,
                        NotificationAccessibilityInternalProtected => DeclareState.InternalProrected,
                        NotificationAccessibilityPrivate => DeclareState.Private,
                        _ => DeclareState.None,
                    };
                }

                static TypeDefinitionInfo BuildTypeDefinitionInfo(ITypeSymbol typeSymbol)
                {
                    ITypeContainer container;

                    if (typeSymbol.ContainingType is null)
                    {
                        var namespaceBuilder = new StringBuilder();
                        AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

                        container = new NameSpaceInfo(namespaceBuilder.ToString());
                    }
                    else
                    {
                        container = BuildTypeDefinitionInfo(typeSymbol.ContainingType);
                    }

                    ImmutableArray<string> genericTypeArgs = ImmutableArray<string>.Empty;

                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol && !namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
                    {
                        var builder = ImmutableArray.CreateBuilder<string>(namedTypeSymbol.TypeArguments.Length);

                        for (int i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
                        {
                            builder.Add(namedTypeSymbol.TypeArguments[i].Name);
                        }
                    }

                    return new TypeDefinitionInfo(container, typeSymbol.Name, typeSymbol.IsValueType, typeSymbol.NullableAnnotation == NullableAnnotation.Annotated, genericTypeArgs);
                }

                static void AppendFullTypeName(StringBuilder typeNameBuilder, ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.ContainingType is null)
                    {
                        typeNameBuilder.Append("global::");
                        AppendFullNamespace(typeNameBuilder, typeSymbol.ContainingNamespace);
                    }
                    else
                    {
                        AppendFullTypeName(typeNameBuilder, typeSymbol.ContainingType);
                    }
                    typeNameBuilder.Append(".");

                    typeNameBuilder.Append(typeSymbol.Name);

                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol && !namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
                    {
                        var typeArguments = namedTypeSymbol.TypeArguments;

                        typeNameBuilder.Append("<");

                        for (int i = 0; i < typeArguments.Length - 1; i++)
                        {
                            AppendFullTypeName(typeNameBuilder, typeArguments[i]);
                            typeNameBuilder.Append(", ");
                        }
                        AppendFullTypeName(typeNameBuilder, typeArguments[typeArguments.Length - 1]);

                        typeNameBuilder.Append(">");
                    }

                    if (typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        typeNameBuilder.Append("?");
                    }
                }

                static void AppendFullNamespace(StringBuilder namespaceNameBuilder, INamespaceSymbol namespaceSymbol)
                {
                    if (namespaceSymbol.ContainingNamespace is not null && !namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        AppendFullNamespace(namespaceNameBuilder, namespaceSymbol.ContainingNamespace);
                        namespaceNameBuilder.Append(".");
                    }

                    namespaceNameBuilder.Append(namespaceSymbol.Name);
                }
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as SourceBuildInputs);
            }

            public bool Equals(SourceBuildInputs? other)
            {
                var result = other is not null &&
                       EqualityComparer<TypeDefinitionInfo>.Default.Equals(ContainingTypeInfo, other.ContainingTypeInfo) &&
                       PropertyName == other.PropertyName &&
                       PropertyType == other.PropertyType &&
                       PropertyTypeIsReferenceType == other.PropertyTypeIsReferenceType &&
                       PropertyTypeNullableAnnotation == other.PropertyTypeNullableAnnotation &&
                       IsEventArgsOnly == other.IsEventArgsOnly &&
                       EnabledNotifyPropertyChanging == other.EnabledNotifyPropertyChanging &&
                       EnabledNotifyPropertyChanged == other.EnabledNotifyPropertyChanged &&
                       ChangedEventDeclareState == other.ChangedEventDeclareState &&
                       ChangingEventDeclareState == other.ChangingEventDeclareState &&
                       ChangedObservableDeclareState == other.ChangedObservableDeclareState &&
                       ChangingObservableDeclareState == other.ChangingObservableDeclareState;

                WriteLogLine($"SourceBuildInputs.Equals({ContainingTypeInfo.Name}.{PropertyName}) => {result}");

                return result;
            }

            public override int GetHashCode()
            {
                int hashCode = 126218788;
                hashCode = hashCode * -1521134295 + EqualityComparer<TypeDefinitionInfo>.Default.GetHashCode(ContainingTypeInfo);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PropertyName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PropertyType);
                hashCode = hashCode * -1521134295 + PropertyTypeIsReferenceType.GetHashCode();
                hashCode = hashCode * -1521134295 + PropertyTypeNullableAnnotation.GetHashCode();
                hashCode = hashCode * -1521134295 + IsEventArgsOnly.GetHashCode();
                hashCode = hashCode * -1521134295 + EnabledNotifyPropertyChanging.GetHashCode();
                hashCode = hashCode * -1521134295 + EnabledNotifyPropertyChanged.GetHashCode();
                hashCode = hashCode * -1521134295 + ChangedEventDeclareState.GetHashCode();
                hashCode = hashCode * -1521134295 + ChangingEventDeclareState.GetHashCode();
                hashCode = hashCode * -1521134295 + ChangedObservableDeclareState.GetHashCode();
                hashCode = hashCode * -1521134295 + ChangingObservableDeclareState.GetHashCode();
                return hashCode;
            }
        }


        class SourceBuilder
        {
#if DEBUG
            public string HintName => $"gen_{string.Join(".", _hintingTypeNames)}.{_sourceBuildInputs.PropertyName}_{_nameSpace}.cs";
#else
            public string HintName => $"gen_{string.Join(".", _hintingTypeNames)}.{_sourceBuildInputs.PropertyName}_{_nameSpace}.g.cs";
#endif

            public string SourceText => _sourceBuilder.ToString();


            readonly SourceProductionContext _context;

            readonly SourceBuildInputs _sourceBuildInputs;

            string _nameSpace = "";

            readonly List<string> _hintingTypeNames = new List<string>();

            readonly StringBuilder _sourceBuilder = new StringBuilder(4000);

            int _currentIndentCount = 0;

            const string indentText = "    ";


            public SourceBuilder(SourceProductionContext context, SourceBuildInputs sourceBuildInputs)
            {
                _context = context;
                _sourceBuildInputs = sourceBuildInputs;
            }

            public void Build()
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                _nameSpace = "global";
                _hintingTypeNames.Clear();
                _sourceBuilder.Clear();

#if !DEBUG
                _sourceBuilder.AppendLine("// <auto-generated />");
#endif

                _sourceBuilder.AppendLine("#nullable enable");
                _sourceBuilder.AppendLine("#pragma warning disable CS0612,CS0618,CS0619");

                _context.CancellationToken.ThrowIfCancellationRequested();
                WriteTypeDeclarationStart();

                _context.CancellationToken.ThrowIfCancellationRequested();
                WriteBody();

                _context.CancellationToken.ThrowIfCancellationRequested();
                WriteTypeDeclarationEnd();

#if DEBUG
                var code = _sourceBuilder.ToString();
                ;
#endif
            }

            void PutIndentSpace()
            {
                for (int i = 0; i < _currentIndentCount; i++)
                {
                    _sourceBuilder.Append(indentText);
                }
            }

            void BeginTryBlock()
            {
                BeginBlock("try");
            }

            void BeginFinallyBlock()
            {
                BeginBlock("finally");
            }

            void BeginBlock(string blockHeadLine)
            {
                PutIndentSpace(); _sourceBuilder.AppendLine(blockHeadLine);
                BeginBlock();
            }

            void BeginBlock()
            {
                PutIndentSpace(); _sourceBuilder.AppendLine("{");
                _currentIndentCount++;
            }

            void EndBlock()
            {
                _currentIndentCount--;
                PutIndentSpace(); _sourceBuilder.AppendLine("}");
            }

            void WriteTypeDeclarationStart()
            {
                WriteContainingTypeStart(_sourceBuildInputs.ContainingTypeInfo, isDesingationType: true);

                return;

                void WriteContainingTypeStart(TypeDefinitionInfo namedTypeSymbol, bool isDesingationType)
                {
                    if (namedTypeSymbol.Container is NameSpaceInfo nameSpace)
                    {
                        WriteContainingNameSpaceStart(nameSpace);
                    }
                    else if (namedTypeSymbol.Container is TypeDefinitionInfo typeInfo)
                    {
                        WriteContainingTypeStart(typeInfo, isDesingationType: false);
                    }

                    _context.CancellationToken.ThrowIfCancellationRequested();

                    PutIndentSpace();
                    _sourceBuilder.Append("partial ");
                    _sourceBuilder.Append(namedTypeSymbol.IsValueType ? "struct " : "class ");
                    _sourceBuilder.Append(namedTypeSymbol.Name);

                    if (namedTypeSymbol.GenericTypeArgs.Length > 0)
                    {

                        var hintingTypeNameBuilder = new StringBuilder();

                        hintingTypeNameBuilder.Append(namedTypeSymbol.Name);
                        hintingTypeNameBuilder.Append("{");
                        hintingTypeNameBuilder.Append(string.Join("_", namedTypeSymbol.GenericTypeArgs));
                        hintingTypeNameBuilder.Append("}");
                    }
                    else
                    {
                        _hintingTypeNames.Add(namedTypeSymbol.Name);
                    }

                    if (isDesingationType)
                    {
                        // なくてもいいが生成されたコードだけを見ても実装対象となっているインターフェイスが分かるようにしておく

                        _sourceBuilder.Append(" // This is implementation class by AutomaticNotifyPropertyChangedImpl.");
                    }
                    _sourceBuilder.AppendLine("");

                    BeginBlock();
                }

                void WriteContainingNameSpaceStart(NameSpaceInfo namespaceSymbol)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("namespace ");
                    _sourceBuilder.Append(namespaceSymbol.Name);
                    _sourceBuilder.AppendLine("");

                    _nameSpace = namespaceSymbol.Name;

                    BeginBlock();
                }
            }

            void WriteTypeDeclarationEnd()
            {
                WriteContainingTypeEnd(_sourceBuildInputs.ContainingTypeInfo);

                return;

                void WriteContainingTypeEnd(TypeDefinitionInfo namedTypeSymbol)
                {
                    EndBlock();

                    _context.CancellationToken.ThrowIfCancellationRequested();


                    if (namedTypeSymbol.Container is NameSpaceInfo nameSpace)
                    {
                        WriteContainingNameSpaceEnd(nameSpace);
                    }
                    else if (namedTypeSymbol.Container is TypeDefinitionInfo typeInfo)
                    {
                        WriteContainingTypeEnd(typeInfo);
                    }
                }

                void WriteContainingNameSpaceEnd(NameSpaceInfo namespaceSymbol)
                {
                    EndBlock();
                }
            }

            void WriteBody()
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                _sourceBuilder.AppendLine();

                var changedEventArgFieldName = $"__PropertyChangedEventArgs_{_sourceBuildInputs.PropertyName}";
                var changingEventArgFieldName = $"__PropertyChangingEventArgs_{_sourceBuildInputs.PropertyName}";

                if (_sourceBuildInputs.EnabledNotifyPropertyChanging)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("private static global::System.ComponentModel.PropertyChangingEventArgs ");
                    _sourceBuilder.Append(changingEventArgFieldName);
                    _sourceBuilder.Append(" = new global::System.ComponentModel.PropertyChangingEventArgs(nameof(");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("));");
                }

                if (_sourceBuildInputs.EnabledNotifyPropertyChanged)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("private static global::System.ComponentModel.PropertyChangedEventArgs ");
                    _sourceBuilder.Append(changedEventArgFieldName);
                    _sourceBuilder.Append(" = new global::System.ComponentModel.PropertyChangedEventArgs(nameof(");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("));");
                }

                if (_sourceBuildInputs.IsEventArgsOnly)
                {
                    return;
                }

                var fieldName = $"__{char.ToLowerInvariant(_sourceBuildInputs.PropertyName[0])}{_sourceBuildInputs.PropertyName.Substring(1)}";

                var methodName = $"_{_sourceBuildInputs.PropertyName}";


                if (_sourceBuildInputs.ChangingEventDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_sourceBuildInputs.ChangingEventDeclareState));
                    _sourceBuilder.Append(" event global::System.EventHandler? ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("Changing;");
                }

                if (_sourceBuildInputs.ChangedEventDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_sourceBuildInputs.ChangedEventDeclareState));
                    _sourceBuilder.Append(" event global::System.EventHandler? ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("Changed;");
                }

                if (_sourceBuildInputs.ChangingObservableDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_sourceBuildInputs.ChangingObservableDeclareState));
                    _sourceBuilder.Append(" global::System.IObservable<object?> ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.Append("ChangingAsObservable() =>  new global::Benutomo.Internal.EventToObservable(h => ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.Append("Changing += h, h => ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("Changing -= h);");
                }

                if (_sourceBuildInputs.ChangedObservableDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_sourceBuildInputs.ChangedObservableDeclareState));
                    _sourceBuilder.Append(" global::System.IObservable<object?> ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.Append("ChangedAsObservable() =>  new global::Benutomo.Internal.EventToObservable(h => ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.Append("Changed += h, h => ");
                    _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                    _sourceBuilder.AppendLine("Changed -= h);");
                }

                PutIndentSpace();
                _sourceBuilder.Append("private ");
                _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                _sourceBuilder.Append(" ");
                _sourceBuilder.Append(fieldName);
                _sourceBuilder.AppendLine(";");

                PutIndentSpace();
                _sourceBuilder.Append("private ");
                _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                _sourceBuilder.Append(" ");
                _sourceBuilder.Append(methodName);
                _sourceBuilder.Append("() => this.");
                _sourceBuilder.Append(fieldName);
                _sourceBuilder.AppendLine(";");

                if (_sourceBuildInputs.PropertyTypeIsReferenceType && _sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(@"[global::System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(");
                    _sourceBuilder.Append(fieldName);
                    _sourceBuilder.AppendLine(@"))]");
                }
                PutIndentSpace();
                _sourceBuilder.Append("private bool ");
                _sourceBuilder.Append(methodName);
                _sourceBuilder.Append("(");
                _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                _sourceBuilder.AppendLine(" value)");
                BeginBlock();
                {
                    if (_sourceBuildInputs.PropertyTypeIsReferenceType)
                    {
                        if (_sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            PutIndentSpace();
                            _sourceBuilder.AppendLine("if (value is null) throw new ArgumentNullException(nameof(value));");
                        }
                        else if (_sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.None)
                        {
                            var descripter = new DiagnosticDescriptor("SGN001", "Nullable context is not enabled.", "Set the Nullable property to enable in the project file or set #nullable enable in the source code.", "code", DiagnosticSeverity.Warning, isEnabledByDefault: true);

                            foreach (var declaration in _sourceBuildInputs.PropertyDeclaringSyntaxReferences)
                            {
                                _context.ReportDiagnostic(Diagnostic.Create(descripter, declaration.GetSyntax(_context.CancellationToken).GetLocation()));
                            }
                        }

                        PutIndentSpace();
                        _sourceBuilder.Append("if (object.ReferenceEquals(");
                        _sourceBuilder.Append(fieldName);
                        _sourceBuilder.AppendLine(", value)) return false;");
                    }
                    else
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append("if (global::System.Collections.Generic.EqualityComparer<");
                        _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                        _sourceBuilder.Append(">.Default.Equals(");
                        _sourceBuilder.Append(fieldName);
                        _sourceBuilder.AppendLine(", value)) return false;");
                    }

                    // フィールドの変更と変更前後通知を行う処理
                    WriteFieldChangeSection();
                }
                EndBlock();

                PutIndentSpace();
                _sourceBuilder.Append("private bool ");
                _sourceBuilder.Append(methodName);
                _sourceBuilder.Append("(");
                _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                _sourceBuilder.Append(" value, global::System.Collections.Generic.IEqualityComparer<");
                _sourceBuilder.Append(_sourceBuildInputs.PropertyType);
                _sourceBuilder.AppendLine("> equalityComparer) ");
                BeginBlock();
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("if (equalityComparer.Equals(");
                    _sourceBuilder.Append(fieldName);
                    _sourceBuilder.AppendLine(", value)) return false;");

                    // フィールドの変更と変更前後通知を行う処理
                    WriteFieldChangeSection();
                }
                EndBlock();

                return;



                string ToAccessibilityToken(DeclareState declareState)
                {
                    switch (declareState)
                    {
                        case DeclareState.Public: return "public";
                        case DeclareState.Protected: return "protected";
                        case DeclareState.Internal: return "internal";
                        case DeclareState.InternalProrected: return "internal protected";
                        case DeclareState.Private: return "private";
                        default: throw new InvalidOperationException();
                    }
                }
                
                void WriteFieldChangeSection()
                {
                    if (_sourceBuildInputs.ChangingEventDeclareState != DeclareState.None)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                        _sourceBuilder.AppendLine("Changing?.Invoke(this, global::System.EventArgs.Empty);");
                    }

                    if (_sourceBuildInputs.EnabledNotifyPropertyChanging)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append("this.PropertyChanging?.Invoke(this, ");
                        _sourceBuilder.Append(changingEventArgFieldName);
                        _sourceBuilder.AppendLine(");");
                    }

                    PutIndentSpace();
                    _sourceBuilder.Append(fieldName);
                    _sourceBuilder.AppendLine(" = value;");

                    if (_sourceBuildInputs.ChangedEventDeclareState != DeclareState.None)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append(_sourceBuildInputs.PropertyName);
                        _sourceBuilder.AppendLine("Changed?.Invoke(this, global::System.EventArgs.Empty);");
                    }

                    if (_sourceBuildInputs.EnabledNotifyPropertyChanged)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append("this.PropertyChanged?.Invoke(this, ");
                        _sourceBuilder.Append(changedEventArgFieldName);
                        _sourceBuilder.AppendLine(");");
                    }

                    PutIndentSpace();
                    _sourceBuilder.AppendLine("return true;");
                }
            }
        }
    }
}
