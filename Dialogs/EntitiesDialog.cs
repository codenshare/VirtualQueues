namespace MultiDialogsBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Connector;
    using Newtonsoft.Json;

    [Serializable]
    public class EntitiesDialog : IDialog<object>
    {
        private string Option;

        public EntitiesDialog(string option)
        {
            this.Option = option;    
        }

        public async Task StartAsync(IDialogContext context)
        {
            // await context.PostAsync($"Welcome to the {Option} finder!");

            // var hotelsFormDialog = FormDialog.FromForm(this.BuildEntitiesForm, FormOptions.PromptInStart);

            //  context.Call(hotelsFormDialog, this.ResumeAfterEntitiesFormDialog);

            await this.ShowProducts(context);
            context.Wait(this.MessageReceivedAsync);

        }

        private IForm<EntitiesQuery> BuildEntitiesForm()
        {
            OnCompletionAsyncDelegate<EntitiesQuery> processHotelsSearch = async (context, state) =>
            {
                await context.PostAsync($"Ok. Searching for {Option} in {state.Destination}");  // from {state.CheckIn.ToString("MM/dd")} to {state.CheckIn.AddDays(state.Nights).ToString("MM/dd")}...
            };

            return new FormBuilder<EntitiesQuery>()
                .Field(nameof(EntitiesQuery.Destination))
                .Message("Looking in {Destination}...")
                .AddRemainingFields()
                .OnCompletion(processHotelsSearch)
                .Build();
        }

        // private async Task ResumeAfterEntitiesFormDialog(IDialogContext context, IAwaitable<EntitiesQuery> result)
        protected async Task ShowProducts(IDialogContext context)
        {
            try
            {
                //var searchQuery = await result;
                var entities = this.GetEntitiesAsync(); //searchQuery
                //await context.PostAsync($"I found in total {entities.Count()} {Option}");  //hotels for your dates:

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                foreach (var entity in entities)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = entity.Name,
                        Subtitle = $"{entity.Timings} Location: {entity.Location}", // per night.
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = entity.Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Queue",
                                Type = ActionTypes.ImBack,
                                Value = entity.Id.ToString()
                            }
                        }
                    };
                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }
                await context.PostAsync(resultMessage);
            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = $"You have canceled the operation. Quitting from the {Option} Dialog";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
            finally
            {
                //context.Wait(this.MessageReceivedAsync);
                //  context.Done<object>(null);
            }
        }



        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            var storeID = message.Text;

            ConversationStarter cS = new ConversationStarter();
            cS.toId = message.From.Id;
            cS.toName = message.From.Name;
            cS.fromId = message.Recipient.Id;
            cS.fromName = message.Recipient.Name;
            cS.serviceUrl = message.ServiceUrl;
            cS.channelId = message.ChannelId;
            cS.conversationId = message.Conversation.Id;


            QueueService qS = new QueueService();
            var response = qS.InsertInQueue(message.From.Id, storeID, cS);

            await context.PostAsync($"You are position# {response.position} in the queue and have a wait time of {response.waitimeinmins} minutes. We will text you when to come"); 

            context.Done<object>(null);
        }




        private List<MultiDialogsBot.Entity> GetEntitiesAsync() //EntitiesQuery searchQuery
        {
            var entities = new List<MultiDialogsBot.Entity>();

            MockDataStore ms = new MockDataStore();

            if (Option == "Groceries")
                entities = ms.Groceries;
            if (Option == "Pharmacy")
                entities = ms.Pharmacies;
            if (Option == "Covid 19 Screening")
                entities = ms.Covid19testsites;

                    //Bing Image Search API
                    //SearchResult result = BingImageSearch(Option);
                    //deserialize the JSON response from the Bing Image Search API
                    //dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result.jsonResult);
                    //var val = jsonObj["value"];

                    //SearchResult result1 = BingLocalSearch($"{ Option} near {searchQuery.Destination}");
                    //dynamic jsonObj1 = Newtonsoft.Json.JsonConvert.DeserializeObject(result1.jsonResult);
                    //var val1 = jsonObj1["value"];



                    // Filling the hotels results manually just for demo purposes
                    //for (int i = 1; i <= 5; i++)
                    //{
                    //    var random = new Random(i);
                    //    Hotel hotel = new Hotel()
                    //    {
                    //        Name = $"{searchQuery.Destination} Hotel {i}",
                    //        Location = searchQuery.Destination,
                    //        Timings = random.Next(1, 5),
                    //        NumberOfReviews = random.Next(0, 5000),
                    //        PriceStarting = random.Next(80, 450),
                    //        Image = jsonObj["value"][i - 1]["contentUrl"]   // $"https://placehold.imgix.net/~text?txtsize=35&txt={Option}+{i}&w=500&h=260"
                    //};

                    //    hotels.Add(hotel);
                    //}

                    //hotels.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

                    return entities;
        }

    





        struct SearchResult
        {
            public string jsonResult;
            public Dictionary<String, String> relevantHeaders;
        }


        private const string APIKey = "6261ebab920749979a52312820dee34c";
        static SearchResult BingImageSearch(string searchQuery)
        {
            string uriBase = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

            // Construct the URI of the search request
            var uriQuery = uriBase + "?q=" + Uri.EscapeDataString(searchQuery);

            // Perform the Web request and get the response
            WebRequest request = WebRequest.Create(uriQuery);
            request.Headers["Ocp-Apim-Subscription-Key"] = APIKey;
            HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create result object for return
            var searchResult = new SearchResult()
            {
                jsonResult = json,
                relevantHeaders = new Dictionary<String, String>()
            };

            // Extract Bing HTTP headers
            foreach (String header in response.Headers)
            {
                if (header.StartsWith("BingAPIs-") || header.StartsWith("X-MSEdge-"))
                    searchResult.relevantHeaders[header] = response.Headers[header];
            }

            return searchResult;
        }

        private async Task<SearchResult> BingLocalSearch(string searchQuery)
        {
            const string uriBase = "https://api.cognitive.microsoft.com/bing/v7.0/localbusinesses/search";
            // Construct the URI of the search request
            var uriQuery = uriBase + "?q=" + Uri.EscapeDataString(searchQuery) + "&mkt=en-us";

            // Perform the Web request and get the response
            WebRequest request = HttpWebRequest.Create(uriQuery);
            request.Headers["Ocp-Apim-Subscription-Key"] = APIKey;

            HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create result object for return
            var searchResult = new SearchResult();
            searchResult.jsonResult = json;
            searchResult.relevantHeaders = new Dictionary<String, String>();

            // Extract Bing HTTP headers
            foreach (String header in response.Headers)
            {
                if (header.StartsWith("BingAPIs-") || header.StartsWith("X-MSEdge-"))
                    searchResult.relevantHeaders[header] = response.Headers[header];
            }

            return searchResult;
        }

  
    }
}



