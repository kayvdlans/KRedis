using System.Buffers;
using System.Net;
using System.Net.Sockets;
using KRedis.Commands;
using KRedis.Resp;

TcpListener server = new(IPAddress.Any, 6379);
server.Start();

try
{
    while (true)
    {
        Socket client = server.AcceptSocket();
        _ = Task.Run(() => HandleClient(client));
    }
}
catch (Exception e)
{
    Console.WriteLine($"[ERROR] {e.Message}");
}
finally
{
    server.Stop();
}

return;

static async Task HandleClient(Socket client)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
    var bufferSize = 0;

    try
    {
        while (true)
        {
            if (bufferSize == buffer.Length)
                buffer = GrowBuffer(buffer, bufferSize);

            Memory<byte> free = buffer.AsMemory(bufferSize);
            int bytesReceived = await client.ReceiveAsync(free);

            if (bytesReceived == 0)
                break;

            bufferSize += bytesReceived;

            var totalConsumed = 0;

            while (RespParser.TryParseAny(buffer.AsSpan(0, bufferSize)[totalConsumed..], out RespValue value,
                       out int consumed))
            {
                totalConsumed += consumed;

                RespValue resp = value switch
                {
                    RespValue.Array(var items, false) => HandleCommand(items),
                    _ => new RespValue.SimpleError("ERR unexpected request")
                };

                await client.SendAsync(resp.AsResponse());
            }

            if (totalConsumed <= 0)
                continue;

            int remaining = bufferSize - totalConsumed;
            if (remaining > 0)
                buffer.AsSpan(totalConsumed, remaining).CopyTo(buffer.AsSpan(0, remaining));

            bufferSize = remaining;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"[ERROR] {e.Message}");
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
        client.Close();
    }
}

static RespValue HandleCommand(IReadOnlyList<RespValue> items)
{
    ICommand? command = CommandFactory.TryCreateCommand(items, out RespValue error);
    return command is not null ? command.Execute() : error;
}

static byte[] GrowBuffer(byte[] buffer, int size)
{
    int newSize = buffer.Length * 2;
    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
    Buffer.BlockCopy(buffer, 0, newBuffer, 0, size);
    ArrayPool<byte>.Shared.Return(buffer);
    return newBuffer;
}