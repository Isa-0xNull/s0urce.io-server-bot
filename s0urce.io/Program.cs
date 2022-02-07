using static s0urce.io.S0urceIo;

namespace s0urce.io;

class Program
{

    public static async Task Main()
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        S0urceIo player0 = await LoginAsync(cancellationToken: cts.Token);

        Task.WaitAll(
            new Task[]
            {
                RunAsyc(player0, cts.Token),
            },cts.Token);

    }

    private static async Task RunAsyc(S0urceIo player, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            for (int i = 1; i <= 16; i++)
            {
                try
                {
                    await player.HackPlayer(Ports.Port1, i, cancellationToken);
                    await player.HackPlayer(Ports.Port2, i, cancellationToken);
                    await player.HackPlayer(Ports.Port3, i, cancellationToken);

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}