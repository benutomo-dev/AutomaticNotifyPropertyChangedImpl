using Microsoft.CodeAnalysis;
using System.Buffers;

namespace Benutomo.AutomaticNotifyPropertyChangedImpl.SourceGenerator
{
    ref struct SourceBuilder
    {
        public string SourceText => _chachedSourceText ??= _buffer.Slice(0, _length).ToString();

        string? _chachedSourceText;

        Span<char> _buffer;

        int _length;

        char[]? _arrayPoolBuffer;

        public SourceProductionContext Context { get; }

        int _currentIndentCount = 0;

        const string IndentText = "    ";

        public SourceBuilder(SourceProductionContext context, Span<char> initialBuffer)
        {
            Context = context;
            _buffer = initialBuffer;

            _chachedSourceText = null;
            _length = 0;
            _arrayPoolBuffer = null;
        }

        public void Dispose()
        {
            if (_arrayPoolBuffer is not null)
            {
                _length = 0;
                _buffer = Span<char>.Empty;
                ArrayPool<char>.Shared.Return(_arrayPoolBuffer);
                _arrayPoolBuffer = null;
            }
        }

        void ExpandBuffer(int requiredSize)
        {
            var nextBuffer = ArrayPool<char>.Shared.Rent(_buffer.Length + requiredSize);

            _buffer.CopyTo(nextBuffer.AsSpan());
            _buffer = nextBuffer;

            if (_arrayPoolBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(_arrayPoolBuffer);
            }

            _arrayPoolBuffer = nextBuffer;
        }

        void InternalClear()
        {
            _length = 0;
        }

        void InternalAppend(string text)
        {
            if (_length + text.Length < _buffer.Length)
            {
                ExpandBuffer(text.Length);
            }

            text.AsSpan().CopyTo(_buffer.Slice(_length));
            _length += text.Length;
        }

        public void PutIndentSpace()
        {
            _chachedSourceText = null;

            for (int i = 0; i < _currentIndentCount; i++)
            {
                InternalAppend(IndentText);
            }
        }

        public void Clear()
        {
            _chachedSourceText = null;

            InternalClear();
        }

        public void Append(string text)
        {
            _chachedSourceText = null;

            InternalAppend(text);
        }

        public void AppendLine(string text)
        {
            InternalAppend(text); InternalAppend(Environment.NewLine);
        }

        public void AppendLine()
        {
            InternalAppend(Environment.NewLine);
        }

        public void BeginTryBlock()
        {
            BeginBlock("try");
        }

        public void BeginFinallyBlock()
        {
            BeginBlock("finally");
        }

        public void BeginBlock(string blockHeadLine)
        {
            Context.CancellationToken.ThrowIfCancellationRequested();

            PutIndentSpace(); InternalAppend(blockHeadLine); InternalAppend(Environment.NewLine);
            BeginBlock();
        }

        public void BeginBlock()
        {
            Context.CancellationToken.ThrowIfCancellationRequested();

            PutIndentSpace(); InternalAppend("{"); InternalAppend(Environment.NewLine);
            _currentIndentCount++;
        }

        public void EndBlock()
        {
            Context.CancellationToken.ThrowIfCancellationRequested();

            _currentIndentCount--;
            PutIndentSpace(); InternalAppend("}"); InternalAppend(Environment.NewLine);
        }
    }

}
