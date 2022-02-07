using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace s0urce.io;

internal partial class S0urceIo
{
    private static readonly Regex s_regexImage = new Regex("\"task\":333,[^,]+,\"url\":{([^}]+)}");
    private static readonly MD5 s_md5 = MD5.Create();

    private HttpClient _httpClient;
    private ClientWebSocket _webSocket;
    private ArraySegment<byte> _buffer = new byte[2048 * 4];

    private const string WORD_URL = "http://s0urce.io/client/img/word/";


    private S0urceIo(HttpClient httpClient, ClientWebSocket webSocket)
    {
        _httpClient = httpClient;
        _webSocket = webSocket;
    }


    public async Task HackPlayer(Ports port, int id = 14, CancellationToken cancellationToken = default)
    {
        await SendMessage(
            _webSocket,
            @$"42[""playerRequest"",{{""task"":100,""id"": {id},""port"":{(int)port}}}]",
            cancellationToken);

        string ret;
        string imageWord = string.Empty;
        int sendTime = int.MaxValue;

        do
        {
            ret = await ReadMessage(
                _webSocket,
                _buffer,
                cancellationToken);

            // 42["mainPackage",{ "unique":[{ "task":2002,"df":1},{ "task":333,"opt":1,"url":{ "t":"m","i":17} }]}]
            if (s_regexImage.IsMatch(ret))
            {
                var data = s_regexImage.Match(ret).Groups[1].Value
                    .Split(',')
                    .Select(x => x.Split(':')[1].Trim('"'))
                    .ToArray();

                string urlOfImage = string.Concat(WORD_URL, data[0], '/', data[1]);

                using var imageStream = await _httpClient.GetStreamAsync(
                    urlOfImage,
                    cancellationToken);

                using BinaryReader binaryReader = new BinaryReader(imageStream);
                
                try
                {
                    var imageHashBuffer = s_md5.ComputeHash(imageStream);
                    var imageKey = ConvertToHexString(imageHashBuffer);

                    imageWord = s_words[imageKey];

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(urlOfImage + " " + imageKey + " - " + imageWord);

                    sendTime = Environment.TickCount;
                    await Task.Delay(
                        500,
                        cancellationToken);

                    await SendMessage(
                        _webSocket,
                        @$"42[""playerRequest"",{{""task"":777,""word"":""{imageWord}""}}]",
                        cancellationToken);
                }
                catch (Exception ex)
                {

                    await File.WriteAllBytesAsync(
                         "error.png",
                        binaryReader.ReadBytes(1024 * 50),
                        cancellationToken);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
            else if (ret.Contains("This port has been closed"))
            {
                return;
            }

            if (Environment.TickCount - sendTime > 2000)
            {
                await SendMessage(
                    _webSocket,
                    @$"42[""playerRequest"",{{""task"":777,""word"":""{imageWord}""}}]",
                    cancellationToken);
            }

        } while (!ret.Contains("Hacking successful"));

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Finish");
        await SendMessage(
            _webSocket,
            @"42[""playerRequest"",{""task"":106,""text"":""github: Isa-0xNull""}]",
            cancellationToken);

        await SendMessage(_webSocket, "2", cancellationToken);
    }

    public static async Task<S0urceIo> LoginAsync(string username = "Isa-0xNull", CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = new HttpClient();
        string io = await GetIOToken(httpClient, cancellationToken);

        ClientWebSocket webSocket = await Login(username, io, cancellationToken);

        return new S0urceIo(httpClient, webSocket);
    }

    private static async Task<string> GetIOToken(HttpClient client, CancellationToken cancellationToken = default)
    {

        var res = await client.GetAsync("http://s0urce.io/socket.io/?EIO=3&transport=polling", cancellationToken);

        var _ = (await res.Content.ReadAsStringAsync())
                        .Split(',')[0]
                        .Split(':').Last()
                        .Trim('"');

        var io = res.Headers.GetValues("Set-Cookie").ToArray()[0]
                        .Split(';')[0]
                        .Split('=')[1];

        var __ = await client.GetAsync("http://s0urce.io/socket.io/?EIO=3&transport=polling&sid=" + io, cancellationToken);
        return io;
    }

    static async Task<ClientWebSocket> Login(string username, string io, CancellationToken cancellationToken)
    {
        ClientWebSocket webSocket = new ClientWebSocket();
        webSocket.Options.Cookies = new CookieContainer();
        webSocket.Options.Cookies.Add(
            new Uri("http://s0urce.io/"),
            new Cookie("io", io));

        await webSocket.ConnectAsync(
            new Uri("ws://s0urce.io/socket.io/?EIO=3&transport=websocket&sid=" + io),
            cancellationToken);

        await SendMessage(
            webSocket,
            "2probe",
            cancellationToken);

        while (true)
        {
            ArraySegment<byte> buffer = new byte[2048 * 4];
            string msg = await ReadMessage(webSocket, buffer, cancellationToken);
            Console.WriteLine(msg);
            if (msg.Equals("3probe", StringComparison.CurrentCulture))
            {
                break;
            }
        }

        await SendMessage(
            webSocket,
            @"5",
            cancellationToken);

        await SendMessage(
            webSocket,
            @$"42[""signIn"",{{""name"":""{username}""}}]",
            cancellationToken);

        return webSocket;
    }


    private static async Task SendMessage(ClientWebSocket ws, string message, CancellationToken cancellationToken = default)
    {
        if (ws.State == WebSocketState.Open)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();

            byte[] data = Encoding.ASCII.GetBytes(message);
            await ws.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
    }

    private static async Task<string> ReadMessage(ClientWebSocket ws, ArraySegment<byte> buffer, CancellationToken cancellation = default)
    {
        WebSocketReceiveResult response;
        StringBuilder sb = new StringBuilder();

        do
        {
            response = await ws.ReceiveAsync(buffer, cancellation);
            sb.Append(Encoding.UTF8.GetString(buffer));
        } while (!response.EndOfMessage);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(sb.ToString());
        return sb.ToString();
    }

    private static string ConvertToHexString(byte[] bytes)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);

        for (int i = 0; i < bytes.Length; i++)
        {
            result.Append(bytes[i].ToString("x2"));
        }

        return result.ToString();
    }
}