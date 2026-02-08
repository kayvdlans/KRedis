using System.Text;

namespace KRedis.Resp;

public static class RespParser
{
    public static bool TryParseAny(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;
        consumed = 0;

        if (buffer.Length == 0)
            return false;

        return buffer[0] switch
        {
            (byte)'$' => TryParseBulkString(buffer, out value, out consumed),
            (byte)'+' => TryParseSimpleString(buffer, out value, out consumed),
            (byte)'-' => TryParseSimpleError(buffer, out value, out consumed),
            (byte)':' => TryParseInteger(buffer, out value, out consumed),
            (byte)'*' => TryParseArray(buffer, out value, out consumed),
            _ => throw new RespParserException("Unknown RESP prefix")
        };
    }

    private static bool TryParseBulkString(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;
        consumed = 0;

        if (!TryReadLine(buffer, out ReadOnlySpan<byte> line, out int headerConsumed))
            return false;

        int length = ParseInt(line[1..]);
        if (length == -1)
        {
            consumed = headerConsumed;
            value = new RespValue.BulkString([], true);
            return true;
        }

        if (buffer.Length < headerConsumed + length + 2)
            return false;

        if (buffer[headerConsumed + length] != (byte)'\r' || buffer[headerConsumed + length + 1] != (byte)'\n')
            throw new RespParserException("Invalid RESP bulk string");

        consumed = headerConsumed + length + 2;
        value = new RespValue.BulkString(buffer.Slice(headerConsumed, length).ToArray());
        return true;
    }

    private static bool TryParseSimpleString(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;

        if (!TryReadLine(buffer, out ReadOnlySpan<byte> line, out consumed))
            return false;

        if (line.Length < 1 || line[0] != (byte)'+')
            throw new RespParserException("Invalid RESP simple string");

        string text = Encoding.ASCII.GetString(line[1..]);
        value = new RespValue.SimpleString(text);
        return true;
    }

    private static bool TryParseSimpleError(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;

        if (!TryReadLine(buffer, out ReadOnlySpan<byte> line, out consumed))
            return false;

        if (line.Length < 1 || line[0] != (byte)'-')
            throw new RespParserException("Invalid RESP simple error");

        string message = Encoding.ASCII.GetString(line[1..]);
        value = new RespValue.SimpleError(message);
        return true;
    }

    private static bool TryParseInteger(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;

        if (!TryReadLine(buffer, out ReadOnlySpan<byte> line, out consumed))
            return false;

        if (line.Length < 2 || line[0] != (byte)':')
            throw new RespParserException("Invalid RESP integer");

        long n = ParseLong(line[1..]);
        value = new RespValue.Integer(n);
        return true;
    }

    private static bool TryParseArray(ReadOnlySpan<byte> buffer, out RespValue value, out int consumed)
    {
        value = null!;
        consumed = 0;

        if (!TryReadLine(buffer, out ReadOnlySpan<byte> line, out int headerConsumed))
            return false;

        if (line.Length < 2 || line[0] != (byte)'*')
            throw new RespParserException("Invalid RESP array");

        int count = ParseInt(line[1..]);
        if (count == -1)
        {
            consumed = headerConsumed;
            value = new RespValue.Array([], true);
            return true;
        }

        int offset = headerConsumed;
        var items = new RespValue[count];

        for (var i = 0; i < count; i++)
        {
            if (offset >= buffer.Length)
                return false;

            if (!TryParseAny(buffer[offset..], out items[i], out int elementConsumed))
                return false;

            offset += elementConsumed;
        }

        consumed = offset;
        value = new RespValue.Array(items);
        return true;
    }

    private static bool TryReadLine(ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> line, out int consumed)
    {
        for (var i = 0; i + 1 < buffer.Length; i++)
        {
            if (buffer[i] != (byte)'\r' || buffer[i + 1] != (byte)'\n')
                continue;

            line = buffer[..i];
            consumed = i + 2;
            return true;
        }

        line = default;
        consumed = 0;
        return false;
    }

    // TODO: We can also just use the bytes, to convert, rather than first converting to string. 
    private static int ParseInt(ReadOnlySpan<byte> line)
    {
        string decoded = Encoding.ASCII.GetString(line);
        return int.TryParse(decoded, out int len)
            ? len
            : throw new RespParserException($"Invalid value for an integer: {decoded}");
    }

    // TODO: We can also just use the bytes, to convert, rather than first converting to string. 
    private static long ParseLong(ReadOnlySpan<byte> line)
    {
        string decoded = Encoding.ASCII.GetString(line);
        return long.TryParse(decoded, out long len)
            ? len
            : throw new RespParserException($"Invalid value for an long: {decoded}");
    }
}