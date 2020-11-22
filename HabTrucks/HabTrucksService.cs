using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO.Ports;
using SuperWebSocket;

namespace HabTrucks
{
    public partial class HabTrucksService : ServiceBase
    {
        SerialPort _serialPort;
        WebSocketServer wsServer;
        List<WebSocketSession> connections = new List<WebSocketSession>();
        System.Timers.Timer timer = new System.Timers.Timer(); // name space(using System.Timers;)  
        public HabTrucksService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds  
            timer.Enabled = true;

            wsServer = new WebSocketServer();
            wsServer.Setup(3000);
            wsServer.NewSessionConnected += WsServer_NewSessionConnected;
            wsServer.NewMessageReceived += WsServer_NewMessageReceived;
            wsServer.NewDataReceived += WsServer_NewDataReceived;
            wsServer.SessionClosed += WsServer_SessionClosed;

            wsServer.Start();

            void ReadSerialPort()
            {
                void Send(String value)
                {
                    List<WebSocketSession> _connections;
                    _connections = new List<WebSocketSession>(connections);
                    foreach (WebSocketSession connection in _connections)
                        connection.Send(value);
                }

                while (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _serialPort.ReadTimeout = 5000;
                    Send("{ \"type\": false, \"message\": \"Connecting\"}");

                    while (_serialPort.IsOpen)
                    {
                        try
                        {
                            Send("{ \"type\": false, \"message\": \"" + _serialPort.ReadLine() + "\"}");
                            Thread.Sleep(500);
                        }
                        catch (System.TimeoutException)
                        {
                            Send("{ \"type\": false, \"message\": \"Time Out\"}");
                            Thread.Sleep(2000);
                            break;
                        }
                    }

                    _serialPort.Close();
                }
            }

            _serialPort = new SerialPort("COM3");
            Thread sPort = new Thread(ReadSerialPort);
            sPort.Start();

            void WsServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
            {
                connections.Remove(session);
            }
            void WsServer_NewDataReceived(WebSocketSession session, byte[] value)
            {
                Console.WriteLine("NewDataReceived");
            }
            void WsServer_NewMessageReceived(WebSocketSession session, string value)
            {
                foreach (WebSocketSession connection in connections)
                    connection.Send("Hola -> " + value);
            }
            void WsServer_NewSessionConnected(WebSocketSession session)
            {
                connections.Add(session);
                Console.WriteLine("NewSessionConnected");
            }
        }
        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
        }
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
