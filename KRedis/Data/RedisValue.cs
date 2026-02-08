namespace KRedis.Data;

public sealed class RedisValue(RedisValueType type, byte[] data, DateTime? expireAt = null)
{
    public RedisValueType Type { get; } = type;
    public DateTime? ExpireAt { get; } = expireAt;
    public byte[] Data { get; } = data;
}