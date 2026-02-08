using KRedis.Data;
using KRedis.Resp;

namespace KRedis.Commands;

public sealed class SetCommand(IReadOnlyList<RespValue> items) : ICommand
{
    public RespValue Execute()
    {
        if (items.Count < 3 ||
            items[1] is not RespValue.BulkString setKey || setKey.IsNull ||
            items[2] is not RespValue.BulkString value || value.IsNull)
            return new RespValue.SimpleError("ERR wrong number of arguments for 'set' command");

        DateTime? expireAt = null;
        for (var i = 3; i < items.Count; i++)
        {
            if (items[i] is not RespValue.BulkString arg || arg.IsNull)
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
        value = 0;
        return index + 1 < items.Count
            && items[index + 1] is RespValue.BulkString str
            && !str.IsNull
            && long.TryParse(str.Value, out value);
    }
}
