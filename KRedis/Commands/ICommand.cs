using KRedis.Resp;

namespace KRedis.Commands;

public interface ICommand
{
    RespValue Execute();
}