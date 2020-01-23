

namespace sendMessage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class Program
    {
        const string ServiceBusConnectionString1 = "Endpoint=sb://licenseplatepublisher.servicebus.windows.net/;SharedAccessKeyName=ConsumeReads;SharedAccessKey=VNcJZVQAVMazTAfrssP6Irzlg/pKwbwfnOqMXqROtCQ=";
        const string TopicName1 = "licenseplateread";
        const string SubscriptionName1 = "85SyE8eUN5kFdBaE";
        static ISubscriptionClient subscriptionClient1;


        const string ServiceBusConnectionString2 = "Endpoint=sb://licenseplatepublisher.servicebus.windows.net/;SharedAccessKeyName=listeneronly;SharedAccessKey=w+ifeMSBq1AQkedLCpMa8ut5c6bJzJxqHuX9Jx2XGOk=";
        static string TopicName2 = "wantedplatelistupdate";
        static string SubscriptionName2 = "JmEKrHDG3S3a8n38";
        static ISubscriptionClient subscriptionClient2;
        static string answer;



        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {

            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            subscriptionClient1 = new SubscriptionClient(ServiceBusConnectionString1, TopicName1, SubscriptionName1);

            answer ="[ 003VLH ,  005AFA ,  006ZCC ,  024PFV ,  025WFD ,  026VDX ,  027SSD ,  027WMW ,  029AFH ,  033ASK ,  034XMW ,  041PSJ ,  047PGM ,  048MQJ746 ,  056TKC ,  060BJQ ,  065AJP ,  065TZN ,  069VLM ,  072WGT ,  075VJN ,  088ASJ ,  092PYB ,  099PGW ,  103EEN ,  106WER ,  106XBJ ,  137XDD ,  145LRR ,  150HZN ,  152WXQ ,  158HMM ,  160VGM ,  170YYT ,  172ZAZ ,  186HMJ ,  186ZMX ,  199SVC305 ,  204PYE ,  204TVD ,  209RHW ,  214SSS ,  217TAP ,  217VMT ,  218ZLZ ,  218ZVN97 ,  220WNZ ,  221HWM ,  221VKW322 ,  241SXX ,  241ZCK ,  244WFB ,  245HWN ,  246ADW ,  250TMW ,  251HLL ,  251KLK ,  251WLL ,  252YRN ,  253ZTD ,  259XYS ,  260DJT ,  262LTC ,  264PFV ,  271VMT ,  276XYK ,  294GSE ,  294TVT48 ,  321WFD ,  327WST ,  330XNV ,  333LBJ ,  344XLY ,  347WRK ,  347WVL ,  356WNW ,  359XZN ,  380RHW ,  391XKV ,  401YWS ,  402RQF ,  440JDR ,  466SYK ,  480CVR ,  488MTK ,  490RVQ ,  498YRH ,  515QYZ ,  529YHG ,  557TRJ ,  660XPS ,  698VLK ,  699BFQ ,  744YHK ,  850WPS ,  Q33AS ] ";

            Console.WriteLine(answer);
            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            // Register subscription message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await subscriptionClient1.CloseAsync();


        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that processes messages.
            subscriptionClient1.RegisterMessageHandler(ProcessMessagesAsync1, messageHandlerOptions);



        }

        static async Task ProcessMessagesAsync1(Message message, CancellationToken token)
        {
            subscriptionClient2 = new SubscriptionClient(ServiceBusConnectionString2, TopicName2, SubscriptionName2);

            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };


            subscriptionClient2.RegisterMessageHandler(ProcessMessagesAsync2, messageHandlerOptions);
            await subscriptionClient2.CloseAsync();



            // Process the message.
            // Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");
            string temp = Encoding.UTF8.GetString(message.Body);



            await PostRequest(temp);

            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
            await subscriptionClient1.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }


        static async Task ProcessMessagesAsync2(Message message, CancellationToken token)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            if (Encoding.UTF8.GetString(message.Body) != "")
            {
                answer = await GetRequest();
                Console.WriteLine(answer);
                await subscriptionClient2.CompleteAsync(message.SystemProperties.LockToken);
                return;
            }
            return;



            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }





        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        static async Task PostRequest(string message)
        {

            var byteArray = new UTF8Encoding().GetBytes("equipe03:f8xSZLJMVTyM3NGk");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


            message = message.Replace("\\", "");


            int virgule = 0;
            int precvirgule = 0;
            int counter2 = 1;
            string v1 = "";
            string v2 = "";
            string v3 = "";
            string v4 = "";
            string v5 = "";

            for (int x = 0; x < message.Length; x++)
            {
                if (message[x] == ',')
                {
                    if (counter2 == 1)
                    {
                        v1 = message.Substring(33, (virgule - 1 - 33));
                        precvirgule = virgule;
                    }
                    if (counter2 == 2)
                    {
                        v2 = message.Substring((precvirgule + 22), (virgule - 1 - (precvirgule + 22)));
                        precvirgule = virgule;
                    }
                    if (counter2 == 3)
                    {
                        v3 = message.Substring((precvirgule + 17), (virgule - (precvirgule + 17)));
                        precvirgule = virgule;
                    }
                    if (counter2 == 4)
                    {
                        v4 = message.Substring((precvirgule + 18), (virgule - (precvirgule + 18)));
                        precvirgule = virgule;

                    }
                    if (counter2 == 5)
                    {
                        v5 = message.Substring((precvirgule + 25), (virgule - 1 - (precvirgule + 25)));


                        string filePath = "C:\\MyImage.jpg";
                        File.WriteAllBytes(filePath, Convert.FromBase64String(v5));

                        break;
                    }
                    counter2++;
                }
                virgule++;
            }


            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=uchihastorage;AccountKey=jasd2PpZFhJDGGNoLL/Mk/+ThOtjGAdBkLcItpoghl1W3ghR+v0FuIBrzAVeOYlpc307KxwMxlaPD3vX7rJTpA==;EndpointSuffix=core.windows.net";
            CloudStorageAccount storageacc = CloudStorageAccount.Parse(storageConnectionString);

            CloudBlobClient blobClient = storageacc.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("uchihastorage");
            container.CreateIfNotExists();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference("11173.jpg");
            blockBlob.Properties.ContentType = "image/jpg";
            using (var filestream = System.IO.File.OpenRead(@"C:\\MyImage.jpg"))
            {
                blockBlob.UploadFromStream(filestream);
            }

            var account = new CloudStorageAccount(new StorageCredentials("uchihastorage", "jasd2PpZFhJDGGNoLL/Mk/+ThOtjGAdBkLcItpoghl1W3ghR+v0FuIBrzAVeOYlpc307KxwMxlaPD3vX7rJTpA=="), true);
            var cloudBlobClient = account.CreateCloudBlobClient();
            var containerv = cloudBlobClient.GetContainerReference("uchihastorage");
            var blob = container.GetBlockBlobReference("11173.jpg");


            var blobUrl = blob.Uri.AbsoluteUri;



            var values = new
            {
                LicensePlateCaptureTime = v1,
                LicensePlate = v2,
                Latitude = v3,
                Longitude = v4,
                ContextImageReference = blobUrl

            };


            Console.WriteLine(values);

            if (answer.Contains(v2))
            {
                var json = JsonConvert.SerializeObject(values);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://licenseplatevalidator.azurewebsites.net/api/lpr/platelocation";



                var response = await client.PostAsync(url, data);

                string result = response.Content.ReadAsStringAsync().Result;

                //                Console.WriteLine(result);
                Console.WriteLine(json);
            }

        }


        static async Task<string> GetRequest()
        {

            var byteArray = new UTF8Encoding().GetBytes("equipe03:f8xSZLJMVTyM3NGk");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var content = await client.GetStringAsync("https://licenseplatevalidator.azurewebsites.net/api/lpr/wantedplates");



            return content;

        }
    }
}
