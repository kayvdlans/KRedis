using System.Text;
using KRedis.Data;
using KRedis.Resp;

namespace KRedis.Commands;

public sealed class SetCommand(ReadOnlyMemory<RespValue> items) : ICommand
{
    public RespValue Execute()
    {
        ReadOnlySpan<RespValue> span = items.Span;
        if (span.Length < 2 ||
            span[0] is not RespValue.BulkString setKey || setKey.IsNull ||
            span[1] is not RespValue.BulkString value || value.IsNull)
            return new RespValue.SimpleError("ERR wrong number of arguments for 'set' command");

        DateTime? expireAt = null;
        for (var i = 2; i < span.Length; i++)
        {
            if (span[i] is not RespValue.BulkString arg || arg.IsNull)
                return new RespValue.SimpleError("ERR invalid argument for 'set' command");

            switch (arg.Value.ToUpperInvariant())
            {
                case "EX":
                    if (!TryGetArgAsLong(i + 1, out long seconds))
                        return new RespValue.SimpleError("ERR invalid argument for 'set' command");

                    expireAt = DateTime.UtcNow.AddSeconds(seconds);
                    break;

                case "PX":
                    if (!TryGetArgAsLong(i + 1, out long millis))
                        return new RespValue.SimpleError("ERR invalid argument for 'set' command");

                    expireAt = DateTime.UtcNow.AddMilliseconds(millis);
                    break;

                default:
                    return new RespValue.SimpleError("ERR invalid argument for 'set' command");
            }

            i++;
        }

        return Database.Set(setKey.Value, value.Data, expireAt);
    }

    private bool TryGetArgAsLong(int index, out long value)
    {
        value = default;
        return index < items.Length
               && items.Span[index] is RespValue.BulkString str
               && !str.IsNull
               && long.TryParse(str.Value, out value);
    }
}