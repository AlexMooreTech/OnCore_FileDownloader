using System;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;




// This program retrieves files from the OnCore file servers using information provided
// for this test.

//I had to use the System.Drawing.Common package as the default System.Drawing package does not directly support image processing in .NET 6.

namespace OnCore_FileDownloader
{
    class Program
    {

        static void Main(string[] args)
        {
            //init the statemachine and let the change of "states" change program output
            Console.WriteLine("Welcome to the OnCore Test Downloader!");
            StateMachine();
        }

        public static void StateMachine()
        {
            //A very simple state machine that will swich between user input and application runtime
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("\n To download the default image press the D key\n To download a custom image from the encore API press the C key\n To exit press the X key\n");

            var input = Console.ReadKey();
            char key = input.KeyChar;
            switch (key)
            {
                case 'd':
                case 'D':
                    Console.WriteLine("\n");
                    DownloadImage("pass.jpg");
                    break;
                case 'c':
                case 'C':
                    Console.WriteLine("\n");
                    Console.WriteLine("Enter your custom image path here: ");
                    var userInput = Console.ReadLine();
                    if (userInput != null)
                    {
                        DownloadImage(userInput);
                    }
                    else
                    {
                        Console.WriteLine("ERROR your value is null\n\n");
                        StateMachine();
                    }
                    break;
                case 'x':
                case 'X':
                    Console.WriteLine(" Bye!\n");
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Not a Valid Key");
                    StateMachine();
                    break;
            }
        }


        public static void DownloadImage(string imageName)
        {
            var endpoint = "https://api.test.oncoreflex.dev/handover/stream/" + imageName;
            var OAuthHTTP = "https://auth.test.oncoreflex.dev/oauth/token";
            var clientID = "uh6T8hfuxmHvlNnEfpN5MJ8FQkReQKgP";
            var clientSecret = "JvkY5pod2WzFyncW4xwUsvc2Y9ciiV91eeAVxG2xROsy1AkLslfvuoAGY4I_k1lv";

            try
            {
                //getting a token for each connection is slow. to make this faster having the application re-use tokens within the allocated time frame should work.
                Console.WriteLine("Getting token!");
                var tokenReturn = GetBearerToken(OAuthHTTP, clientID, clientSecret).Result;
                Dictionary<string, string> tokenDir = DeSerialize(tokenReturn.ToString());
                Console.WriteLine("Got token\n");

                //get the value of the returned token to use for API auth
                string token = tokenDir.ElementAt(0).Value;
                //get value of token type
                string tokenType = tokenDir.ElementAt(3).Value;


                //setup a wait for the async request.
                Console.WriteLine("Getting images using API");
                var imageStream = HttpGetAsyncImage(token, endpoint).Result;
                var img = System.Drawing.Image.FromStream(imageStream);

                var userName = Environment.UserName;
                string filePath = System.IO.Path.GetFullPath("C:\\Users\\" + userName + "\\Pictures\\" + imageName);
                Console.WriteLine("Saving image at: " + filePath);
                img.Save(filePath, ImageFormat.Jpeg);

                Console.WriteLine("Image downloaded. Have a great day.");
                StateMachine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR - address not found");
                Console.WriteLine(ex.Message);
                StateMachine();
            }
        }


        //could not find a built-in json deserialiser so I wrote this basic one
        public static Dictionary<string, string> DeSerialize(string Data)
        {

            var returnDir = new Dictionary<string, string>
            {

            };
            //sift through returned data and package into an array
            //limiting to 20 for now. Most returns wont be above 10 categories
            string[] DataArray = new string[20];
            var dataPosition = 0;
            var ArrayTotal = 0;

            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i] == ':' || Data[i] == ',')
                {
                    try
                    {
                        //calculate length and position of substring.
                        var stringx = dataPosition + 2;
                        var stringx2 = i - dataPosition - 3;

                        //create a clone and Substring to the required coordinates to add into a Dictionary. the + and - values will cut the " out of the string
                        string insertString = Data.Substring(stringx, stringx2);
                        dataPosition = i;
                        DataArray[ArrayTotal] = insertString;
                        ArrayTotal++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            }

            //package into Dictionary
            for (int i = 0; i < ArrayTotal; i += 2)
            {
                returnDir.Add(DataArray[i], DataArray[i + 1]);
            }
            return returnDir;
        }


        //Auth0.com has so much useful info on this.
        static async Task<string> GetBearerToken(string authHTTP, string clientID, string clientSecret)
        {
            Console.WriteLine("Getting Bearer Token");

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(authHTTP);

                var packetData = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", clientID},
                    {"client_secret", clientSecret},
                    {"audience", "https://api.test.oncoreflex.dev"},
                };

                var requestData = new FormUrlEncodedContent(packetData);
                Console.WriteLine("Connecting to:");
                Console.WriteLine(client.BaseAddress);

                var response = await client.PostAsync(client.BaseAddress, requestData);

                var download = await response.Content.ReadAsStringAsync();
                return download;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get Token!");
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        //get image using token and api path
        static async Task<Stream> HttpGetAsyncImage(string token, string endpoint)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(endpoint);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                Console.WriteLine("Connecting to:");
                Console.WriteLine(client.BaseAddress);

                var response = await client.GetAsync(client.BaseAddress);
              
                var download = await response.Content.ReadAsStreamAsync();
                return download;
            }
            catch (Exception ex)
            {
                Console.WriteLine("API call failed");
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
