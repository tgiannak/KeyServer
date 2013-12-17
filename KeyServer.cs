using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

public class KeyServer
{
    public static void Main(string[] args)
    {
        new KeyServer().Start();
    }

    public void Start()
    {
        var page = LoadPage();
        var pwd = GetPassword();
        var port = GetPort();

        var listener = new HttpListener();
        var prefix = "http://*:"+port+"/";
        listener.Prefixes.Add(prefix);

        Console.WriteLine("Binding to prefix: " + prefix);
        Console.WriteLine("Starting...");
        listener.Start();

        var running = true;
        var t = new Thread(() => {
            while (running)
            {
                try
                {
                    var ctx = listener.GetContext();
                    new Thread(new RequestHandler(page, pwd, ctx).HandleRequest).Start();
                }
                catch (Exception ex)
                {
                    if (running)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        });
        t.Start();

        Console.WriteLine("Listening...");
        Console.WriteLine("Press <ENTER> to stop.");

        Console.ReadLine();
        running = false;
        listener.Stop();
    }

    public static string LoadPage()
    {
        var file = new FileInfo("index.html");
        using (var reader = file.OpenText())
        {
            return reader.ReadToEnd();
        }
    }

    public static string GetPassword()
    {
        Console.Write("Set password: ");
        return Console.ReadLine();
    }

    public static ushort GetPort()
    {
        ushort port;
        while (true)
        {
            Console.Write("Set port (default 8080): ");
            var portStr = Console.ReadLine();
            if (string.IsNullOrEmpty(portStr))
            {
                return 8080;
            }
            else if (ushort.TryParse(portStr, out port))
            {
                return port;
            }
            else
            {
                Console.WriteLine("Could not parse port...");
            }
        }
    }
}

public class RequestHandler
{
    private readonly HttpListenerContext ctx;
    private readonly string pwd;
    private readonly string page;

    public RequestHandler(string page, string pwd, HttpListenerContext ctx)
    {
        this.page = page;
        this.pwd = pwd;
        this.ctx = ctx;
    }

    public void HandleRequest()
    {
        try
        {
            switch (ctx.Request.Url.AbsolutePath)
            {
                case "/key":
                    HandleKey();
                    break;
                default:
                    WriteResponse(page);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            ctx.Response.OutputStream.Close();
        }
    }

    public void WriteResponse(string responseContent)
    {
        var response = ctx.Response;
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseContent);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer,0,buffer.Length);
    }

    public void HandleKey()
    {
        if (ctx.Request.QueryString["pwd"] == pwd)
        {
            SendKeys.SendWait(ctx.Request.QueryString["str"]);
        }
    }
}
