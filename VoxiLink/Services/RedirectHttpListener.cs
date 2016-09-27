using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace VoxiLink.Services
{
    class RedirectHttpListener
    {
        public async void Listen(string uri)
        {
            string code = null;

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                throw new HttpListenerException();
            }

            HttpListener listener = new HttpListener();

            // Build the redirect Uri to listen for the code
            listener.Prefixes.Add(uri +"/");

            listener.Start();

            while (code == null)
            {
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
            
                if (request.HasEntityBody)
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        System.Diagnostics.Debug.WriteLine(sr.ReadToEnd());
                    }
                }
                    
                // Obtain a response object.
                using (HttpListenerResponse response = context.Response)
                {
                    string responseString = "<HTML><BODY>Authentification on the Vox-Api-Winapp.<br/>Error : <b>" + context.Request.Url + "</b>.<br/>Try to connect later.</BODY></HTML>";
                    if (context.Request.QueryString["code"] != null)
                    {
                        using (var streamReader = new StreamReader("Resources/valid.html", Encoding.UTF8))
                        {
                            responseString = streamReader.ReadToEnd();
                        }
                        code = context.Request.QueryString["code"];
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                }
            }

            listener.Stop();


            await Api.Session.Connect(code);
        }
    }
}