using System.Net;
using System.Threading;
using UnityEngine;

namespace AssetLayer.Unity { 

    public delegate void RequestHandler(HttpListenerContext context);


    public class HttpServer
    {
        public RequestHandler onRequestReceived;
        private HttpListener listener;
        private Thread listenerThread;

        public HttpServer()
        {
            listener = new HttpListener();
            // Listen on localhost, port 8080, and all URIs starting with "test"
            listener.Prefixes.Add("http://localhost:8080/loginUnity/");
        }



        public void Start()
        {
            Debug.Log("https server starting");
            listener.Start();
            listenerThread = new Thread(HandleRequests);
            listenerThread.Start();
            Debug.Log("https server started");
        }

        private void HandleRequests()
        {
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(o => ProcessRequest(context));
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            Debug.Log("Request received ");
            onRequestReceived?.Invoke(context);
        }

        public void Stop()
        {
            listener.Stop();
            listenerThread.Abort();
        }
    }
}

