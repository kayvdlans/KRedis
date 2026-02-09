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

    public sealed record Array(RespValue[] Items, bool IsNull = false) : RespValue
    {
        public override byte[] AsResponse()
        {
            if (IsNull)
                return "*-1\r\n"u8.ToArray();

            byte[] header = Encoding.ASCII.GetBytes($"*{Items.Length}\r\n");

            int totalLength = header.Length;
            var parts = new byte[Items.Length][];
            for (var i = 0; i < Items.Length; i++)
            {
                parts[i] = Items[i].AsResponse();
                totalLength += parts[i].Length;
            }

            var response = new byte[totalLength];
            Buffer.BlockCopy(header, 0, response, 0, header.Length);

            int offset = header.Length;
            foreach (byte[] bytes in parts)
            {
                Buffer.BlockCopy(bytes, 0, response, offset, bytes.Length);
                offset += bytes.Length;
            }

            return response;
        }
    }
}
