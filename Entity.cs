namespace MultiDialogsBot
{
    using System;

    [Serializable]
    public class Entity
    {
        public string Name { get; set; }

        public string Timings { get; set; }

        //public int NumberOfReviews { get; set; }

        //public int PriceStarting { get; set; }

        public string Image { get; set; }

        public string Location { get; set; }

        public int Id { get; set; }

        public int AvgDwellTimeInSeconds {get;set;}

        public int Capacity { get; set; }


    }
}