using CsvHelper.Configuration;
using CsvHelper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO; 

class Program
{
    private static readonly string[] SourceUrls = new string[]
    {
        "https://randomuser.me/api/",
        "https://jsonplaceholder.typicode.com/users",
        "https://dummyjson.com/users",
        "https://reqres.in/api/users"
    };

    static async Task Main()
    {
        // Prompt the user for input
        Console.WriteLine("Please enter the path to the folder:");
        string folderPath = Console.ReadLine();

        Console.WriteLine("Please enter the file format (JSON or CSV):");
        string chosenFormat = Console.ReadLine().ToLower();

        // Check if the chosen format is valid
        if (chosenFormat == "json" || chosenFormat == "csv")
        {
            var users = new List<User>();
            var httpClient = new HttpClient();
            var tasks = new List<Task<JArray>>();

            // Fetch data asynchronously from the specified URLs
            foreach (var sourceUrl in SourceUrls)
            {
                tasks.Add(GetDataFromUrlAsync(httpClient, sourceUrl));
            }

            // Wait for all data fetching tasks to complete
            await Task.WhenAll(tasks);

            // Process and aggregate user data from fetched results
            foreach (var result in tasks.Select(t => t.Result))
            {
                foreach (JObject item in result)
                {
                    users.Add(ProcessUserObject(item));
                }
            }

            // Determine the file path based on user input
            string filePath = Path.Combine(folderPath, $"users.{chosenFormat}");
            using (var writer = new StreamWriter(filePath))
            {
                if (chosenFormat == "json")
                {
                    // Write user data as JSON to the file
                    var json = JArray.FromObject(users);
                    await writer.WriteAsync(json.ToString());
                }
                else if (chosenFormat == "csv")
                {
                    // Write user data as CSV to the file
                    var csvConfig = new CsvConfiguration(new System.Globalization.CultureInfo("en-US"));
                    using (var csv = new CsvWriter(writer, csvConfig))
                    {
                        await csv.WriteRecordsAsync(users);
                    }
                }
            }

            // Provide user with success message and total user count
            Console.WriteLine($"Data written successfully to {filePath}");
            Console.WriteLine($"Total number of users: {users.Count}");
        }
        else
        {
            // Inform the user about an invalid format choice
            Console.WriteLine("Invalid format choice.");
        }
    }

    // Fetch data asynchronously from a URL and return as a JArray
    static async Task<JArray> GetDataFromUrlAsync(HttpClient httpClient, string url)
    {
        var jsonData = await httpClient.GetStringAsync(url);
        var data = JToken.Parse(jsonData);

        if (data is JArray jsonArray)
        {
            return jsonArray;
        }
        else if (data is JObject jsonObject)
        {
            return new JArray { jsonObject };
        }

        return new JArray();
    }

    // Extract relevant properties from a user object and create a User instance
    static User ProcessUserObject(JObject item)
    {
        var user = new User
        {
            FirstName = GetPropertyValue(item, new[] { "first_name", "firstName", "name" }),
            LastName = GetPropertyValue(item, new[] { "last_name", "lastName" }),
            Email = GetPropertyValue(item, new[] { "email" }),
            SourceId = GetPropertyValue(item, new[] { "id" })
        };
        return user;
    }

    // Retrieve property value from a JObject based on multiple possible property names
    static string GetPropertyValue(JObject obj, string[] possiblePropertyNames)
    {
        foreach (var propName in possiblePropertyNames)
        {
            if (obj.TryGetValue(propName, StringComparison.OrdinalIgnoreCase, out var value))
            {
                return value.ToString();
            }
        }
        return "NULL";
    }
}

// User class to hold user data
class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string SourceId { get; set; }
}
