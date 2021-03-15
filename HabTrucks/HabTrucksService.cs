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
using Newtonsoft.Json;

namespace HabTrucks
{
    public partial class HabTrucksService : ServiceBase
    {
        WebSocketServer wsServer;
        List<WebSocketSession> connections = new List<WebSocketSession>();
        System.Timers.Timer timer = new System.Timers.Timer(); // name space(using System.Timers;)  
        public HabTrucksService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds  
            timer.Enabled = true;

            wsServer = new WebSocketServer();
            wsServer.Setup(1337);
            wsServer.NewSessionConnected += WsServer_NewSessionConnected;
            wsServer.NewMessageReceived += WsServer_NewMessageReceived;
            wsServer.NewDataReceived += WsServer_NewDataReceived;
            wsServer.SessionClosed += WsServer_SessionClosed;

            if (!File.Exists("config.json"))
            {
                StreamWriter fs = File.CreateText("config.json");
                fs.WriteLine("{");
                fs.WriteLine("\t\"center_code_sap\": \"1709\",");
                fs.WriteLine("\t\"websocket\": \"http://portal.daabon.com.co:1337\",");
                fs.WriteLine("\t\"ports\": [");
                fs.WriteLine("\t\t{");
                fs.WriteLine("\t\t\t\"PortName\": \"COM3\",");
                fs.WriteLine("\t\t\t\"BaudRate\": 9600,");
                fs.WriteLine("\t\t\t\"DataBits\": 8,");
                fs.WriteLine("\t\t\t\"ReadTimeout\": 10000,");
                fs.WriteLine("\t\t\t\"NewLine\": \"\\r\",");
                fs.WriteLine("\t\t\t\"Broadcast\": \"weight\"");
                fs.WriteLine("\t\t}");
                fs.WriteLine("\t]");
                fs.WriteLine("}");
                fs.Close();
            }

            wsServer.Start();

            void ReadSerialPort(dynamic config_port)
            {
                void Send(bool type, String message, string value = "")
                {
                    List<WebSocketSession> _connections;
                    _connections = new List<WebSocketSession>(connections);
                    foreach (WebSocketSession connection in _connections)
                        connection.Send("{ \"broadcast\": " + config_port.Broadcast + ", \"type\": " + type + ", \"message\": \"" + message + "\", \"value\": \"" + value + "\"}");
                }

                SerialPort _serialPort = new SerialPort("COM3");
                _serialPort.ReadTimeout = 1000;

                while (true)
                {
                    try
                    {
                        if (!_serialPort.IsOpen)
                        {
                            _serialPort.Open();
                            Send(false, "Connecting port " + config_port.PortName);
                        }
                    }
                    catch
                    {
                        Send(false, "Port " + config_port.PortName + " not open");
                        Thread.Sleep(1000);
                    }

                    while (_serialPort.IsOpen)
                    {
                        try
                        {
                            String _read = _serialPort.ReadLine();
                            String value = String.Join("", _read.ToCharArray().Where(Char.IsDigit));

                            Send(true, _read, value);
                        }
                        catch (System.TimeoutException)
                        {
                            Send(false, "Time Out " + config_port.PortName);
                            break;
                        }
                    }
                }
            }

            StreamReader r = new StreamReader("config.json");
            string json = r.ReadToEnd();
            r.Close();
            dynamic config = JsonConvert.DeserializeObject(json);
            foreach (var port in config.ports)
            {
                Thread sPort = new Thread(() => ReadSerialPort(port));
                sPort.Start();
            }

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
            }
            void WsServer_NewSessionConnected(WebSocketSession session)
            {
                connections.Add(session);
                Console.WriteLine("NewSessionConnected");
            }
        }
        protected override void OnStop()
        {
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
        }
    }
}
