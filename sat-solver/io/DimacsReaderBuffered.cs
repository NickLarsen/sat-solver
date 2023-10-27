using System.Runtime.CompilerServices;

namespace sat_solver.io;

public class DimacsReaderBuffered : IDimacsReader
{
    private const byte COMMENT_LINE_STARTER = (byte)'c';
    private const byte PROBLEM_LINE_STARTER = (byte)'p';
    private const byte CARRIAGE_RETURN = (byte)'\r';
    private const byte NEW_LINE = (byte)'\n';


    public int LiteralCount => _literalCount;
    public int ClauseCount => _clauseCount;

    // this buffer is used to return clauses
    // this impl arbitrarily limits the max number of clauses to what this data structure can support
    private readonly List<int> _buffer = new List<int>();
    private int _literalCount;
    private int _clauseCount;
    private byte _current;
    private readonly byte[] _fileContents;
    private int _fileContentsIndex = 0;

    public DimacsReaderBuffered(FileInfo fileInfo)
    {
        _fileContents = File.ReadAllBytes(fileInfo.FullName);
        if (_fileContents.Length == 0) throw new InvalidDataException("read zero bytes from the file");
    }

    public (int literalCount, int clauseCount) ReadHeader()
    {
        while (true)
        {
            ReadNextByte();
            if (IsEOF())
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

    public IReadOnlyList<int>? ReadNextClause()
    {
        if (_current == CARRIAGE_RETURN)
            ReadNextByte();
        if (_current == NEW_LINE) {
            if (IsEOF())
                return null;
            ReadNextByte();
        }
        _buffer.Clear();
        while(true) {
            int value = ReadInt();
            if (value == 0)
            {
                if (_current == CARRIAGE_RETURN)
                    ReadNextByte();
                if (_current != NEW_LINE)
                    throw new InvalidDataException("expected to find end of line after end of clause");
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
        _current = _fileContents[_fileContentsIndex];
        _fileContentsIndex++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEOF()
    {
        return _fileContentsIndex >= _fileContents.Length;
    }

    private void ReadComment()
    {
        if (_current != COMMENT_LINE_STARTER)
            throw new InvalidOperationException("ReadComment called but not currently at start of comment line");
        // just skipping comments
        while(_current != NEW_LINE) {
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
        if (_current == CARRIAGE_RETURN)
            ReadNextByte();
        if (_current != NEW_LINE)
            throw new InvalidDataException("reading problem did not encounter a new line immediately following the clause count in the problem starter");
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