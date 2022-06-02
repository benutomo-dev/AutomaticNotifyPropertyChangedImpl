﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    public partial class AutomaticGenerator
    {
        class SourceBuilder
        {
#if DEBUG
            public string HintName => $"gen_{string.Join(".", _hintingTypeNames)}.{_propertySymbol.Name}_{string.Join(".", _nameSpaceNames)}.cs";
#else
            public string HintName => $"gen_{string.Join(".", _hintingTypeNames)}.{_propertySymbol.Name}_{string.Join(".", _nameSpaceNames)}.g.cs";
#endif

            public string SourceText => _sourceBuilder.ToString();


            readonly SourceProductionContext _context;

            readonly IPropertySymbol _propertySymbol;

            readonly bool _isEventArgsOnly;

            readonly bool _enabledNotifyPropertyChanging;

            readonly bool _enabledNotifyPropertyChanged;

            readonly DeclareState _changedEventDeclareState;

            readonly DeclareState _changingEventDeclareState;

            readonly DeclareState _changedObservableDeclareState;

            readonly DeclareState _changingObservableDeclareState;

            readonly List<string> _hintingTypeNames = new List<string>();

            readonly List<string> _nameSpaceNames = new List<string>();

            readonly StringBuilder _sourceBuilder = new StringBuilder(4000);

            int _currentIndentCount = 0;

            const string indentText = "    ";

            enum DeclareState
            {
                None,
                Private,
                Public,
                Protected,
                Internal,
                InternalProrected,
            }

            public SourceBuilder(SourceProductionContext context, IPropertySymbol propertySymbol, UsingSymbols usingSymbols, AttributeData enableNotificationSupportAttributeData)
            {
                _context = context;
                _propertySymbol = propertySymbol;

                _isEventArgsOnly = enableNotificationSupportAttributeData.NamedArguments.Where(v => v.Key == "EventArgsOnly").Select(v => (bool)(v.Value.Value ?? false)).FirstOrDefault();

                _enabledNotifyPropertyChanging = propertySymbol.ContainingType.AllInterfaces.Any(v => SymbolEqualityComparer.Default.Equals(v, usingSymbols.NotifyPropertyChanging));
                _enabledNotifyPropertyChanged = propertySymbol.ContainingType.AllInterfaces.Any(v => SymbolEqualityComparer.Default.Equals(v, usingSymbols.NotifyPropertyChanged));

                foreach (var attributeData in propertySymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangedEvent))
                    {
                        _changedEventDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangingEvent))
                    {
                        _changingEventDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChangedObservable))
                    {
                        _changedObservableDeclareState = GetDeclareState(attributeData);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, usingSymbols.ChantingObservable))
                    {
                        _changingObservableDeclareState = GetDeclareState(attributeData);
                    }
                }

                if (_changedObservableDeclareState != DeclareState.None && _changedEventDeclareState == DeclareState.None)
                {
                    // Observableはイベントを変換する形で実装するのでprivateでイベントを用意する。
                    _changedEventDeclareState = DeclareState.Private;
                }

                if (_changingObservableDeclareState != DeclareState.None && _changingEventDeclareState == DeclareState.None)
                {
                    // Observableはイベントを変換する形で実装するのでprivateでイベントを用意する。
                    _changingEventDeclareState = DeclareState.Private;
                }

                static DeclareState GetDeclareState(AttributeData attributeData)
                {
                    if (attributeData.ConstructorArguments.Length == 0) return DeclareState.None;

                    if (attributeData.ConstructorArguments.Length > 1) throw new InvalidOperationException();

                    return attributeData.ConstructorArguments[0].Value switch
                    {
                        NotificationAccessibilityPublic => DeclareState.Public,
                        NotificationAccessibilityInternal => DeclareState.Internal,
                        NotificationAccessibilityProtected => DeclareState.Protected,
                        NotificationAccessibilityInternalProtected => DeclareState.InternalProrected,
                        _ => DeclareState.None,
                    };
                }
            }

            public void Build()
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                _hintingTypeNames.Clear();
                _nameSpaceNames.Clear();
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
                WriteContainingTypeStart(_propertySymbol.ContainingType, isDesingationType: true);

                return;

                void WriteContainingTypeStart(INamedTypeSymbol namedTypeSymbol, bool isDesingationType)
                {
                    if (namedTypeSymbol.ContainingType is not null)
                    {
                        WriteContainingTypeStart(namedTypeSymbol.ContainingType, false);
                    }
                    else if (namedTypeSymbol.ContainingNamespace is not null)
                    {
                        WriteContainingNameSpaceStart(namedTypeSymbol.ContainingNamespace);
                    }

                    _context.CancellationToken.ThrowIfCancellationRequested();

                    PutIndentSpace();
                    _sourceBuilder.Append("partial ");
                    _sourceBuilder.Append(namedTypeSymbol.IsValueType ? "struct " : "class ");
                    _sourceBuilder.Append(namedTypeSymbol.Name);

                    if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Length > 0)
                    {
                        _sourceBuilder.Append("<");
                        _sourceBuilder.Append(string.Join(", ", namedTypeSymbol.TypeArguments.Select(v => v.Name)));
                        _sourceBuilder.Append(">");

                        var hintingTypeNameBuilder = new StringBuilder();

                        hintingTypeNameBuilder.Append(namedTypeSymbol.Name);
                        hintingTypeNameBuilder.Append("{");
                        hintingTypeNameBuilder.Append(string.Join("_", namedTypeSymbol.TypeArguments.Select(v => v.Name)));
                        hintingTypeNameBuilder.Append("}");
                        _hintingTypeNames.Add(hintingTypeNameBuilder.ToString());
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

                void WriteContainingNameSpaceStart(INamespaceSymbol namespaceSymbol)
                {
                    if (namespaceSymbol.IsGlobalNamespace) return;

                    WriteContainingNameSpaceStart(namespaceSymbol.ContainingNamespace);

                    _context.CancellationToken.ThrowIfCancellationRequested();

                    PutIndentSpace();
                    _sourceBuilder.Append("namespace ");
                    _sourceBuilder.Append(namespaceSymbol.Name);
                    _sourceBuilder.AppendLine("");

                    BeginBlock();


                    _nameSpaceNames.Add(namespaceSymbol.Name);
                }
            }

            void WriteTypeDeclarationEnd()
            {
                WriteContainingTypeEnd(_propertySymbol.ContainingType);

                return;

                void WriteContainingTypeEnd(INamedTypeSymbol namedTypeSymbol)
                {
                    EndBlock();

                    _context.CancellationToken.ThrowIfCancellationRequested();

                    if (namedTypeSymbol.ContainingType is not null)
                    {
                        WriteContainingTypeEnd(namedTypeSymbol.ContainingType);
                    }
                    else if (namedTypeSymbol.ContainingNamespace is not null)
                    {
                        WriteContainingNameSpaceEnd(namedTypeSymbol.ContainingNamespace);
                    }
                }

                void WriteContainingNameSpaceEnd(INamespaceSymbol namespaceSymbol)
                {
                    if (namespaceSymbol.IsGlobalNamespace) return;

                    WriteContainingNameSpaceEnd(namespaceSymbol.ContainingNamespace);

                    _context.CancellationToken.ThrowIfCancellationRequested();

                    EndBlock();
                }
            }

            void WriteBody()
            {
                var property = _propertySymbol;

                _context.CancellationToken.ThrowIfCancellationRequested();

                _sourceBuilder.AppendLine();

                var changedEventArgFieldName = $"__PropertyChangedEventArgs_{property.Name}";
                var changingEventArgFieldName = $"__PropertyChangingEventArgs_{property.Name}";

                if (_enabledNotifyPropertyChanging)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("private static global::System.ComponentModel.PropertyChangingEventArgs ");
                    _sourceBuilder.Append(changingEventArgFieldName);
                    _sourceBuilder.Append(" = new global::System.ComponentModel.PropertyChangingEventArgs(nameof(");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("));");
                }

                if (_enabledNotifyPropertyChanged)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append("private static global::System.ComponentModel.PropertyChangedEventArgs ");
                    _sourceBuilder.Append(changedEventArgFieldName);
                    _sourceBuilder.Append(" = new global::System.ComponentModel.PropertyChangedEventArgs(nameof(");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("));");
                }

                if (_isEventArgsOnly)
                {
                    return;
                }

                var fieldName = $"__{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}";

                var methodName = $"_{property.Name}";


                if (_changingEventDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_changingEventDeclareState));
                    _sourceBuilder.Append(" event global::System.EventHandler ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("Changing;");
                }

                if (_changedEventDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_changedEventDeclareState));
                    _sourceBuilder.Append(" event global::System.EventHandler ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("Changed;");
                }

                if (_changingObservableDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_changingObservableDeclareState));
                    _sourceBuilder.Append(" global::System.IObservable<object?> ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.Append("ChangingAsObservable() =>  new global::Benutomo.Internal.EventToObservable(h => ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.Append("Changing += h, h => ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("Changing -= h);");
                }

                if (_changedObservableDeclareState != DeclareState.None)
                {
                    PutIndentSpace();
                    _sourceBuilder.Append(ToAccessibilityToken(_changedObservableDeclareState));
                    _sourceBuilder.Append(" global::System.IObservable<object?> ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.Append("ChangedAsObservable() =>  new global::Benutomo.Internal.EventToObservable(h => ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.Append("Changed += h, h => ");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("Changed -= h);");
                }

                PutIndentSpace();
                _sourceBuilder.Append("private ");
                AppendFullTypeName(property.Type);
                _sourceBuilder.Append(" ");
                _sourceBuilder.Append(fieldName);
                _sourceBuilder.AppendLine(";");

                PutIndentSpace();
                _sourceBuilder.Append("private ");
                AppendFullTypeName(property.Type);
                _sourceBuilder.Append(" ");
                _sourceBuilder.Append(methodName);
                _sourceBuilder.Append("() => this.");
                _sourceBuilder.Append(fieldName);
                _sourceBuilder.AppendLine(";");

                if (property.Type.IsReferenceType && property.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
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
                AppendFullTypeName(property.Type);
                _sourceBuilder.AppendLine(" value)");
                BeginBlock();
                {
                    if (property.Type.IsReferenceType)
                    {
                        if (property.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            PutIndentSpace();
                            _sourceBuilder.AppendLine("if (value is null) throw new ArgumentNullException(nameof(value));");
                        }
                        else if (property.Type.NullableAnnotation == NullableAnnotation.None)
                        {
                            var descripter = new DiagnosticDescriptor("SGN001", "Nullable context is not enabled.", "Set the Nullable property to enable in the project file or set #nullable enable in the source code.", "code", DiagnosticSeverity.Warning, isEnabledByDefault: true);

                            foreach (var declaration in property.DeclaringSyntaxReferences)
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
                        AppendFullTypeName(property.Type);
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
                AppendFullTypeName(property.Type);
                _sourceBuilder.Append(" value, global::System.Collections.Generic.IEqualityComparer<");
                AppendFullTypeName(property.Type);
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


                void AppendFullTypeName(ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.ContainingType is null)
                    {
                        AppendFullNamespace(typeSymbol.ContainingNamespace);
                    }
                    else
                    {
                        AppendFullTypeName(typeSymbol.ContainingType);
                        _sourceBuilder.Append(".");
                    }

                    _sourceBuilder.Append(typeSymbol.Name);

                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol && !namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
                    {


                        var typeArguments = namedTypeSymbol.TypeArguments;

                        _sourceBuilder.Append("<");

                        for (int i = 0; i < typeArguments.Length - 1; i++)
                        {
                            AppendFullTypeName(typeArguments[i]);
                            _sourceBuilder.Append(", ");
                        }
                        AppendFullTypeName(typeArguments[typeArguments.Length - 1]);

                        _sourceBuilder.Append(">");
                    }

                    if (typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        _sourceBuilder.Append("?");
                    }
                }

                void AppendFullNamespace(INamespaceSymbol namespaceSymbol)
                {
                    if (namespaceSymbol.IsGlobalNamespace)
                    {
                        _sourceBuilder.Append("global::");
                        return;
                    }

                    if (namespaceSymbol.ContainingNamespace is not null)
                    {
                        AppendFullNamespace(namespaceSymbol.ContainingNamespace);
                    }

                    _sourceBuilder.Append(namespaceSymbol.Name);
                    _sourceBuilder.Append(".");
                }

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
                    if (_changingEventDeclareState != DeclareState.None)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append(property.Name);
                        _sourceBuilder.AppendLine("Changing?.Invoke(this, global::System.EventArgs.Empty);");
                    }

                    if (_enabledNotifyPropertyChanging)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append("this.PropertyChanging?.Invoke(this, ");
                        _sourceBuilder.Append(changingEventArgFieldName);
                        _sourceBuilder.AppendLine(");");
                    }

                    PutIndentSpace();
                    _sourceBuilder.Append(fieldName);
                    _sourceBuilder.AppendLine(" = value;");

                    if (_changedEventDeclareState != DeclareState.None)
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append(property.Name);
                        _sourceBuilder.AppendLine("Changed?.Invoke(this, global::System.EventArgs.Empty);");
                    }

                    if (_enabledNotifyPropertyChanged)
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
