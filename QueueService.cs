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

        public QueueService()
        {
          storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("StorageConnectionString"));
          queueClient = storageAccount.CreateCloudQueueClient();
          conversationStarterClient = new ConversationStarter();
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
                int? invitequeuecount = queue1.ApproximateMessageCount;

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

                int? waitqueuecount = queue.ApproximateMessageCount;

                EstimateWait eW = new EstimateWait();
                eW.waitimeinmins = (waitqueuecount + 1) * 15;
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

                CloudQueueMessage inviteQueueMessage = queueInvite.PeekMessage();

                if (inviteQueueMessage != null)
                {
                    TimeSpan? timediff = DateTimeOffset.UtcNow - inviteQueueMessage.InsertionTime ;
                    QueueItem deserializeinviteQueueMessage = JsonConvert.DeserializeObject<QueueItem>(inviteQueueMessage.AsString);
                    if (timediff.Value.TotalSeconds > 300) //wait for 5 minutes
                    {
                        if (deserializeinviteQueueMessage.ReAttempt < 2)
                        {
                            CloudQueueMessage waitQueueMessage = queueWait.PeekMessage();
                            if (waitQueueMessage != null)
                            {
                                queueInvite.AddMessage(waitQueueMessage);
                                //queueWait.DeleteMessage(waitQueueMessage);

                                queueInvite.DeleteMessage(inviteQueueMessage);
                                deserializeinviteQueueMessage.ReAttempt++;

                                string output = JsonConvert.SerializeObject(deserializeinviteQueueMessage);
                                CloudQueueMessage message = new CloudQueueMessage(output);
                                queueInvite.AddMessage(message);

                                //Message the last customer in the invite queue for their next turn - use peekmessage
                                QueueItem deserializeMessage = JsonConvert.DeserializeObject<QueueItem>(queueInvite.PeekMessage().AsString);
                                conversationStarterClient = (ConversationStarter)(deserializeMessage.QueueObject);
                                conversationStarterClient.Resume("You are next, please arrive in 5 minutes. Use code 7893 ");
                            }
                        }
                        else
                        {
                            queueInvite.DeleteMessage(inviteQueueMessage);
                            
                            //Message this deleted person that they have been removed from the queue
                            QueueItem deserializeMessage = JsonConvert.DeserializeObject<QueueItem>(inviteQueueMessage.AsString);
                            conversationStarterClient = (ConversationStarter)(deserializeMessage.QueueObject);
                            conversationStarterClient.Resume("You are removed from the queue, please queue again");
                        }
                    }
                    else
                    {
                        //Message the last customer in the invite queue that we are waiting for them to come else they will lose the spot - use peekmessage
                        QueueItem deserializeMessage = JsonConvert.DeserializeObject<QueueItem>(queueInvite.PeekMessage().AsString);
                        conversationStarterClient = (ConversationStarter)(deserializeMessage.QueueObject);
                        conversationStarterClient.Resume("We are waiting for you, You are next, please arrive soon else you lose the spot. Use code 7893 ");
                    }
                }
                else
                {
                    CloudQueueMessage waitQueueMessage = queueWait.GetMessage();
                    if (waitQueueMessage != null)
                    {
                        queueInvite.AddMessage(waitQueueMessage);
                        queueWait.DeleteMessage(waitQueueMessage.Id, waitQueueMessage.PopReceipt);

                        //Message the last customer in the invite queue for their next turn - use peekmessage
                        QueueItem deserializeMessage = JsonConvert.DeserializeObject<QueueItem>(queueInvite.PeekMessage().AsString);
                        conversationStarterClient = (ConversationStarter)(deserializeMessage.QueueObject);
                        conversationStarterClient.Resume("You are next, please arrive in 5 minutes. Use code 7893 ");
                    }

                }
                return true;
            }
            catch (Exception ex)
            { return false; }

        }


        public bool CustomerEntersTheStore(string CustomerID, string StoreID)
        {
            try
            {
                CloudQueue queueInvite = queueClient.GetQueueReference($"{StoreID}invite");
                queueInvite.CreateIfNotExists();

                CloudQueue queueInStore = queueClient.GetQueueReference($"{StoreID}instore");
                queueInStore.CreateIfNotExists();


                CloudQueueMessage retrievedMessage = queueInvite.PeekMessage();
                QueueItem deserializedqI = JsonConvert.DeserializeObject<QueueItem>(retrievedMessage.AsString);


                if (retrievedMessage != null && deserializedqI.CustomerID == CustomerID)
                {
                    queueInStore.AddMessage(retrievedMessage);
                    queueInvite.DeleteMessage(retrievedMessage);
                    InviteCustomer(StoreID);
                    return true;
                }
                else
                    return false;
                
            }
            catch (Exception ex)
            { return false; }

        }




    }
}