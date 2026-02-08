using KRedis.Resp;

namespace KRedis.Commands;

public static class CommandFactory
{
    public static ICommand? TryCreateCommand(IReadOnlyList<RespValue> items, out RespValue error)
    {
        error = null!;
        if (items.Count == 0)
        {
            error = new RespValue.SimpleError("ERR empty command");
            return null;
        }

        if (items[0] is not RespValue.BulkString cmd || cmd.IsNull)
        {
            error = new RespValue.SimpleError("ERR protocol error");
            return null;
        }

        string name = cmd.Value.ToUpperInvariant();

        return name switch
        {
            "PING" => new PingCommand(),
            "ECHO" => new EchoCommand(items),
            "GET" => new GetCommand(items),
            "SET" => new SetCommand(items),
            _ => null
        };
    }
}