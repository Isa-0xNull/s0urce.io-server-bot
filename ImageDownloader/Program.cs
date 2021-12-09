

using System.Net;
using System.Security.Cryptography;
using System.Text;


ConvertAllImages();

static void ConvertAllImages()
{
    using MD5 md5 = MD5.Create();
    var dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

    foreach (var img in dirInfo.GetFiles("*.png"))
    {
        byte[] buffer = File.ReadAllBytes(img.FullName);
        byte[] hashBuffer = md5.ComputeHash(buffer);

        string hash = ConvertToHexString(hashBuffer);
        Console.WriteLine($"{hash}, {Path.GetFileNameWithoutExtension(img.Name)}");
    }

    
}

static string ConvertToHexString(byte[] bytes)
{
    StringBuilder result = new StringBuilder(bytes.Length * 2);

    for (int i = 0; i < bytes.Length; i++)
    {
        result.Append(bytes[i].ToString("x2"));
    }

    return result.ToString();
}

static void downloadImages()
{
    using WebClient wc = new WebClient();


    for (char start = 'a'; start < 'z'; start++)
    {
        for (int i = 0; i < 1000; i++)
        {
            try
            {
                string url = "http://s0urce.io/client/img/word/" + start + "/" + i;
                Console.WriteLine(url);
                wc.DownloadFile(url, $"{start}{i}.png");
            }
            catch
            {
                break;
            }
        }
    }

}