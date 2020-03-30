This is an entry for the COVID 19 Global Hackathon and here is my submission https://devpost.com/software/virtual-queues/joins/nPOM17LJSaW8-W9WWF7r7g 

Our working theme is how we can enable social distancing using virtual queues powered by bot framework and help vulnerable population plan their essentials trips for grocery/pharmacy/health screening. 

## Inspiration
•	Social Distancing and Flattening the Curve. 
•	Develop a simple solution using Microsoft Bot Framework to showcase how Microsoft can enable every person on the planet during Crisis Situation.

## What it does
This solution helps people get essential services like grocery shopping, pharmacy pickups, healthcare screenings at wellness centers etc. not standing and waiting for their turn in a line. 

##Uniqueness
•	It integrates with all messaging and social platforms – SMS, Facebook, Teams, Slack etc.
•	Easy to implement with no setups or apps required both for customers or service providers. There are other waitlist apps in the market which requires a setup and mobile application, this is a self-managed queuing application. 

## How it works
Using Virtual Queues and Messaging people get a spot in a queue which advances automatically as the queue progress. 

Here is the workflow - 
Customer
Customer sends text to this common number and says  "Hi" and the bot takes over

•	Bot - Please tell where would you like to shop?
•	Customer - Target at Hillsborough
•	Bot - Confirms the location and address with a map.
•	Customer - Can narrow down the search if the location is not correct.
•	Bot - Sends a secret key (4 digit code) and tells the # in queue, and tells a tentative time to arrive.

Customer goes to the location and waits in the car for their turn to arrive and once they get the text they get out.
At the entrance they scan a barcode (printed on a paper) on the entrance with their phone which lets them ask their secret code. Customer enters the 4 digit code and the queue advances

No Shows
If the customer doesn't show in 5 minutes then the queue advances automatically placing the missed customer active for next 15 minutes and send them pings periodically after every queue movement. After 15 minutes they lose their location.

Service Provider Enrollment
Fill a web form with the following information
•	Name
•	Address (Geo Location verified with Bing Maps)
•	Business Timings
•	Average time it takes to service 1 customer.
•	Phone

Once the provider fills the form they get a text message to confirm 
•	Once the provider sends subscribe they are subscribed to this service.
•	When the provider sends unsubscribe they are unsubscribed from this service.
•	The provider can edit their information from a secured link.


