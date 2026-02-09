using KRedis.Resp;

namespace KRedis.Commands;

public static class CommandFactory
{
    public static ICommand? TryCreateCommand(ReadOnlyMemory<RespValue> items, out RespValue error)
    {
        error = null!;
        if (items.Length == 0)
        {
            error = new RespValue.SimpleError("ERR empty command");
            return null;
        }

        if (items.Span[0] is not RespValue.BulkString cmd || cmd.IsNull)
        {
            error = new RespValue.SimpleError("ERR protocol error");
            return null;
        }

        string name = cmd.Value.ToUpperInvariant();

        return name switch
        {
            "PING" => new PingCommand(),
            "ECHO" => new EchoCommand(items[1..]),
            "GET" => new GetCommand(items[1..]),
            "SET" => new SetCommand(items[1..]),
            _ => null
        };
    }
}