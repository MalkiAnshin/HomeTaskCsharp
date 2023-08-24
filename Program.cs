using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;
using CsvHelper;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var httpClient = new HttpClient();
        List<string> apiResponses = new List<string>();
        List<User> users = new List<User>();

        var tasks = new List<Task>
        {
            CollectUsersFromApi(httpClient, "https://randomuser.me/api/", users, apiResponses),
            CollectUsersFromApi(httpClient, "https://jsonplaceholder.typicode.com/users", users, apiResponses),
            CollectUsersFromApi(httpClient, "https://dummyjson.com/users", users, apiResponses),
            CollectUsersFromApi(httpClient, "https://reqres.in/api/users", users, apiResponses)
        };

        await Task.WhenAll(tasks);

        Console.WriteLine("All requests completed.");
        Console.WriteLine();

        static void PrintUsers(List<User> users)
        {
            foreach (var user in users)
            {
                Console.WriteLine($"First Name: {user.FirstName}");
                Console.WriteLine($"Last Name: {user.LastName}");
                Console.WriteLine($"Email: {user.Email}");
                Console.WriteLine($"Source ID: {user.SourceId}");
                Console.WriteLine();
            }
        }

        int totalUsers = users.Count;
        Console.WriteLine($"Total number of users: {totalUsers}");

        string outputFormat = GetOutputFormat();
        string outputDirectory = GetOutputDirectory();

        await SaveUsersToFileAsync(users, outputFormat, outputDirectory);
    }

    static async Task CollectUsersFromApi(HttpClient httpClient, string apiUrl, List<User> users, List<string> apiResponses)
    {
        try
        {
            string responseText = await httpClient.GetStringAsync(apiUrl);
            apiResponses.Add(responseText);

            var userResponse = JsonSerializer.Deserialize<UserResponse>(responseText);
            if (userResponse != null)
            {
                AddUsersFromApiResults(users, userResponse.Results, apiUrl);
            }
            else
            {
                Console.WriteLine($"Invalid response format from {apiUrl}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data from {apiUrl}: {ex.Message}");
        }
    }

    static void AddUsersFromApiResults(List<User> users, UserResult[] apiResults, string sourceId)
    {
        try
        {
            foreach (var user in apiResults)
            {
                if (user != null && user.Name != null && user.Email != null)
                {
                    Console.WriteLine($"User Information from {sourceId}:");
                    Console.WriteLine($"Name: {user.Name?.First} {user.Name?.Last}");
                    Console.WriteLine($"Email: {user.Email}");
                    Console.WriteLine();

                    bool isValidName = !string.IsNullOrWhiteSpace(user.Name.First) && !string.IsNullOrWhiteSpace(user.Name.Last);

                    if (isValidName)
                    {
                        users.Add(CreateUserFromApiResult(user.Name, user.Email, sourceId));
                    }
                    else
                    {
                        Console.WriteLine($"Skipping user with incomplete name from {sourceId}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping null or incomplete user data from {sourceId}");
                }

                Console.WriteLine();
            }

            Console.WriteLine($"Users added from {sourceId}: {apiResults.Length}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding users from {sourceId}: {ex}");
        }
    }
    static User CreateUserFromApiResult(UserName name, string email, string sourceId)
    {
        var user = new User
        {
            FirstName = GetUserField(name, "first_name", "firstName", "name"),
            LastName = GetUserField(name, "last_name", "lastName"),
            Email = email,
            SourceId = sourceId
        };
        return user;
    }

    static string GetUserField(UserName name, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            var propertyInfo = typeof(UserName).GetProperty(fieldName);
            var value = propertyInfo?.GetValue(name) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    static string GetOutputFormat()
    {
        Console.WriteLine("Enter the output format (JSON or CSV):");
        string format = Console.ReadLine();
        if (format.Equals("JSON", StringComparison.OrdinalIgnoreCase) || format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
        {
            return format;
        }
        else
        {
            Console.WriteLine("Invalid output format. Defaulting to JSON.");
            return "JSON";
        }
    }

    static string GetOutputDirectory()
    {
        Console.WriteLine("Enter the output directory:");
        return Console.ReadLine();
    }

    static async Task SaveUsersToFileAsync(List<User> users, string outputFormat, string outputDirectory)
    {
        string outputFilePath = Path.Combine(outputDirectory, $"users.{outputFormat.ToLower()}");

        using (var writer = new StreamWriter(outputFilePath))
        {
            if (outputFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                await writer.WriteAsync(json);
            }
            else if (outputFormat.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(users);
                }
            }
            else
            {
                Console.WriteLine("Invalid output format.");
                return;
            }
            await writer.FlushAsync();
        }
        int totalUsers = users.Count;

        Console.WriteLine($"Total number of users: {totalUsers}");

        Console.WriteLine($"Data saved to {outputFilePath} ({outputFormat} format)");
    }

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string SourceId { get; set; }
    }

    public class UserResponse
    {
        public UserResult[] Results { get; set; }
    }

    public class UserResult
    {
        public UserName Name { get; set; }
        public string Email { get; set; }
    }

    public class UserName
    {
        public string First { get; set; }
        public string Last { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string name { get; set; }
    }
}