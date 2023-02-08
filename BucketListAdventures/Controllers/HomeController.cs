
﻿using BucketListAdventures.Data;
using BucketListAdventures.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SearchActivities.ViewModel;
using System.Diagnostics;
using System.Linq;
using System;
using static BucketListAdventures.Models.ClimateNormals;


namespace BucketListAdventures.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserProfileRepository _repository;
        private readonly ILogger<HomeController> _logger;
        private static JArray data;

        private ApplicationRepository _repo;
        private readonly IConfiguration _config;
        private ClimateNormals climateNormals = new ClimateNormals();
        private static string travelAdvisorApiKey;
        public HomeController(ILogger<HomeController> logger, ApplicationRepository repo, IUserProfileRepository repository, IConfiguration config)
        {
            _logger = logger;
            _repo = repo;
            _repository = repository;
            _config = config;
            travelAdvisorApiKey = _config["travelAdvisorApiKey"];
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [Route("/home/search")]
        public IActionResult Search()
        {
            SearchViewModel searchViewModel = new();
            return View(searchViewModel);
        }
        [HttpGet]
        [Route("/home/navigate")]
        public IActionResult Navigate()
        {
            SearchViewModel searchViewModel = new();
            return View(searchViewModel);
        }


        public static async Task<JObject> GetLatLong(string city)
        {
            string accessToken = "pk.eyJ1IjoiY2hhbWFuZWJhcmJhdHRpIiwiYSI6ImNsY3FqcW9rZTA2aW4zcXBoMGx2eTBwNm0ifQ.LFRkBS7N5yGXvCQ_F5cF9g";
            HttpClient clientName = new();
            string url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{city}.json?types=place,locality&country=us&access_token={accessToken}";
            HttpResponseMessage responseName = await clientName.GetAsync(url);
            string responseString = await responseName.Content.ReadAsStringAsync();
            JObject position = JObject.Parse(responseString);
            return position;
        }

        public async Task<string> GetAirPortDetails(string destination)
        {
            var client = new HttpClient();

            HttpRequestMessage request = GetHeaderRequest(destination);
            using var response = await client.SendAsync(request);
            Environment.GetCommandLineArgs();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JArray value = JArray.Parse(body);
            //data = value[0];
            return value[0]["code"].ToString();
        }

        public async Task<JToken> GetFlightDetails(string origin, string destination, DateTime startDate, int totalTravellers)
        {
            var client = new HttpClient();

            HttpRequestMessage request = GetFlightHeaderRequest(origin, destination, startDate, totalTravellers);
            using var response = await client.SendAsync(request);
            Environment.GetCommandLineArgs();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JToken value = JToken.Parse(body);
            //data = value[0];
            return value["airports"];
        }

        private HttpRequestMessage GetFlightHeaderRequest(string origin, string destination, DateTime startDate, int totalTravellers)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/flights/create-session?o1={origin}&d1={destination}&dd1={startDate.ToString("yyyy-MM-dd")}&ta={totalTravellers}&c=0"),
                Headers =
                    {
                        { "X-RapidAPI-Key", travelAdvisorApiKey },
                        { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                    },
            };
        }

        private HttpRequestMessage GetHeaderRequest(string destination)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/airports/search?query={destination}&locale=en_US"),
                Headers =
                    {
                        { "X-RapidAPI-Key", travelAdvisorApiKey },
                        { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                    },
            };
        }

        public static async Task<JArray> GetActivities(double lon, double lat)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/attractions/list-by-latlng?longitude={lon}&latitude={lat}&lunit=km&currency=USD&lang=en_US"),
                Headers =

                {
                    { "X-RapidAPI-Key", travelAdvisorApiKey },
                    { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                },

            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JObject value = JObject.Parse(body);
            data = (JArray)value["data"];
            return data;
        }
        public static async Task<JArray> GetNavigation(double lon, double lat)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.mapbox.com/directions/v5/mapbox/driving/-90.199585,38.626426;{lon},{lat}?geometries=geojson&access_token=pk.eyJ1IjoiY2hhbWFuZWJhcmJhdHRpIiwiYSI6ImNsY3FqcW9rZTA2aW4zcXBoMGx2eTBwNm0ifQ.LFRkBS7N5yGXvCQ_F5cF9g"),
            
               
            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JObject value = JObject.Parse(body);
            
     
            
            data = (JArray)value["routes"];
            return data;
        }
        [HttpPost]
        [Route("/home/search")]
        public IActionResult DisplayResults(SearchViewModel searchViewModel)
        {
            Task<JObject> LatLong = GetLatLong(searchViewModel.CityName);
            JObject LatlongObject = LatLong.Result;
            double lon = (double)LatlongObject["features"][0]["geometry"]["coordinates"][0];
            double lat = (double)LatlongObject["features"][0]["geometry"]["coordinates"][1];
            Task<JArray> Activities = GetActivities(lon, lat);
            JArray activitiesObject = Activities.Result;
            ViewBag.activitiesObject = activitiesObject.Where(activity => (activity["name"] != null));

            WeatherStation closest_station = _repo.GetNearestWeatherStation(lat, lon);
            IEnumerable<MonthlyData> climateData = ReadCsvData(closest_station.station_id);
            ViewBag.climateData = climateData;

            return View();
        }
        [HttpPost]
        [Route("/home/navigate")]

        public IActionResult DisplayNavigate(SearchViewModel searchViewModel)
        {

            Task<JObject> LatLong = GetLatLong(searchViewModel.CityName);
            JObject LatlongObject = LatLong.Result;
            double lon = (double)LatlongObject["features"][0]["geometry"]["coordinates"][0];
            double lat = (double)LatlongObject["features"][0]["geometry"]["coordinates"][1];
            Task<JArray> Directions = GetNavigation(lon, lat);
            JArray directionsObject = Directions.Result;
            ViewBag.lon = lon;
            ViewBag.lat = lat;

           UserProfile userProfile = _repository.GetUserProfileByUserName(User.Identity.Name.ToString());
            if (userProfile == null || userProfile.Address == null)
            {
                //MessageBox.Show("You need a profile AND a valid home address to access navigation.");
            } else
            {
                ViewBag.Address = userProfile.Address;
                ViewBag.Name = userProfile.Name;
            }
            // Code for getting the address from the database goes here.
            
       
            string homeAddress = ViewBag.Address;
            
            Task<JObject> homeAddressLatLong = GetLatLong(homeAddress);
            JObject homeAddressLatlongObject = homeAddressLatLong.Result;
            double homeAddresslon = (double)homeAddressLatlongObject["features"][0]["geometry"]["coordinates"][0];
            double homeAddresslat = (double)homeAddressLatlongObject["features"][0]["geometry"]["coordinates"][1];
            
            
           
            ViewBag.homeAddresslon = homeAddresslon;
            ViewBag.homeAddresslat = homeAddresslat;
            ViewBag.directionsObject = directionsObject;


            return View();
        }
        [HttpGet]
        [Route("/home/details")]
        public IActionResult Details(string activity)
        {
            foreach (var activityDetail in data)
            {
                if (activity == (string)activityDetail["name"])
                {
                    ViewBag.activityDetails = activityDetail;
                    return View();
                }
            }
            return View();
        }

        [Authorize]
        [HttpGet]
        [Route("/home/searchtravellers")]
        public IActionResult SearchTravellers()
        {
            SearchTravellerViewModel SearchTravellerViewModel = new();
            SearchTravellerViewModel.StartDate = DateTime.Now;
            SearchTravellerViewModel.CurrentLocation = _repository.GetUserProfileByUserName(User.Identity.Name.ToString()).AirLineCode;
            return View(SearchTravellerViewModel);
        }

        [HttpPost]
        [Route("/home/searchtravellers")]
        public IActionResult DisplayResultsForTraveller(SearchTravellerViewModel searchTravellerViewModel)
        {
            ViewBag.flightResults = GetFlightDetails(searchTravellerViewModel);

            return View();
        }

        private JArray GetFlightDetails(SearchTravellerViewModel searchTravellerViewModel)
        {
            string destinationAirLineCode = GetAirPortDetails(searchTravellerViewModel.DesiredDestination).Result;
            Task<JToken> flightResults = GetFlightDetails(searchTravellerViewModel.CurrentLocation,
                                        destinationAirLineCode,
                                        new DateTime(searchTravellerViewModel.StartDate.Year, searchTravellerViewModel.StartDate.Month, searchTravellerViewModel.StartDate.Day),
                                        searchTravellerViewModel.NoOfTravellers);
            JArray result = JArray.Parse(flightResults.Result.ToString());
            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}