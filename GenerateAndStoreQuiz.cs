using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;

internal partial class Program
{
    public static async Task<string?> GenerateAndStoreQuiz(string? apiKey, string? apiURL, string? prompt, string? model, int max_tokens, double temperature, string? connectionString)
    {
        // Variable to store the generated quiz content
        string? getQuizContent = string.Empty; 

        // Variable to store the response status code
        string? responseStatusCode = string.Empty; 

        // Create an AuthenticationHeaderValue with the API key
        var headers = new AuthenticationHeaderValue("Bearer", apiKey); 

        // Data object to hold prompt, model, max_tokens, and temperature
        var data = new
        {
            prompt,
            model,
            max_tokens,
            temperature
        };
        
        // Serialize the data object to JSON
        string json = JsonConvert.SerializeObject(data); 
        
        // Create a new HttpClient
        using (var client = new HttpClient()) 
        {
            // Set the Authorization header
            client.DefaultRequestHeaders.Authorization = headers; 

            // Send a POST request to the OpenAI API with the JSON data
            var response = await client.PostAsync(apiURL, new StringContent(json, Encoding.UTF8, "application/json"));

            // Get the response status code
            responseStatusCode = response.StatusCode.ToString(); 

            if (response.IsSuccessStatusCode) // If the response is successful
            {
                // Read the response content as string
                string responseContent =  CheckAndFixJson(await response.Content.ReadAsStringAsync()); 

                // Try to deserialise the response content
                try 
                {
                    // Deserialize the response content as dynamic object, or use a new ExpandoObject if null
                    dynamic result = JsonConvert.DeserializeObject(responseContent) ?? new System.Dynamic.ExpandoObject();

                    // Check generated quiz is present in result and get the generated quiz content if it does.
                    if (result.choices != null && result.choices.Count > 0 && result.choices[0].text != null) 
                    {
                        getQuizContent = result.choices[0].text; 
                    }

                }
                // Handle the exception during deserialisation
                catch (Exception ex) 
                {
                    ErrorHandler($"\nERROR: An error occurred during deserialization from OpenAI: {ex.Message}");
                }
            }
            // Response statuscode indicates an error. Display the statuscode
            else 
            {
                return ErrorHandler($"\nERROR: There was a problem communicating with the API. Status code: {responseStatusCode}");
            }
        }

        // Parse the JSON output and add it to Postgres
        AddQuiz(getQuizContent, prompt, connectionString);

        //Mark the dupliacte entreis as true
        //MarkDuplicate(connectionString);

        //return the Quiz Content
        return getQuizContent;
    }
}