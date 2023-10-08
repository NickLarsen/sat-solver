using System.Runtime.CompilerServices;

namespace sat_solver.io;

public class DimacsReader : IDimacsReader, IAsyncDisposable
{
    private const int EOF = -1;
    private const int COMMENT_LINE_STARTER = 'c';
    private const int PROBLEM_LINE_STARTER = 'p';

    private Stream _fileStream;
    // this buffer is used to return clauses
    // this impl arbitrarily limits the max number of clauses to what this data structure can support
    private List<int> _buffer = new List<int>();
    private int _literalCount;
    private int _clauseCount;
    private int _current;

    public DimacsReader(FileInfo fileInfo)
    {
        _fileStream = File.OpenRead(fileInfo.FullName);
    }

    public ValueTask DisposeAsync()
    {
        if (_fileStream != null)
        {
            return _fileStream.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<int>? ReadNextClause()
    {
        if (_current == EOF)
            return null;
        _buffer.Clear();
        while(true) {
            int value = ReadInt();
            if (value == 0)
            {
                if (_current == '\r')
                    ReadNextByte();
                if (_current != '\n')
                    throw new InvalidDataException("expected to find end of line after end of clause");
                // we want to move to the start of the next line for the next call
                ReadNextByte();
                break;
            }
            if (_current == ' ')
            {
                _buffer.Add(value);
                ReadNextByte();
            } else {
                throw new InvalidDataException("expected to find space immediately following clause literal");
            }
        }
        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadNextByte()
    {
        _current = _fileStream.ReadByte();
        //Console.WriteLine($"{_current} '{Convert.ToChar(_current)}'");
    }

    public (int literalCount, int clauseCount) ReadHeader()
    {
        while (true)
        {
            ReadNextByte();
            if (_current == EOF)
                throw new InvalidDataException("unexpected end of file encountered while reading header");
            if (_current == COMMENT_LINE_STARTER)
                ReadComment();
            else if (_current == PROBLEM_LINE_STARTER)
            {
                ReadProblem();
                break;
            }
            else
                throw new InvalidDataException($"unexpected value encountered while reading header '{_current}'");
        }
        return (_literalCount, _clauseCount);
    }

    private void ReadComment()
    {
        if (_current != COMMENT_LINE_STARTER)
            throw new InvalidOperationException("ReadComment called but not currently at start of comment line");
        // just skipping comments
        while(_current != '\n') {
            ReadNextByte();
        }
    }

    private void ReadProblem()
    {
        if (_current != PROBLEM_LINE_STARTER)
            throw new InvalidOperationException("ReadProblem called but not current at start of problem line");
        ReadNextByte();
        if (_current != ' ')
            throw new InvalidDataException("reading problem did not encounter a space immediately following the problem starter");
        ReadNextByte();
        if (_current != 'c')
            throw new InvalidDataException("reading problem did not find 'cnf' following the problem starter");
        ReadNextByte();
        if (_current != 'n')
            throw new InvalidDataException("reading problem did not find 'cnf' following the problem starter");
        ReadNextByte();
        if (_current != 'f')
            throw new InvalidDataException("reading problem did not find 'cnf' following the problem starter");
        ReadNextByte();
        if (_current != ' ')
            throw new InvalidDataException("reading problem did not encounter a space immediately following the 'cnf' declaration in the problem starter");
        ReadNextByte();
        _literalCount = ReadInt();
        _buffer.Capacity = _literalCount;
        if (_current != ' ')
            throw new InvalidDataException("reading problem did not encounter a space immediately following the literal count in the problem starter");
        ReadNextByte();
        _clauseCount = ReadInt();
        if (_current == '\r')
            ReadNextByte();
        if (_current != '\n')
            throw new InvalidDataException("reading problem did not encounter a new line immediately following the clause count in the problem starter");
        // we want to start on the next line
        ReadNextByte();
        if (_current == EOF && _clauseCount > 0)
            throw new InvalidDataException("encountered unexpected end of file after parsing problem starter");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadInt()
    {
        int value = 0;
        int sign = 1;
        if (_current == '-')
        {
            sign = -1;
            ReadNextByte();
        }
        if (!IsDigit(_current))
            throw new InvalidDataException($"expected to find a digit when reading integer but found '{_current}' instead");
        while(IsDigit(_current)) {
            value *= 10;
            value += _current - '0';
            ReadNextByte();
        }
        return sign * value;
    }

    private bool IsDigit(int value)
    {
        return value >= '0' && value <= '9';
    }
}