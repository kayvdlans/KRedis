using KRedis.Data;
using KRedis.Resp;

namespace KRedis.Commands;

public sealed class GetCommand(IReadOnlyList<RespValue> items) : ICommand
{
    public RespValue Execute()
    {
        if (items.Count < 2 || items[1] is not RespValue.BulkString getKey || getKey.IsNull)
            return new RespValue.SimpleError("ERR wrong number of arguments for 'get' command");

        return Database.Get(getKey.Value);
    }
}