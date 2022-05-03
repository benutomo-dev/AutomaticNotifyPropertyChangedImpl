using Microsoft.CodeAnalysis;
using System.Text;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    public partial class AutomaticGenerator
    {
        class SourceBuilder
        {
            public string HintName => $"gen_{string.Join(".", _hintingTypeNames)}_{string.Join(".", _nameSpaceNames)}_AutomaticNotifyImpl.cs";

            public string SourceText => _sourceBuilder.ToString();


            readonly GeneratorExecutionContext _context;

            readonly INamedTypeSymbol _classDeclarationSymbol;

            readonly List<IPropertySymbol> _properties;


            readonly List<string> _hintingTypeNames = new List<string>();

            readonly List<string> _nameSpaceNames = new List<string>();

            readonly StringBuilder _sourceBuilder = new StringBuilder(4000);

            int _currentIndentCount = 0;

            const string indentText = "    ";

            public SourceBuilder(GeneratorExecutionContext context, INamedTypeSymbol classDeclarationSymbol, AttributeData automaticDisposeAttributeData)
            {
                _context = context;
                _classDeclarationSymbol = classDeclarationSymbol;

                _properties = new();

                foreach (var member in classDeclarationSymbol.GetMembers())
                {
                    _context.CancellationToken.ThrowIfCancellationRequested();

                    if (member.IsStatic) continue;

                    if (member is IPropertySymbol propertySymbol)
                    {
                        if (propertySymbol.IsImplicitlyDeclared) continue;

                        _properties.Add(propertySymbol);
                    }
                }
            }

            public void Build()
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                _hintingTypeNames.Clear();
                _nameSpaceNames.Clear();
                _sourceBuilder.Clear();

                _sourceBuilder.AppendLine("#nullable enable");

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
                WriteContainingTypeStart(_classDeclarationSymbol, isDesingationType: true);

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

                        _sourceBuilder.Append(" // This is implementation class for INotifyPropertyChanged by AutomaticNotifyPropertyChangedImpl.");
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
                WriteContainingTypeEnd(_classDeclarationSymbol);

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

            void ﾾadsf()
            {

            }

            void WriteBody()
            {
                foreach (var property in _properties)
                {
                    _sourceBuilder.AppendLine();

                    var eventArgFieldName = $"ﾾ{property.Name}";

                    var fieldName = $"ﾾ_{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}";

                    var methodName = $"_{property.Name}";

                    PutIndentSpace();
                    _sourceBuilder.Append("private static global::System.ComponentModel.PropertyChangedEventArgs ");
                    _sourceBuilder.Append(eventArgFieldName);
                    _sourceBuilder.Append(" = new global::System.ComponentModel.PropertyChangedEventArgs(nameof(");
                    _sourceBuilder.Append(property.Name);
                    _sourceBuilder.AppendLine("));");

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

                    PutIndentSpace();
                    _sourceBuilder.Append("private void ");
                    _sourceBuilder.Append(methodName);
                    _sourceBuilder.Append("(");
                    AppendFullTypeName(property.Type);
                    _sourceBuilder.AppendLine(" value)");
                    BeginBlock();
                    {
                        PutIndentSpace();
                        _sourceBuilder.Append("if (");
                        _sourceBuilder.Append(fieldName);
                        _sourceBuilder.AppendLine(" == value) return;");

                        PutIndentSpace();
                        _sourceBuilder.Append(fieldName);
                        _sourceBuilder.AppendLine(" = value;");

                        PutIndentSpace();
                        _sourceBuilder.Append("this.PropertyChanged?.Invoke(this, ");
                        _sourceBuilder.Append(eventArgFieldName);
                        _sourceBuilder.AppendLine(");");
                    }
                    EndBlock();
                }

                return;


                void AppendFullTypeName(ITypeSymbol typeSymbol)
                {
                    AppendFullNamespace(typeSymbol.ContainingNamespace);
                    _sourceBuilder.Append(typeSymbol.Name);
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
            }
        }
    }
}
