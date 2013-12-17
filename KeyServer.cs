using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public class KeyServer
{
    public static void Main(string[] args)
    {
        new KeyServer().Start();
    }

    /// <summary>
    /// Starts the server.
    /// </summary>
    public void Start()
    {
        var pages = LoadPages();
        var pwd = GetPassword();
        var port = GetPort();

        var listener = new HttpListener();
        var prefix = "http://*:"+port+"/";
        listener.Prefixes.Add(prefix);

        Console.WriteLine("Binding to prefix: " + prefix);
        Console.WriteLine("Starting...");
        listener.Start();

        var running = true;
        // runs the request handler dispatch thread
        var t = new Thread(() => {
            while (running)
            {
                try
                {
                    var ctx = listener.GetContext();
                    var handler = new RequestHandler(pages, pwd, ctx);
                    // start a thread to handle the request and move on
                    new Thread(handler.HandleRequest).Start();
                }
                catch (Exception ex)
                {
                    // if we are done running, no need to worry about error messages
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

    /// <summary>
    /// Loads all of the pages in the "ui" directory into memory, keyed by the extensionless file name.
    /// </summary>
    private static IDictionary<string, string> LoadPages()
    {
        var folder = new DirectoryInfo("ui");
        return folder.GetFiles()
                     .Where(f => f.Extension.ToLower() == ".html")
                     .ToDictionary(GetFileKey, GetFileContents);
    }

    private static string GetFileKey(FileInfo file)
    {
        return Path.GetFileNameWithoutExtension(file.Name);
    }

    /// <summary>
    /// Reads a whole file into memory as a string.
    /// </summary>
    private static string GetFileContents(FileInfo file)
    {
        using (var reader = file.OpenText())
        {
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// Asks the user for the password to use.
    /// </summary>
    private static string GetPassword()
    {
        Console.Write("Set password: ");
        return Console.ReadLine();
    }

    /// <summary>
    /// Asks the user for the port to use.
    /// </summary>
    private static ushort GetPort()
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


/// <summary>
/// Handles a request based on the HttpListenerContext.
/// </summary>
public class RequestHandler
{
    private readonly HttpListenerContext ctx;
    private readonly string pwd;
    private readonly IDictionary<string, string> pages;

    public RequestHandler(IDictionary<string, string> pages, string pwd, HttpListenerContext ctx)
    {
        this.pages = pages;
        this.pwd = pwd;
        this.ctx = ctx;
    }

    public void AttemptHandleRequest()
    {
        switch (ctx.Request.Url.AbsolutePath)
        {
            case "/key":
                HandleKey();
                break;
            default:
                HandlePage(ctx.Request.Url.AbsolutePath);
                break;
        }
    }

    public void HandleRequest()
    {
        try
        {
            AttemptHandleRequest();
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

    /// <summary>
    /// Lists the available pages.
    /// </summary>
    private string AvailablePages()
    {
        var sb = new StringBuilder(@"<html><head>
                <meta name=""viewport"" content=""width=device-width"">
                </head><body style=""font-size:36pt""><ul>");
        foreach (var page in pages)
        {
            sb.AppendFormat(@"<li><a href=""{0}"">{0}</a></li>", page.Key);
        }
        sb.Append(@"</ul></body></html>");
        return sb.ToString();
    }

    private void HandlePage(string url)
    {
        WriteResponse(RenderPage(url));
    }

    private string RenderPage(string url)
    {
        var pageName = url.TrimStart('/');
        string page;
        if (!pages.TryGetValue(pageName, out page))
        {
            return AvailablePages();
        }
        return page;
    }

    /// <summary>
    /// Writes the string to the response stream and sets the Content-Length
    /// header appropriately.
    /// </summary>
    private void WriteResponse(string responseContent)
    {
        var response = ctx.Response;
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseContent);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer,0,buffer.Length);
    }

    /// <summary>
    /// Sends the value for str in the query string as keyboard presses, as
    /// long as the password is correct.  If the password is incorrect, does
    /// nothing.
    /// </summary>
    private void HandleKey()
    {
        if (ctx.Request.QueryString["pwd"] == pwd)
        {
            SendKeys.SendWait(ctx.Request.QueryString["str"]);
        }
    }
}
