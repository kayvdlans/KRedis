using KRedis.Resp;

namespace KRedis.Commands;

public sealed class PingCommand : ICommand
{
    public RespValue Execute()
    {
        return new RespValue.SimpleString("PONG");
    }
}