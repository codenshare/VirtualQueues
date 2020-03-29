using System;

namespace MultiDialogsBot
{
    internal class QueueItem
    {
        public string StoreID {get;set;}
        public string CustomerID { get; set; }
        public int ReAttempt { get; set; }
        public ConversationStarter QueueObject { get; set; }
        public DateTimeOffset InsertedAt { get; set; }

    }
}