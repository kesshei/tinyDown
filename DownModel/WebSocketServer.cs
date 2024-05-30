using System.Buffers;
using System.Linq;
using System.Net.WebSockets;
using System.Text;

namespace DownModel
{
    public static class WebSocketServer
    {
        public static int bufferLength = 1 * 1024 * 1024;
        public static async Task<bool> UpdateFileAsync(WebSocket webSocket, string upfile, string savefile)
        {
            if (File.Exists(upfile))
            {
                using var file = File.OpenRead(upfile);
                var hash256 = file.Hash256();

                RequestInfo requestInfo = new RequestInfo() { FileLength = file.Length, ServerPath = upfile, LoalPath = savefile, Hash256 = hash256 };
                var bytes = Encoding.UTF8.GetBytes(requestInfo.ToJson());
                var HeadBytes = BitConverter.GetBytes(bytes.Length);
                await webSocket.SendAsync(HeadBytes.Concat(bytes).ToArray(), WebSocketMessageType.Binary, false, CancellationToken.None);
                var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                try
                {
                    file.Position = 0;
                    var endOfMessage = false;
                    while (!endOfMessage)
                    {
                        var count = file.Read(buffer);
                        if (count < buffer.Length)
                        {
                            endOfMessage = true;
                        }
                        await webSocket.SendAsync(buffer.Take(count).ToArray(), WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
                        Console.WriteLine($"已发送:{((double)file.Position / (double)file.Length).ToString("F2")}%");
                    }
                    Console.WriteLine("发送完毕!");
                    return true;
                }
                catch (Exception ex )
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            return false;
        }
        public static async Task<bool> DownFileAsync(WebSocket webSocket)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
            var listBytes = new List<byte>();
            var length = -1;
            var success = false;
            RequestInfo requestInfo = null;
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        if (length == -1)
                        {
                            length = BitConverter.ToInt32(buffer, 0);
                        }
                        if (requestInfo == null)
                        {
                            listBytes.AddRange(buffer.Take(result.Count));
                            if (listBytes.Count >= length + 4)
                            {
                                requestInfo = Encoding.UTF8.GetString(listBytes.Skip(4).Take(length).ToArray()).ToObj<RequestInfo>();
                                if (File.Exists(requestInfo.LoalPath))
                                {
                                    File.Delete(requestInfo.LoalPath);
                                }
                            }
                        }
                        else
                        {
                            using var write = File.OpenWrite(requestInfo.LoalPath);
                            write.Seek(0, SeekOrigin.End);
                            write.Write(buffer.Take(result.Count).ToArray());
                        }
                    }
                    else
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bad type", CancellationToken.None);
                        throw new Exception("接收文件的类型不正确!");
                    }
                    if (result.EndOfMessage)
                    {
                        using var read = File.OpenRead(requestInfo.LoalPath);
                        read.Position = 0;
                        var hash256 = read.Hash256();
                        if (hash256 == requestInfo.Hash256)
                        {
                            Console.WriteLine($"上传并验证成功:{requestInfo.LoalPath}");
                            success = true;
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "get ok", CancellationToken.None);
                        }
                        else
                        {
                            Console.WriteLine($"上传并验证失败:{requestInfo.LoalPath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return success;
        }
    }
}
