using KRedis.Resp;

namespace KRedis.Data;

public static class Database
{
    private static readonly Dictionary<string, RedisValue> Store = [];

    public static RespValue Get(string key)
    {
        if (!Store.TryGetValue(key, out RedisValue? val))
            return new RespValue.BulkString([], true);

        if (val.ExpireAt is null || val.ExpireAt > DateTime.Now)
            return new RespValue.BulkString(val.Data);

        Store.Remove(key);
        return new RespValue.BulkString([], true);
    }

    public static RespValue Set(string key, byte[] value, DateTime? expireAt = null)
    {
        Store[key] = new RedisValue(RedisValueType.String, value, expireAt);
        return new RespValue.SimpleString("OK");
    }
}