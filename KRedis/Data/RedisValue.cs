namespace KRedis.Data;

public class RedisValue
{
    private RedisValue(RedisValueType type, DateTime? expireAt)
    {
        Type = type;
        ExpireAt = expireAt;
    }

    public RedisValueType Type { get; }
    public DateTime? ExpireAt { get; }

    public sealed class String(byte[] data, DateTime? expireAt = null) : RedisValue(RedisValueType.String, expireAt)
    {
        public byte[] Data { get; } = data;
    }

    public sealed class List(List<byte[]> data) : RedisValue(RedisValueType.List, null)
    {
        public List<byte[]> Data { get; } = data;
    }
}