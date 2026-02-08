using KRedis.Resp;

namespace KRedis.Commands;

public sealed class EchoCommand(IReadOnlyList<RespValue> items) : ICommand
{
    public RespValue Execute()
    {
        if (items.Count < 2 || items[1] is not RespValue.BulkString msg || msg.IsNull)
            return new RespValue.SimpleError("ERR wrong number of arguments for 'echo' command");

        return msg;
    }
}