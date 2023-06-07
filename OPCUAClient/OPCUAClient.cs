using System;
using System.Collections.Generic;
using Opc.Ua.Client;
using Opc.Ua;
using System.Reflection;
using Opc.Ua.Configuration;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace OPCUAClient
{
    public class OPCUAClient
    {
        protected ApplicationInstance application;
        protected ApplicationConfiguration configuration;
        protected Session session;
        protected Subscription subscription;
        protected MonitoredItem monitoredItem;
        public bool IsConnected { get; set; }
        public string TagRequest { get; set; }
        public string TagResponse { get; set; }

        public delegate void ValuesChanged(string TagName, object TagValue);
        public event ValuesChanged Changed;
        protected string uaConfigFileName;
        protected CancellationToken stoppingToken;


        public OPCUAClient(string uaConfigFileName, CancellationToken stoppingToken, string tagRequest, string tagResponse)
        {
            this.uaConfigFileName = uaConfigFileName;
            this.stoppingToken = stoppingToken;

            TagRequest = tagRequest;
            TagResponse = tagResponse;

        }

        public void Start()
        {
            Task task = Task.Run(ControlConnection, stoppingToken);
        }

        public void Connect()
        {
            Disconnect();

            application = new ApplicationInstance();
            application.ApplicationName = uaConfigFileName;
            application.ApplicationType = ApplicationType.Client;

            application.LoadApplicationConfiguration(uaConfigFileName, silent: false).Wait();
            application.CheckApplicationInstanceCertificate(silent: false, minimumKeySize: 0).Wait();


            configuration = application.ApplicationConfiguration;

            configuration.CertificateValidator.CertificateValidation += delegate (CertificateValidator sender, CertificateValidationEventArgs e)
            {
                e.AcceptAll = true;
            };

            CreateSession();
            AddSubscription();

            
        }

        public void Disconnect()
        {
            if (session != null)
            {
                session.KeepAlive -= Session_KeepAlive;
                session.Close();
                session.Dispose();
                session = null;
            }
        }

        protected async void ControlConnection()
        {
            //log.LogInformation("ControlConnection");
            
            while (stoppingToken.IsCancellationRequested == false)
            {
                if (!IsConnected)
                {
                    try
                    {
                        Connect();
                        //log.LogInformation("Подключение к OPCUA успешно");

                    }
                    catch (Exception ex)
                    {
                        //#if DEBUG
                        //                        Console.WriteLine("Reconnect...");
                        //#endif

                        await Task.Delay(10 * 1000, stoppingToken);
                    }
                }

                await Task.Delay(5000, stoppingToken);
            }
        }


        protected void CreateSession()
        {
            EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(configuration.ClientConfiguration.WellKnownDiscoveryUrls[0], false);
            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(configuration);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            Task<Session> sessionTask = Session.Create(
                configuration,
                endpoint,
                false,
                false,
                configuration.ApplicationName,
                30 * 60,
                new UserIdentity(),
                null
            );

            session = sessionTask.Result;
            session.KeepAlive += Session_KeepAlive;

            IsConnected = true;
        }



        private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                IsConnected = false;
            }
            else
            {
                IsConnected = true;
            }
        }

        public object ReadValue()
        {
            Browser browser = new Browser(session);
            browser.BrowseDirection = BrowseDirection.Both;
            browser.ContinueUntilDone = true;
            browser.ReferenceTypeId = ReferenceTypeIds.References;

            DataValue value = session.ReadValue(new NodeId(TagResponse, 1));
            //NodeId currentPhase = value.GetValue<NodeId>(null);
            return value.Value;
        }

        public void WriteValue(string TagName, object TagValue)
        {
            WriteValue valueToWrite = new WriteValue()
            {
                NodeId = new NodeId(TagName),
                AttributeId = Attributes.Value
            };
            valueToWrite.Value.Value = TagValue;
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

            WriteValueCollection valuesToWrite = new WriteValueCollection
            {
                valueToWrite
            };

            session.Write(
                null,
                valuesToWrite,
                out StatusCodeCollection results,
                out DiagnosticInfoCollection diagnosticInfos);
        }

        public void AddSubscription()
        {
            //log.LogInformation(TagRequest);

            //Console.WriteLine("Step 4 - Create a subscription. Set a faster publishing interval if you wish.");
            var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000 };

            //Console.WriteLine("Step 5 - Add a list of items you wish to monitor to the subscription.");
            var list = new List<MonitoredItem>
            {
                new MonitoredItem(subscription.DefaultItem) { DisplayName = "ServerStatusCurrentTime", StartNodeId = new NodeId(TagRequest) },
                //new MonitoredItem(subscription.DefaultItem) { DisplayName = "ServerStatusCurrentTime", StartNodeId = new NodeId(TagResponse, 1) }
            };


            list.ForEach(i => i.Notification += monitoredItem_Notification);
            subscription.AddItems(list);

            //Console.WriteLine("Step 6 - Add the subscription to the session.");
            session.AddSubscription(subscription);
            subscription.Create();

            //log.LogInformation(session.ReadValue(new NodeId(TagRequest)).Value.ToString());
        }

        private void monitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {

            //#if DEBUG
            //            Console.WriteLine($"monitoredItem_Notification. StatusCode: {((MonitoredItemNotification)e.NotificationValue).Value.StatusCode}");
            //#endif
            //log.LogInformation((((MonitoredItemNotification)e.NotificationValue).Value.StatusCode).ToString());
            if (StatusCode.IsGood(((MonitoredItemNotification)e.NotificationValue).Value.StatusCode))
            {
                Changed?.Invoke(monitoredItem.ResolvedNodeId.Identifier.ToString(), ((MonitoredItemNotification)e.NotificationValue).Value.Value);
            }
        }
    }
}
