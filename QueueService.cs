using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;


using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.Azure.Storage; // Namespace for CloudStorageAccount
using Microsoft.Azure.Storage.Queue; // Namespace for Queue storage types
using Newtonsoft.Json;

namespace MultiDialogsBot
{
    public class QueueService
    {
        // static List<Queue<QueueItem>> storeQueues = new List<Queue<QueueItem>>();

        private CloudStorageAccount storageAccount;
        private CloudQueueClient queueClient;
        private ConversationStarter conversationStarterClient;
        private MockDataStore mockStore;

        public QueueService()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("StorageConnectionString"));
            queueClient = storageAccount.CreateCloudQueueClient();
            conversationStarterClient = new ConversationStarter();
            mockStore = new MockDataStore();
        }

        public EstimateWait InsertInQueue(string CustomerID, string StoreID, ConversationStarter obj)
        {
            try
            {
                // Retrieve a reference to a container.
                CloudQueue queue = queueClient.GetQueueReference($"{StoreID}wait");
                queue.CreateIfNotExists();

                CloudQueue queue1 = queueClient.GetQueueReference($"{StoreID}invite");
                queue1.CreateIfNotExists();

                queue1.FetchAttributes();
                int? invitequeuecount = queue1.ApproximateMessageCount;
                if (invitequeuecount == null)
                    invitequeuecount = 0;


                CloudQueue queue2 = queueClient.GetQueueReference($"{StoreID}instore");
                queue2.CreateIfNotExists();

                QueueItem q = new QueueItem();
                q.CustomerID = CustomerID;
                q.QueueObject = obj;
                q.StoreID = StoreID;
                q.ReAttempt = 0;

                string output = JsonConvert.SerializeObject(q);

                CloudQueueMessage message = new CloudQueueMessage(output);

                queue.AddMessage(message);

                queue.FetchAttributes();
                int? waitqueuecount = queue.ApproximateMessageCount;
                if (waitqueuecount == null)
                    waitqueuecount = 0;

                EstimateWait eW = new EstimateWait();

                int avgdwelltime = (mockStore.GetEntity(Convert.ToInt32(StoreID)).AvgDwellTimeInSeconds )/ 60;

                eW.waitimeinmins = (waitqueuecount + 1) * avgdwelltime;
                eW.position = waitqueuecount + invitequeuecount;

                return eW;
            }
            catch (Exception ex)
            { return null; }

        }

        public bool InviteCustomer(string StoreID)
        {
            try
            {
                CloudQueue queueWait = queueClient.GetQueueReference($"{StoreID}wait");
                queueWait.CreateIfNotExists();

                CloudQueue queueInvite = queueClient.GetQueueReference($"{StoreID}invite");
                queueInvite.CreateIfNotExists();

                CloudQueueMessage inviteQueueMessage = queueInvite.GetMessage();

                if (inviteQueueMessage != null)
                {
                    TimeSpan? timediff = DateTimeOffset.UtcNow - inviteQueueMessage.InsertionTime;
                    QueueItem deserializeinviteQueueMessage = JsonConvert.DeserializeObject<QueueItem>(inviteQueueMessage.AsString);

                    if (timediff.Value.TotalSeconds > 30) //wait for 30 seconds
                    {
                        if (deserializeinviteQueueMessage.ReAttempt < 4)
                        {
                            CloudQueueMessage waitQueueMessage = queueWait.GetMessage();
                            if (waitQueueMessage != null)
                            {
                                queueInvite.AddMessage(CreateNewMessage(waitQueueMessage));
                                queueWait.DeleteMessage(waitQueueMessage.Id, waitQueueMessage.PopReceipt);
                            }
                            queueInvite.DeleteMessage(inviteQueueMessage.Id, inviteQueueMessage.PopReceipt);

                            deserializeinviteQueueMessage.ReAttempt++;
                            string output = JsonConvert.SerializeObject(deserializeinviteQueueMessage);
                            CloudQueueMessage message = new CloudQueueMessage(output);
                            queueInvite.AddMessage(message);

                            //Message the last customer in the invite queue for their next turn - use peekmessage
                            MessageCustomer(queueInvite.PeekMessage(), $"You are next, please arrive in next 30 seconds. If you miss to reach, we will keep your spot for next 2 minutes and look for the message. Use code {GetCode(queueInvite.PeekMessage())}  ");

                            //}
                        }
                        else
                        {
                            queueInvite.DeleteMessage(inviteQueueMessage.Id, inviteQueueMessage.PopReceipt);

                            //Message this deleted person that they have been removed from the queue
                            MessageCustomer(inviteQueueMessage, "You are removed from the queue, please queue again");

                        }
                    }
                    else
                    {
                        //Message the last customer in the invite queue that we are waiting for them to come else they will lose the spot - use peekmessage
                        MessageCustomer(inviteQueueMessage, $"We are waiting for you, You are next, please arrive soon else you lose the spot. Use code {GetCode(inviteQueueMessage)} ");
                    }
                }
                else
                {
                    CloudQueueMessage waitQueueMessage = queueWait.GetMessage();
                    if (waitQueueMessage != null)
                    {
                        queueInvite.AddMessage(CreateNewMessage(waitQueueMessage));
                        queueWait.DeleteMessage(waitQueueMessage.Id, waitQueueMessage.PopReceipt);

                        //Message the last customer in the invite queue for their next turn - use peekmessage
                        MessageCustomer(queueInvite.PeekMessage(), $"You are next, please arrive in 5 minutes. Use code {GetCode(queueInvite.PeekMessage())} ");
                    }

                }
                return true;
            }
            catch (Exception ex)
            { return false; }

        }

        CloudQueueMessage CreateNewMessage(CloudQueueMessage message)
        {
            QueueItem deserializeQueueMessage = JsonConvert.DeserializeObject<QueueItem>(message.AsString);
            string output = JsonConvert.SerializeObject(deserializeQueueMessage);
            return (new CloudQueueMessage(output));

        }

        string GetCode(CloudQueueMessage message)
        {
            QueueItem deserializeQueueMessage = JsonConvert.DeserializeObject<QueueItem>(message.AsString);
            return (deserializeQueueMessage.CustomerID);
        }

        void MessageCustomer(CloudQueueMessage sendto, string notification)
        {
            QueueItem deserializeMessage = JsonConvert.DeserializeObject<QueueItem>(sendto.AsString);
            conversationStarterClient = (ConversationStarter)(deserializeMessage.QueueObject);
            conversationStarterClient.Resume(notification);

        }

        public bool CustomerEntersTheStore(string SecretCode, string StoreID)
        {
            try
            {
                CloudQueue queueInvite = queueClient.GetQueueReference($"{StoreID}invite");
                queueInvite.CreateIfNotExists();

                CloudQueue queueInStore = queueClient.GetQueueReference($"{StoreID}instore");
                queueInStore.CreateIfNotExists();

                bool condition = false;
                bool authorized = false;

                while (condition == false)
                {
                    CloudQueueMessage retrievedMessage = queueInvite.GetMessage();
                   
                    if (retrievedMessage == null)
                    { condition = true; authorized = false; }
                    else
                    {
                        QueueItem deserializedqI = JsonConvert.DeserializeObject<QueueItem>(retrievedMessage.AsString);
                        if (deserializedqI.CustomerID == SecretCode)
                        {
                            queueInStore.AddMessage(CreateNewMessage(retrievedMessage));
                            queueInvite.DeleteMessage(retrievedMessage.Id, retrievedMessage.PopReceipt);
                            MessageCustomer(retrievedMessage, $"CHECKED IN: Enjoy!");

                            condition = true; authorized = true;
                        }
                        else
                        { condition = false; authorized = false; }
                    }
                }

                return authorized;

            }
            catch (Exception ex)
            { return false; }

        }

        public void MonitorInviteQueues()
        {
            try
            {
                List<int> StoreIds = new List<int>();
                StoreIds = mockStore.GetEntities();

                foreach (var StoreID in StoreIds)
                {
                    CloudQueue queueWait = queueClient.GetQueueReference($"{StoreID}wait");
                    queueWait.CreateIfNotExists();

                    CloudQueue queueInvite = queueClient.GetQueueReference($"{StoreID}invite");
                    queueInvite.CreateIfNotExists();

                    CloudQueueMessage inviteQueueMessage = queueInvite.GetMessage();

                    if (inviteQueueMessage != null)
                    {
                        TimeSpan? timediff = DateTimeOffset.UtcNow - inviteQueueMessage.InsertionTime;
                        QueueItem deserializeinviteQueueMessage = JsonConvert.DeserializeObject<QueueItem>(inviteQueueMessage.AsString);

                        double timeDiff = timediff.Value.TotalSeconds;

                        if (timeDiff > 60) //wait for 60 seconds
                        {

                            queueInvite.DeleteMessage(inviteQueueMessage.Id, inviteQueueMessage.PopReceipt);
                            MessageCustomer(inviteQueueMessage, "QUEUE EXPIRED: We waited you for 2 minutes. Your spot expired, Please queue again");

                            //if (deserializeinviteQueueMessage.ReAttempt < 4)
                            //{
                            //    queueInvite.DeleteMessage(inviteQueueMessage.Id, inviteQueueMessage.PopReceipt);

                            //    deserializeinviteQueueMessage.ReAttempt++;
                            //    string output = JsonConvert.SerializeObject(deserializeinviteQueueMessage);
                            //    CloudQueueMessage message = new CloudQueueMessage(output);
                            //    queueInvite.AddMessage(message);

                            //    //Message the last customer in the invite queue for their next turn - use peekmessage
                            //    MessageCustomer(queueInvite.PeekMessage(), $"You are next, please arrive in next 30 seconds. If you miss to reach, we will keep your spot for next 2 minutes and look for the message. Use code {GetCode(queueInvite.PeekMessage())}  ");

                            //    //}
                            //}
                            //else
                            //{
                            //    queueInvite.DeleteMessage(inviteQueueMessage.Id, inviteQueueMessage.PopReceipt);

                            //    //Message this deleted person that they have been removed from the queue
                            //    MessageCustomer(inviteQueueMessage, "You are removed from the queue, please queue again");

                            //}
                        }
                        else if ( (timeDiff <= 60) && (timeDiff >0))
                        {
                            //Message the last customer in the invite queue that we are waiting for them to come else they will lose the spot - use peekmessage
                            MessageCustomer(inviteQueueMessage, $"WAITING: We are waiting for you, Time left to check-in: {string.Format("{0:0}", timeDiff)} seconds. Use code {GetCode(inviteQueueMessage)} ");
                        }
                    }
                    //else
                    //{
                    //    CloudQueueMessage waitQueueMessage = queueWait.GetMessage();
                    //    if (waitQueueMessage != null)
                    //    {
                    //        queueInvite.AddMessage(CreateNewMessage(waitQueueMessage));
                    //        queueWait.DeleteMessage(waitQueueMessage.Id, waitQueueMessage.PopReceipt);

                    //        //Message the last customer in the invite queue for their next turn - use peekmessage
                    //        MessageCustomer(queueInvite.PeekMessage(), $"You are next, please arrive in 5 minutes. Use code {GetCode(queueInvite.PeekMessage())} ");
                    //    }

                    //}
                }
         
            }
            catch (Exception ex)
            {  }

        }

        public void MonitorInStoreQueues()
        {
            try
            { 
                List<int> StoreIds = new List<int>();
                StoreIds = mockStore.GetEntities();

                foreach (var StoreID in StoreIds)
                { 
                        var StoreEntity = mockStore.GetEntity(Convert.ToInt32(StoreID));

                        CloudQueue queueWait = queueClient.GetQueueReference($"{StoreID}wait");
                        queueWait.CreateIfNotExists();

                        CloudQueue queueInvite = queueClient.GetQueueReference($"{StoreID}invite");
                        queueInvite.CreateIfNotExists();

                        CloudQueue queueInStore = queueClient.GetQueueReference($"{StoreID}instore");
                        queueInStore.CreateIfNotExists();

                        bool loopbreak = false;

                        while (loopbreak == false)
                        {
                            CloudQueueMessage inStoreQueueMessage = queueInStore.GetMessage();
                            if (inStoreQueueMessage != null)
                            {
                                TimeSpan? timediff = DateTimeOffset.UtcNow - inStoreQueueMessage.InsertionTime;
                                QueueItem deserializeinviteQueueMessage = JsonConvert.DeserializeObject<QueueItem>(inStoreQueueMessage.AsString);

                                if (timediff.Value.TotalSeconds > StoreEntity.AvgDwellTimeInSeconds)
                                {
                                    queueInStore.DeleteMessage(inStoreQueueMessage.Id, inStoreQueueMessage.PopReceipt);
                                }
                                else
                                    loopbreak = true;
                            }
                            else
                                loopbreak = true;
                        }

                        queueInStore.FetchAttributes();
                        int? waitqueuecount = queueInStore.ApproximateMessageCount;
                        int waitcount;

                        if (waitqueuecount == null)
                            waitcount = 0;
                        else
                            waitcount = (int)waitqueuecount;


                        queueInvite.FetchAttributes();
                        int? waitqueueInvitecount = queueInvite.ApproximateMessageCount;
                        int waitInvitecount;

                        if (waitqueueInvitecount == null)
                            waitInvitecount = 0;
                        else
                            waitInvitecount = (int)waitqueueInvitecount;


                    int RemainingCapacity = StoreEntity.Capacity - waitcount - waitInvitecount;

                        if (RemainingCapacity > 0)
                        {
                            for (int i = 0; i < RemainingCapacity; i++)
                            {
                                CloudQueueMessage waitQueueMessage = queueWait.GetMessage();
                                if (waitQueueMessage != null)
                                {
                                    queueInvite.AddMessage(CreateNewMessage(waitQueueMessage));
                                    queueWait.DeleteMessage(waitQueueMessage.Id, waitQueueMessage.PopReceipt);
                                    MessageCustomer(waitQueueMessage, $"COME IN: You are next, Please check-in. Use code {GetCode(waitQueueMessage)}");

                                }
                                else
                                    i = RemainingCapacity; //exit for loop
                            }
                        }
                    }
            }
            catch (Exception ex)
                {  }

        }
    }
}