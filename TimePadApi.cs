
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot.Types;

public class TimePadApi
{
    private readonly string _apiKey;
    private readonly HttpClient _client;

    public TimePadApi(string apiKey)
    {
        _apiKey = apiKey;
        _client = new HttpClient();
    }

    
    public async Task<Event> GetEventAsync(int eventId)
    {
        var apiUrl = $"https://api.timepad.ru/v1/events/{eventId}";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        var response = await _client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var eventDetails = JsonSerializer.Deserialize<Event>(json, options);

        return eventDetails;
    }

    public async Task<List<Event>> SearchEventsAsync(string query = "", string city = "Москва", int? category_ids = null, DateTime? dateFrom = null, DateTime? dateTo = null, double? minPrice = null, double? maxPrice = null)
    {
        query = query ?? string.Empty;
        var apiUrl = $"https://api.timepad.ru/v1/events?q={Uri.EscapeDataString(query)}";

        if (!string.IsNullOrEmpty(city))
        {
            apiUrl += $"&cities={Uri.EscapeDataString(city)}";
        }

        if (category_ids.HasValue)
        {
            apiUrl += $"&category_ids[]={category_ids.Value}";
        }

        
        if (!dateFrom.HasValue)
        {
            dateFrom = DateTime.Now;
            apiUrl += $"&starts_at_min={dateFrom.Value.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }
        else
        {
            apiUrl += $"&starts_at_min={dateFrom.Value.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }

        if (!dateTo.HasValue)
        {
            dateTo = DateTime.Now.AddMonths(1);
            apiUrl += $"&starts_at_max={dateTo.Value.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }
        else
        {
            apiUrl += $"&starts_at_max={dateTo.Value.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }

        if (minPrice.HasValue)
        {
            apiUrl += $"&min_price={minPrice.Value}";
        }

        if (maxPrice.HasValue)
        {
            apiUrl += $"&max_price={maxPrice.Value}";
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        var response = await _client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var timePadApiResponse = JsonSerializer.Deserialize<TimePadApiResponse>(json, options);

        var events = new List<Event>();
        foreach (var eventJson in timePadApiResponse.Values)
        {
            events.Add(new Event
            {
                Id = eventJson.Id,
                Name = eventJson.Name.Replace("&quot;","").Replace("&amp;","").Replace("quot;",""),
                Description = eventJson.Description ?? string.Empty,
                Url = eventJson.Url,
                Location = eventJson.Location,
                StartsAt = eventJson.StartsAt
            });
        }
      

        return events;
    }

    

}

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public EventLocation Location { get; set; }
    public DateTime StartsAt { get; set; }
}
public class EventLocation
{
    public string City { get; set; }
    public string Address { get; set; }
   
}
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TimePadApiResponse
{
    public List<Event> Values { get; set; }
}
