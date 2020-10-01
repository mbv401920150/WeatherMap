using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace DCU_Developer_Test
{
    public class Program
    {
        // CUSTOM API KEY (MICHAEL BOLANOS)
        private static readonly string API_KEY = "663b10460afb2cedbcede2fa5a013225";
        // Into each location was included the Country Code (US) to be more specific with the searchs
        private static readonly List<string> LOCATIONS = new List<string>
            (new string[] {
                "Marlboro,MA,US",
                "San Diego,CA,US",
                "Cheyenne,WY,US",
                "Anchorage,AK,US",
                "Austin,TX,US",
                "Orlando,FL,US",
                "Seattle,WA,US",
                "Cleveland,OH,US",
                "Portland,ME,US",
                "Honolulu,HI,US"
            });

        public static void Main()
        {
            GetWeather api = new GetWeather(API_KEY);
            
            Console.WriteLine("Current datetime: " + DateTime.Now.ToString("g") + "/n");

            foreach (var location in LOCATIONS)
            {
                try
                {
                    CityWeather result = api.getWeather(location);
                    printResult(result);
                }
                catch (Exception)
                {
                    Console.WriteLine("Something goes wrong when the application search the location: " + location);
                }
                
            }
        }

        private static void printResult(CityWeather res)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine(String.Format("{0} ({1})", res.Location, res.Id));
            Console.WriteLine("");
            Console.WriteLine("Date \t\t Avg Temp (F)");
            Console.WriteLine("-----------------------------");
            res.WeatherStats.ForEach(stat =>
            {
                string precipitation = stat.ChanceOfPrecipitation ? "*" : " ";
                Console.WriteLine(String.Format("{0}{1} \t {2}°", stat.Date, precipitation, stat.AverageTemp.ToString("#,##0.00")));
            });
            Console.WriteLine("");
        }
    }

    /// <summary> 
    /// Class to get the forecast of the next 5 days per location
    /// Accept an string with the following format: CITY,CITY_CODE,COUNTRY
    /// </summary>
    public class GetWeather
    {
        /// <summary> API Key of the Weather API </summary>
        public string apiKey { private get; set; }

        /// <summary> Base URL of the API Service </summary>
        private readonly string baseUrl = "http://api.openweathermap.org/data/2.5/forecast?q={LOCATION},HI&appid={APIKEY}&units=imperial";

        /// <summary> Inject the API Key </summary>
        /// <param name="apiKey">API Key (https://openweathermap.org/) </param>
        public GetWeather(string apiKey)
        {
            this.apiKey = apiKey;
        }

        /// <summary> Return the URL combined with the current city and the API key</summary>
        /// <param name="location">Location based on ({city name},{city code},{country code})</param>
        /// <returns>The complete URL to make call to the API</returns>
        private string getUrl(string location)
        {
            return baseUrl
                .Replace("{LOCATION}", location)
                .Replace("{APIKEY}", apiKey);
        }

        /// <summary> Get the Forecast of the Weather of the next 5 days of the selected location </summary>
        /// <param name="location">Use the format CITY,CITY_CODE,COUNTRY</param>
        /// <returns>An instance of the class CityWeather </returns>
        public CityWeather getWeather(string location)
        {
            try
            {
                // Create the URL to make the request
                string url = getUrl(location);

                // Create the new request using the location and the API Key
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                // Store the API result as JSON String
                string resultJsonString = "";

                // Execute the request
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                // Read the result as an Stream
                using (Stream stream = response.GetResponseStream())
                // Reader to get the result as String
                using (StreamReader reader = new StreamReader(stream))
                {
                    // String as JSON
                    resultJsonString = reader.ReadToEnd();
                }

                // Parse the JSON String to a WeatherResult class
                var weatherResult = JsonConvert.DeserializeObject<WeatherResult>(resultJsonString);

                // Create a new instace of CityWeather to return the result
                CityWeather result = new CityWeather();

                // Use LINQ to extract and calculate the forecast per days
                // The API return entries for 5 days, in a range of 3 hours
                // First the list is sorted by date ASC
                // The WHERE clause remove the current day
                // The LINQ group the data per date (without taking consideration the time)
                // The SELECT return the date, the average temp, and if exists or not chance of precipitation
                // TAKE will only return the first 5 days
                var stats = weatherResult
                    .list
                    .OrderBy(measure => measure.date)
                    .Where(measure => measure.date >= DateTime.Today.AddDays(1))
                    .GroupBy(measure => measure.date.ToString("MM/dd/yyyy"))
                    .Select(daily => new CityWeatherPerDay()
                    {
                        Date = daily.Key,
                        AverageTemp = Math.Round(daily.Average(m => m.main.temp_average), 2),
                        ChanceOfPrecipitation = daily.Average(m => m.pop) > 0
                    })
                    .Take(5);

                result.Id = weatherResult.city.id; // Add the City ID
                result.Location = location.Replace(",", ", "); // Beautify the string for final display
                result.WeatherStats.AddRange(stats); // Add 5 days

                // Return the result
                return result;
            }
            catch { throw; }
        }

    }

    /// <summary>
    /// Main class that will store the result of the forecast
    /// </summary>
    public class CityWeather
    {
        public CityWeather()
        {
            WeatherStats = new List<CityWeatherPerDay>();
        }

        public string Location { get; set; }
        public int Id { get; set; }

        public List<CityWeatherPerDay> WeatherStats { get; set; }
    }

    /// <summary>
    /// Class to store the forecast per each day
    /// </summary>
    public class CityWeatherPerDay
    {
        public string Date { get; set; }
        public double AverageTemp { get; set; }
        public bool ChanceOfPrecipitation { get; set; }
    }


    /// <summary> 
    /// Main class that will work as model to Parse the JSON String to an object
    /// It's fully recommended to allow manipulation through LINQ
    /// </summary>
    public class WeatherResult
    {
        public string cod { get; set; }
        public int message { get; set; }
        public int cnt { get; set; }
        public List<WheaterEntry> list { get; set; }
        public City city { get; set; }
    }

    /// <summary> 
    /// Weather information
    /// </summary>
    public class WheaterEntry
    {
        /// <summary>
        /// UnixEpoch date format
        /// </summary>
        public double dt { get; set; }
        public MainMeasures main { get; set; }
        public List<Wheater> weather { get; set; }
        public Clouds clouds { get; set; }
        public Wind wind { get; set; }
        public double visibility { get; set; }
        public float pop { get; set; }
        public SysWheater sys { get; set; }
        public string dt_txt { get; set; }

        /// <summary>
        /// Take the TimeStamp from the API result and return a DateTime format
        /// </summary>
        public DateTime date
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(dt);
            }
        }
    }

    /// <summary>
    /// Class with the temperature measures of each entry 
    /// </summary>
    public class MainMeasures
    {
        /// <summary> 
        /// Get the average of temp per each measure
        /// Necessary to calculate the average per day (Max and Min)
        /// </summary>
        public double temp_average
        {
            get
            {
                return Math.Round((temp_min + temp_max) / 2, 2);
            }
        }
        public float temp { get; set; }
        public float feels_like { get; set; }
        public float temp_min { get; set; }
        public float temp_max { get; set; }
        public float pressure { get; set; }
        public float sea_level { get; set; }
        public float grnd_level { get; set; }
        public float humidity { get; set; }
        public float temp_kf { get; set; }
    }

    #region " COMPLEMENTARY CLASSES TO READ CORRECTLY THE JSON DATA "
    public class Wheater
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }

    public class Coord
    {
        public float lat { get; set; }
        public float lon { get; set; }
    }

    public class Clouds
    {
        public int id { get; set; }
    }

    public class City
    {
        public int id { get; set; }
        public string name { get; set; }
        public Coord coord { get; set; }
        public string country { get; set; }
        public double population { get; set; }
        public int timezone { get; set; }
        public double sunrise { get; set; }
        public double sunset { get; set; }
    }

    public class Wind
    {
        public float speed { get; set; }
        public float deg { get; set; }
    }

    public class SysWheater
    {
        public string pod { get; set; }
    }

    #endregion
}