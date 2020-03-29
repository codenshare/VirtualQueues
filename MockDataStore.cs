using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MultiDialogsBot
{
    public class MockDataStore
    {
        public readonly List<Entity> Groceries;
        public readonly List<Entity> Pharmacies;
        public readonly List<Entity> Covid19testsites;

        public MockDataStore()
        {
            Groceries = new List<Entity>()
            {
                new Entity {Id=1, Name = "Super Target", Location = "Hillsborough County, Tampa FL 33635", Timings= "9 AM to 5 PM", Image="http://www.shawver.net/files/6413/3952/5703/IMG_0334.jpg", AvgDwellTimeInSeconds = 900, Capacity = 10},
                new Entity {Id=2,Name = "Joe's Farmer Market", Location = "Hillsborough County, Oldsmar FL 33637", Timings= "8 AM to 3 PM", Image = "http://joesfarmfreshmarket.yolasite.com/resources/100_0718.JPG.opt448x204o0%2C0s448x204.JPG", AvgDwellTimeInSeconds = 1000, Capacity = 6},
                new Entity {Id=3,Name = "Fresh Farms & Dairy", Location = "Hillsborough County, Tampa 33637", Timings= "7 AM to 11 AM", Image = "https://www.raisethehammer.org/static/images/ceretti_market_01.jpg", AvgDwellTimeInSeconds = 1200, Capacity = 10}
            };

            Pharmacies = new List<Entity>()
            {
                new Entity {Id=4,Name = "CVS Pharmacy", Location = "Hillsborough County, Tampa 33637", Timings= "9 AM to 5 PM", Image = "http://s.marketwatch.com/public/resources/MWimages/MW-DF172_CVS_20_ZG_20150209160431.jpg", AvgDwellTimeInSeconds = 1000, Capacity = 8},
                new Entity {Id=5,Name = "MinuteClinic", Location = "3771 Tampa Rd, Oldsmar FL 33637", Timings= "8 AM to 3 PM", Image = "https://cdn.trendhunterstatic.com/thumbs/cvs-healths-minuteclinic.jpeg", AvgDwellTimeInSeconds = 900, Capacity = 6},
                new Entity {Id=6,Name = "Walgreens", Location = "Hillsborough County, Tampa 33637", Timings= "7 AM to 11 AM", Image = "http://media.nj.com/middlesex_impact/photo/walgreensjpg-bf1891406f7cf2bd.jpg", AvgDwellTimeInSeconds = 900, Capacity = 6}
            };

            Covid19testsites = new List<Entity>()
            {
                new Entity {Id=7,Name = "Test Center 9780", Location = "Hillsborough County, Tampa 33637", Timings= "9 AM to 5 PM", Image = "http://s.marketwatch.com/public/resources/MWimages/MW-DF172_CVS_20_ZG_20150209160431.jpg", AvgDwellTimeInSeconds = 1000, Capacity = 6},
                new Entity {Id=8,Name = "County Health Screening Center", Location = "Tampa Rd, Oldsmar FL 33637", Timings= "8 AM to 3 PM", Image = "https://media.graytvinc.com/images/IMG_04933.JPG", AvgDwellTimeInSeconds = 900, Capacity = 6},
                new Entity {Id=9,Name = "Covid 19 Test Site 8922", Location = "Hillsborough County, Tampa 33637", Timings= "7 AM to 11 AM", Image = "https://img.republicworld.com/republic-prod/stories/promolarge/xxhdpi/atxn0gx778dqcax2_1584028151.jpeg?tr=f-jpeg", AvgDwellTimeInSeconds = 900, Capacity = 6}
            };

        }


    }
}