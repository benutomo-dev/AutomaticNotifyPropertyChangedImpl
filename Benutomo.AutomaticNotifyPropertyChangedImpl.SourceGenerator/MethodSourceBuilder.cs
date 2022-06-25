﻿using Microsoft.CodeAnalysis;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    ref struct MethodSourceBuilder
    {
        public string HintName => $"gen_{string.Join(".", _sourceBuilder.HintingTypeNames)}.{_sourceBuildInputs.InternalPropertyName}_{_sourceBuilder.NameSpace}.cs";

        ClassSourceBuilder _sourceBuilder;

        readonly MethodSourceBuildInputs _sourceBuildInputs;


        public MethodSourceBuilder(SourceProductionContext context, MethodSourceBuildInputs sourceBuildInputs, Span<char> initialBuffer)
        {
            _sourceBuildInputs = sourceBuildInputs;
            _sourceBuilder = new ClassSourceBuilder(context, sourceBuildInputs.ContainingTypeInfo, initialBuffer);
        }

        public void Dispose()
        {
            _sourceBuilder.Dispose();
        }

        #region _sourceBuilder Methods
        public SourceProductionContext Context => _sourceBuilder.Context;
        public string SourceText => _sourceBuilder.SourceText;
        public void PutIndentSpace() => _sourceBuilder.PutIndentSpace();
        public void Clear() => _sourceBuilder.Clear();
        public void Append(string text) => _sourceBuilder.Append(text);
        public void AppendLine(string text) => _sourceBuilder.AppendLine(text);
        public void AppendLine() => _sourceBuilder.AppendLine();
        public void BeginTryBlock() => _sourceBuilder.BeginTryBlock();
        public void BeginFinallyBlock() => _sourceBuilder.BeginFinallyBlock();
        public void BeginBlock(string blockHeadLine) => _sourceBuilder.BeginBlock(blockHeadLine);
        public void BeginBlock() => _sourceBuilder.BeginBlock();
        public void EndBlock() => _sourceBuilder.EndBlock();
        public void WriteTypeDeclarationStart() => _sourceBuilder.WriteTypeDeclarationStart();
        public void WriteTypeDeclarationEnd() => _sourceBuilder.WriteTypeDeclarationEnd();
        #endregion

        public void Build()
        {
            _sourceBuilder.Clear();

#if !DEBUG
            AppendLine("// <auto-generated />");
#endif

            AppendLine("#nullable enable");
            AppendLine("#pragma warning disable CS0612 // Obsolete属性でマークされたメソッドの呼び出しに対する警告を抑止");
            AppendLine("#pragma warning disable CS0618 // Obsolete属性でマークされたメソッドの呼び出しに対する警告を抑止");
            AppendLine("#pragma warning disable CS0619 // Obsolete属性でマークされたメソッドの呼び出しに対するエラーを抑止");

            WriteTypeDeclarationStart();

            WriteBody();

            WriteTypeDeclarationEnd();
        }

        void WriteBody()
        {
            string changedEventBaseName;
            if (_sourceBuildInputs.ChangedEventAccessibility == GenerateMemberAccessibility.PrivateForExplicitImplimetOnly)
            {
                changedEventBaseName = _sourceBuildInputs.FieldName;
            }
            else
            {
                changedEventBaseName = _sourceBuildInputs.PropertyName;
            }

            string changingEventBaseName;
            if (_sourceBuildInputs.ChangedEventAccessibility == GenerateMemberAccessibility.PrivateForExplicitImplimetOnly)
            {
                changingEventBaseName = _sourceBuildInputs.FieldName;
            }
            else
            {
                changingEventBaseName = _sourceBuildInputs.PropertyName;
            }


            foreach (var explicitImplementation in _sourceBuildInputs.ExplicitInterfaceImplementations)
            {
                AppendLine();

                string redirectEventHandlerBasename;
                string redirectEventHandlerSuffix;
                switch (explicitImplementation.EventType)
                {
                    case ExplicitImplementationEventType.ChangedEventHandler:
                        redirectEventHandlerBasename = changedEventBaseName;
                        redirectEventHandlerSuffix = "Changed";
                        break;
                    case ExplicitImplementationEventType.ChangingEventHandler:
                        redirectEventHandlerBasename = changingEventBaseName;
                        redirectEventHandlerSuffix = "Changing";
                        break;
                    default:
                        continue;
                }

                PutIndentSpace();
                Append("event global::System.EventHandler? ");
                Append(explicitImplementation.InterfaceType);
                Append(".");
                Append(explicitImplementation.InterfaceEventName);
                Append(" { add => ");
                Append(redirectEventHandlerBasename);
                Append(redirectEventHandlerSuffix);
                Append(" += value; remove => ");
                Append(redirectEventHandlerBasename);
                Append(redirectEventHandlerSuffix);
                AppendLine(" += value;}");
            }

            if (_sourceBuildInputs.ChangingEventAccessibility != GenerateMemberAccessibility.None)
            {
                AppendLine();

                PutIndentSpace();
                Append(ToAccessibilityToken(_sourceBuildInputs.ChangingEventAccessibility));
                Append(" event global::System.EventHandler? ");
                Append(changingEventBaseName);
                AppendLine("Changing;");
            }

            if (_sourceBuildInputs.ChangedEventAccessibility != GenerateMemberAccessibility.None)
            {
                AppendLine();

                PutIndentSpace();
                Append(ToAccessibilityToken(_sourceBuildInputs.ChangedEventAccessibility));
                Append(" event global::System.EventHandler? ");
                Append(changedEventBaseName);
                AppendLine("Changed;");
            }

            if (_sourceBuildInputs.ChangingObservableAccesibility != GenerateMemberAccessibility.None)
            {
                AppendLine();

                PutIndentSpace();
                Append(ToAccessibilityToken(_sourceBuildInputs.ChangingObservableAccesibility));
                Append(" global::System.IObservable<object?> ");
                Append(_sourceBuildInputs.PropertyName);
                Append("ChangingAsObservable() =>  new global::Benutomo.Internal.EventToObservable<object?>(h => ");
                Append(_sourceBuildInputs.PropertyName);
                Append("Changing += h, h => ");
                Append(_sourceBuildInputs.PropertyName);
                AppendLine("Changing -= h, () => this, pushValueAtSubscribed: false);");
            }

            if (_sourceBuildInputs.ChangedObservableAccesibility != GenerateMemberAccessibility.None)
            {
                AppendLine();

                PutIndentSpace();
                Append(ToAccessibilityToken(_sourceBuildInputs.ChangedObservableAccesibility));
                Append(" global::System.IObservable<");
                Append(_sourceBuildInputs.PropertyType);
                Append("> ");
                Append(_sourceBuildInputs.PropertyName);
                Append("ChangedAsObservable(bool pushValueAtSubscribed = false) =>  new global::Benutomo.Internal.EventToObservable<");
                Append(_sourceBuildInputs.PropertyType);
                Append(">(h => ");
                Append(_sourceBuildInputs.PropertyName);
                Append("Changed += h, h => ");
                Append(_sourceBuildInputs.PropertyName);
                Append("Changed -= h, () => ");
                Append(_sourceBuildInputs.FieldName);
                AppendLine(", pushValueAtSubscribed);");
            }

            AppendLine();

            PutIndentSpace();
            Append("private ");
            Append(_sourceBuildInputs.PropertyType);
            Append(" ");
            Append(_sourceBuildInputs.FieldName);
            AppendLine(";");

            AppendLine();

            PutIndentSpace();
            Append("private ");
            Append(_sourceBuildInputs.PropertyType);
            Append(" ");
            Append(_sourceBuildInputs.MethodName);
            Append("() => this.");
            Append(_sourceBuildInputs.FieldName);
            AppendLine(";");

            AppendLine();

            if (_sourceBuildInputs.PropertyTypeIsReferenceType && _sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                PutIndentSpace();
                Append(@"[global::System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(");
                Append(_sourceBuildInputs.FieldName);
                AppendLine(@"))]");
            }
            PutIndentSpace();
            Append("private bool ");
            Append(_sourceBuildInputs.MethodName);
            Append("(");
            Append(_sourceBuildInputs.PropertyType);
            AppendLine(" value)");
            BeginBlock();
            {
                if (_sourceBuildInputs.PropertyTypeIsReferenceType)
                {
                    if (_sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.NotAnnotated)
                    {
                        PutIndentSpace();
                        AppendLine("if (value is null) throw new ArgumentNullException(nameof(value));");
                    }
                    else if (_sourceBuildInputs.PropertyTypeNullableAnnotation == NullableAnnotation.None)
                    {
                        var descripter = new DiagnosticDescriptor("SGN001", "Nullable context is not enabled.", "Set the Nullable property to enable in the project file or set #nullable enable in the source code.", "code", DiagnosticSeverity.Warning, isEnabledByDefault: true);

                        foreach (var declaration in _sourceBuildInputs.PropertyDeclaringSyntaxReferences)
                        {
                            Context.ReportDiagnostic(Diagnostic.Create(descripter, declaration.GetSyntax(Context.CancellationToken).GetLocation()));
                        }
                    }

                    PutIndentSpace();
                    Append("if (object.ReferenceEquals(");
                    Append(_sourceBuildInputs.FieldName);
                    AppendLine(", value)) return false;");
                }
                else
                {
                    PutIndentSpace();
                    Append("if (global::System.Collections.Generic.EqualityComparer<");
                    Append(_sourceBuildInputs.PropertyType);
                    Append(">.Default.Equals(");
                    Append(_sourceBuildInputs.FieldName);
                    AppendLine(", value)) return false;");
                }

                AppendLine();

                // フィールドの変更と変更前後通知を行う処理
                WriteFieldChangeSection(changingEventBaseName, changedEventBaseName);
            }
            EndBlock();

            AppendLine();

            PutIndentSpace();
            Append("private bool ");
            Append(_sourceBuildInputs.MethodName);
            Append("(");
            Append(_sourceBuildInputs.PropertyType);
            Append(" value, global::System.Collections.Generic.IEqualityComparer<");
            Append(_sourceBuildInputs.PropertyType);
            AppendLine("> equalityComparer) ");
            BeginBlock();
            {
                PutIndentSpace();
                Append("if (equalityComparer.Equals(");
                Append(_sourceBuildInputs.FieldName);
                AppendLine(", value)) return false;");

                AppendLine();

                // フィールドの変更と変更前後通知を行う処理
                WriteFieldChangeSection(changingEventBaseName, changedEventBaseName);
            }
            EndBlock();

            return;



            static string ToAccessibilityToken(GenerateMemberAccessibility accessibility)
            {
                switch (accessibility)
                {
                    case GenerateMemberAccessibility.Public: return "public";
                    case GenerateMemberAccessibility.Protected: return "protected";
                    case GenerateMemberAccessibility.Internal: return "internal";
                    case GenerateMemberAccessibility.ProrectedInternal: return "protected internal";
                    case GenerateMemberAccessibility.PrivateProrected: return "private protected";
                    case GenerateMemberAccessibility.Private: return "private";
                    case GenerateMemberAccessibility.PrivateForExplicitImplimetOnly: return "private";
                    default: throw new InvalidOperationException();
                }
            }
        }

        void WriteFieldChangeSection(string changingEventBaseName, string changedEventBaseName)
        {
            if (_sourceBuildInputs.ChangingEventAccessibility != GenerateMemberAccessibility.None)
            {
                PutIndentSpace();
                Append(changingEventBaseName);
                AppendLine("Changing?.Invoke(this, global::System.EventArgs.Empty);");
            }

            if (_sourceBuildInputs.EnabledNotifyPropertyChanging)
            {
                var changingEventArgFieldName = $"__PropertyChangingEventArgs_{_sourceBuildInputs.PropertyName}";

                PutIndentSpace();
                Append("this.PropertyChanging?.Invoke(this, ");
                Append(changingEventArgFieldName);
                AppendLine(");");
            }

            PutIndentSpace();
            Append(_sourceBuildInputs.FieldName);
            AppendLine(" = value;");

            if (_sourceBuildInputs.ChangedEventAccessibility != GenerateMemberAccessibility.None)
            {
                PutIndentSpace();
                Append(changedEventBaseName);
                AppendLine("Changed?.Invoke(this, global::System.EventArgs.Empty);");
            }

            if (_sourceBuildInputs.EnabledNotifyPropertyChanged)
            {
                var changedEventArgFieldName = $"__PropertyChangedEventArgs_{_sourceBuildInputs.PropertyName}";

                PutIndentSpace();
                Append("this.PropertyChanged?.Invoke(this, ");
                Append(changedEventArgFieldName);
                AppendLine(");");
            }

            PutIndentSpace();
            AppendLine("return true;");
        }
    }
}
