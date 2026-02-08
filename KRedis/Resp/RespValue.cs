using System.Text;

namespace KRedis.Resp;

public abstract record RespValue
{
    private RespValue()
    {
    }

    public abstract byte[] AsResponse();

    public sealed record BulkString(byte[] Data, bool IsNull = false) : RespValue
    {
        public string Value => Encoding.ASCII.GetString(Data);

        public override byte[] AsResponse()
        {
            if (IsNull)
                return "$-1\r\n"u8.ToArray();

            byte[] header = Encoding.ASCII.GetBytes($"${Data.Length}\r\n");
            var response = new byte[header.Length + Data.Length + 2];

            Buffer.BlockCopy(header, 0, response, 0, header.Length);
            Buffer.BlockCopy(Data, 0, response, header.Length, Data.Length);

            response[^2] = (byte)'\r';
            response[^1] = (byte)'\n';

            return response;
        }
    }

    public sealed record SimpleString(string Value) : RespValue
    {
        public override byte[] AsResponse()
        {
            return Encoding.ASCII.GetBytes($"+{Value}\r\n");
        }
    }

    public sealed record SimpleError(string Message) : RespValue
    {
        public override byte[] AsResponse()
        {
            return Encoding.ASCII.GetBytes($"-{Message}\r\n");
        }
    }

    public sealed record Integer(long Value) : RespValue
    {
        public override byte[] AsResponse()
        {
            return Encoding.ASCII.GetBytes($":{Value}\r\n");
        }
    }

    public sealed record Array(IReadOnlyList<RespValue> Items, bool IsNull = false) : RespValue
    {
        public override byte[] AsResponse()
        {
            if (IsNull)
                return "*-1\r\n"u8.ToArray();

            var resp = new StringBuilder($"*{Items.Count}\r\n");
            foreach (RespValue item in Items)
            {
                resp.Append(item.AsResponse());
            }

            return Encoding.ASCII.GetBytes(resp.ToString());
        }
    }
}