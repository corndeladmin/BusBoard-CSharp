using Newtonsoft.Json;
using BusBoard.Postcodes;
using BusBoard.Tfl;

namespace BusBoard;

internal class Program
{
    async static Task BusBoard()
    {
        string postcode = "EN5 5LP";

        var tflClient = new HttpClient();
        tflClient.BaseAddress = new Uri("https://api.tfl.gov.uk/StopPoint/");

        var postcodeClient = new HttpClient();
        postcodeClient.BaseAddress = new Uri("https://api.postcodes.io/postcodes/");

        // get postcode data
        string postcodeJson = await postcodeClient.GetStringAsync(postcode);
        PostcodeResponse postcodeResponse = JsonConvert.DeserializeObject<PostcodeResponse>(postcodeJson);
        PostcodeData postcodeData = postcodeResponse.Result;
        float lat = postcodeData.Latitude;
        float lon = postcodeData.Longitude;

        // find closest stops
        var stopsJson = await tflClient.GetStringAsync($"?lat={lat}&lon={lon}&stopTypes=NaptanPublicBusCoachTram&radius=500");
        var stopsResponse = JsonConvert.DeserializeObject<StopPointsResponse>(stopsJson);
        var closestStop = stopsResponse.StopPoints.OrderBy(s => s.Distance).First();

        Console.WriteLine($"Closest stop is \"{closestStop.CommonName}\" ({closestStop.Distance}m)");

        // find next 5 buses
        var arrivalsJson = await tflClient.GetStringAsync($"{closestStop.NaptanId}/Arrivals");
        var predictions = JsonConvert.DeserializeObject<List<ArrivalPrediction>>(arrivalsJson);

        if (predictions is null)
        {
            throw new Exception("Unable to retrieve predictions from JSON response");
        }

        var firstFiveBuses = predictions.OrderBy(p => p.TimeToStation).Take(5);

        foreach (var p in firstFiveBuses)
        {
            Console.WriteLine($"{p.LineName} bus to {p.DestinationName} arriving in {p.TimeToStation} seconds");
        }
    }
    
    async static Task Main(string[] args)
    {
        await BusBoard();
    }
}