
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using RabbitMQ.Client;
using OPC_UA.Client;
using OPC_UA.Client.Common;
using Serilog;
using System.Text;
using System;

namespace OPCUA.TestClient
{

    internal class Program
    {
        private static IUaClient _uaClient;
        private static IConfigurationRoot _config;

        private static IConnection _rabbitMqConnection;
        private static IModel _emailChannel;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting OPCUA Test Client!");

           // TaskScheduler.UnobservedTaskException += GlobalExceptionHandler.TaskScheduler_UnobservedTaskException;
            // AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler.CurrentDomain_UnhandledException;

            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

            _config = config.Build();
            Log.Logger = new LoggerConfiguration()
             //.WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
             //.WriteTo.(path: "./logs/TestUaClient.log", rollingInterval: RollingInterval.Day)
             .MinimumLevel.Debug()
             .Enrich.FromLogContext()
             .CreateLogger();


            if (ConnectUaClient())
            {
                StartTagMonitoring();
            }
            else
            {
                Log.Warning("Faild to connect UaClient");
            }

            Console.WriteLine(Environment.NewLine + "Press anny key to close the application", ConsoleColor.Green);
            Console.ReadLine();
            Console.WriteLine("Closing application");
        }


        private static bool ConnectUaClient()
        {
            try
            {
                var url = _config.GetValue("URL", "");
                Log.Information("Connecting OPCUA Server: " + url);
                _uaClient = new UaClient(new Uri(url));
                _uaClient.Connect();
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error connecting UaClient");
                return false;
            }
        }


        private static async Task StartTagMonitoring()
        {
            try
            {
                var connectionString = _config.GetValue("ConnectionString", "");
                var connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(connectionString)
                };
                // connectionFactory.AutomaticRecoveryEnabled = true;
                //connectionFactory.DispatchConsumersAsync = false;
                _rabbitMqConnection = connectionFactory.CreateConnection();
                _emailChannel = _rabbitMqConnection.CreateModel();

                var wTag = _config.GetValue("WatchDogTag", "");
                if (wTag != "")
                {
                    Log.Information($"Start watchdog on tag: {wTag}");
                    _uaClient.Monitor<int>(wTag, (readEvent, cancelationToken) =>
                    {
                        WatchdogTogle(readEvent);
                    }, 1000);
                }

                var intRead = _config.GetValue("Int16ReadTag", "");
                var intWrite = _config.GetValue("Int16WriteTag", "");
                if (intRead != "" && intWrite != "")
                {
                    Log.Information($"Start monitoring on tag: {intWrite}");
                    _uaClient.Monitor<short>(intWrite, (readEvent, cancelationToken) =>
                    {
                        OnIntChange(readEvent);
                    }, 1000);
                }

                //var strRead = _config.GetValue("StringReadTag", "");
                //var strWrite = _config.GetValue("StringWriteTag", "");
                //if (strRead != "")
                //{
                //    Log.Information($"Start monitoring on tag: {strRead}");
                //    _uaClient.Monitor<string>(strRead, (readEvent, cancelationToken) =>
                //    {
                //        OnStrChange(readEvent);
                //    }, 1000);
                //}

            }
            catch (Exception e)
            {
                Log.Error(e, "Error connecting tags");
            }


        }

        private static async void OnStrChange(ReadEvent<string> readEvent)
        {
            Log.Information($"ReadString tag changed to: {readEvent.Value}");
            Log.Information($"Set WriteString tag : {readEvent.Value} - Ok");
                //await _uaClient.WriteAsync<string>(_config.GetValue("StringWriteTag", ""), readEvent.Value + " - Ok");

        }

        private static async void OnIntChange(ReadEvent<short> readEvent)
        {
            //Log.Information($"ReadInt16 tag changed to: {readEvent.Value}");
            Console.WriteLine($"ReadInt16 tag changed to: {readEvent.Value}");

            //Log.Information($"Set WriteInt16 tag : {readEvent.Value + 10}");
            Console.WriteLine($"Set WriteInt16 tag : {readEvent.Value + 10}");
            //await _uaClient.WriteAsync<short>(_config.GetValue("Int16WriteTag", ""), (short)(readEvent.Value + 10));
        }


        private static async void WatchdogTogle(ReadEvent<int> readEvent)
        {
            Log.Information($"Watchdog tag changed to: {readEvent.Value}");
            
                Log.Information($"Set Watchdog tag : true (1)");
                Console.WriteLine(readEvent.Value.ToString());
            //await _uaClient.WriteAsync<int>(_config.GetValue("WatchDogTag", ""), readEvent.Value);

            var userPaymentsBody = Encoding.UTF8.GetBytes(readEvent.Value.ToString());
            if (readEvent.Value % 2 == 0)
            {
                _emailChannel.BasicPublish(exchange: "topic", routingKey: "test.sweden.payments", null,
                    userPaymentsBody);
            }
            else
            {
                _emailChannel.BasicPublish(exchange: "topic", routingKey: "user.payments", null,
                    userPaymentsBody);
            }



        }
    }
}
