using KRedis.Resp;

namespace KRedis.Commands;

public sealed class EchoCommand(ReadOnlyMemory<RespValue> items) : ICommand
{
    public RespValue Execute()
    {
        if (items.Length != 1 || items.Span[0] is not RespValue.BulkString msg || msg.IsNull)
            return new RespValue.SimpleError("ERR wrong number of arguments for 'echo' command");

        return msg;
    }
}