using Microsoft.Extensions.Configuration;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
internal partial class Program
{

      private static void Main(string[] args, object dotNetEnv)
    {   
        //Create new configuration object to get and read the appsettings.json file
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        //Get the IConfigurationSection for the connection string
        IConfigurationSection connectionStringSection = configuration.GetSection("ConnectionStrings:DefaultConnection");
       
        //Get the value (connection string) from the IConfigurationSection
        string? connectionString = connectionStringSection.Value;
        

        //ServicesModelBinder.configure <OrderOption>(ApiKeySection);
        IConfigurationSection ApiKeySection = configuration.GetSection("APIKeys:openAiApiKey");
        string? openAiApiKey1 = ApiKeySection.Value;

        IConfigurationSection ApiURLSection = configuration.GetSection("APIKeys:APIURL");
        string? APIURL1 = ApiURLSection.Value;


        IConfigurationSection ApiMODELSection = configuration.GetSection("APIKeys:MODEL");
        string? MODEL1 = ApiMODELSection.Value;

        IConfigurationSection ApiMAX_TOKENSSection = configuration.GetSection("APIKeys:MAX_TOKENS");
        string? MAX_TOKENS1 = ApiMAX_TOKENSSection.Value;

        IConfigurationSection ApiTEMPERATURESection = configuration.GetSection("APIKeys:TEMPERATURE");
        string? TEMPERATURE1 = ApiTEMPERATURESection.Value;

        IConfigurationSection ApiPROMPTSection = configuration.GetSection("APIKeys:PROMPT");
        string? PROMPT1 = ApiPROMPTSection.Value;



        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //Read the environment variables from azure
            DotNetEnv.Env.Load();
            string? dtp_Quiz_Key = Environment.GetEnvironmentVariable("Dtp_Quiz");
            string? openAiApiKey = Environment.GetEnvironmentVariable("openAiApiKey");
            string? APIURL = Environment.GetEnvironmentVariable("APIURL");
            string? MODEL = Environment.GetEnvironmentVariable("MODEL");
            string? MAX_TOKENS = Environment.GetEnvironmentVariable("MAX_TOKENS");
            string? TEMPERATURE = Environment.GetEnvironmentVariable("TEMPERATURE");
            string? PROMPT = Environment.GetEnvironmentVariable("PROMPT");
            var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//Add services to container
var app = builder.Build();

app.UseSwaggerUI(options =>
        {

            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });


        // if (app.Environment.IsDevelopment())
        // {
            app.UseSwagger();
            app.UseSwaggerUI();
        // }
       
        // Read the OpenAi Api Key and Database Password from environment variable that you set with dotnet user-secrets set command.
        // string openAiApiKey = builder.Configuration["OpenAI:APIKey"] ?? String.Empty;
        // string pgsqlDbPassword = builder.Configuration["PGSQL:DbPassword"] ?? String.Empty;
        
        // Dictionary<string, string>? settings = GetSettings(); // Declare a dictionary variable to load and store the settings from Settings.user file

        // // Create a Connection String for NPGSQL
        // string connectionString = $"Host={settings["HOST"]};Username={settings["USERNAME"]};Password={settings["PASSWORD"]};Database={settings["DATABASE"]}"; 

        // var app = builder.Build();

        // // Display the README.txt eg http://localhost:5000/
        // app.MapGet("/", () =>
        // {
        //     string readmeContents = File.ReadAllText("README.txt");
        //     return readmeContents;
        // });  
     
        app.MapGet("/users", () => GetUsers(connectionString)); // Display all of the users in the table in JSON format eg: http://localhost:5000/users

        // Display  the OpenAI generated Quiz as JSON but does not store it anywhere. Refreshing the page will re-generate new quiz.
        // app.MapGet("/generatequiz", () =>
        // {
        //     return GenerateQuiz(
        // openAiApiKey, 
        // settings["APIURL"], 
        // settings["PROMPT"], 
        // settings["MODEL"]);
        // });

        // Add the OpenAI generated Quiz to database and return it as JSON as well
        app.MapGet("/generateandstorequiz", () =>
        {

        if (int.TryParse(MAX_TOKENS1, out int maxTokensValue) && double.TryParse(TEMPERATURE1, out double temperatureValue))
            {
                var quizResult = GenerateAndStoreQuiz(
                    apiKey: openAiApiKey,
                    apiURL: APIURL1,
                    prompt: PROMPT1,
                    model: MODEL1,
                    max_tokens: maxTokensValue, // Convert MAX_TOKENS to int
                    temperature: temperatureValue, // Convert TEMPERATURE to double

                    connectionString: connectionString
                );
                //return the result int he response
                return Results.Ok(quizResult.Result);
            }
        else
            {
                return Results.BadRequest("Invalid input parameters");
            }
        });

        //  Display specific user info. Using route parameters in the URL eg: http://localhost:5000/user/tateclinton
        app.MapGet("/user/{login_id}", (string login_id) => GetUserByLoginName(login_id, connectionString));

        // Display specific user info. Using query parameters in the URL eg: http://localhost:5000/user?login_id=
        app.MapGet("/user", (string login_id) => GetUserByLoginName(login_id, connectionString));

 
        //  Insert user to quiz users table. Using query parameters in the URL eg: http://localhost:5000/adduser?loginid=&firstname=&lastname=
        app.MapPost("/adduser", async (context) =>
        {
            // Get the value of the "loginid" parameter from the request query string.
            string? LoginId = context.Request.Query["login_id"];

            // Get the value of the "firstname" parameter from the request query string.
            string? FirstName = context.Request.Query["first_name"];

            // Get the value of the "lastname" parameter from the request query string.
            string? LastName = context.Request.Query["last_name"];

            // Get the value of the "password" parameter from the request query string.
            string? Password = context.Request.Query["password_hash"];

            // Check if any of the required parameters (loginid, firstname, lastname) are missing or empty.
            if (string.IsNullOrEmpty(LoginId) || string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName)|| string.IsNullOrEmpty(Password))
            {
                // Set the HTTP response status code to 400 (Bad Request).
                context.Response.StatusCode = 400;

                // Write an error message to the response.
                await context.Response.WriteAsync("One or more parameters are missing.");

                // Exit the function early.
                return;
            }

            // Call the "AddUser" method, passing the "loginid", "firstname", "lastname", and "connectionString" parameters.
            // Await the method's asynchronous execution and assign the returned value to "result".
            string? result = await AddUser(LoginId, FirstName, LastName, Password,connectionString);

            // Write the value of "result" to the response, or an empty string if "result" is null.
            await context.Response.WriteAsync(result ?? string.Empty);
        });

        //Return all questions, options and user's answers for completed questions
        app.MapGet("/getalloldquiz", (string login_id) => GetAllOldQuiz(login_id, connectionString));

        // Return json of all questions and options eg: http://localhost:5000/getquiz
        app.MapGet("/getquiz", () => GetQuiz(connectionString));

        // Return json of unattempted questions and options eg: http://localhost:5000/getnewquiz?loginname=
        app.MapGet("/getnewquiz", (string login_id) => GetNewQuiz(login_id, connectionString));

        // Return json of attempted questions and options eg: http://localhost:5000/getnewquiz?loginname=
        app.MapGet("/getoldquiz", (string login_id) => GetOldQuiz(login_id, connectionString));


 
        //  Display True or False. Using route parameters in the URL eg: http://localhost:5000/checkanswer/21/b
        app.MapGet("/checkanswer/{question_id}/{option_name}", (int question_id, char option_name) => CheckAnswer(question_id, Char.ToUpper(option_name), connectionString));

        // Display True or False. Using query parameters in the url eg: http://localhost:5000/checkanswer?questionid=&optionname=
        app.MapGet("/checkanswer", (int question_id, char option_name) => CheckAnswer(question_id, Char.ToUpper(option_name), connectionString));



        // Insert record to quiz history table and returns true or false. Using route parameters in the URL eg: http://localhost:5000/recordanswer/anhnguyen/11/d
        // The route spells out as follows: user / the question ID / the answer given for that question ID
        app.MapPost("/recordanswer/{login_id}/{question_id}/{option_name}", async (context) =>
        {
            string? login_id = context.Request.RouteValues["login_id"] as string;

            //int questionid = int.Parse(context.Request.RouteValues["question_id"] as string);
            string? questionIdString = context.Request.RouteValues["question_id"] as string;
            int question_id = 0;
            int.TryParse(questionIdString, out question_id);
            
            //char optionname = char.Parse(context.Request.RouteValues["option_name"] as string);
            string? optionNameString = context.Request.RouteValues["option_name"] as string;
            char option_name = !string.IsNullOrEmpty(optionNameString) ? optionNameString[0] : '\0';            

            //string? result = RecordAnswer(loginname, questionid, char.ToUpper(optionname), connectionString);
            string? result = null;
            if (login_id != null)
            {
                result = await RecordAnswer(login_id, question_id, char.ToUpper(option_name), connectionString);
            }
            
            //await context.Response.WriteAsync(result);
            await context.Response.WriteAsync(result ?? string.Empty);
        });


        //  Insert record to quiz history table and returns true or false. Using query parameters in the URL eg: http://localhost:5000/recordanswer?loginname=&questionid=&optionname=
        app.MapPost("/recordanswer", async (context) =>
        {
            string? login_id = context.Request.Query["login_id"];
            //int questionid = int.Parse(context.Request.Query["questionid"]);
            int question_id;
            if (!int.TryParse(context.Request.Query["questionid"], out question_id))
            {
                question_id = 0; // Assign a default value of 0 in case no question ID was supplied.
            }

            string? option_name = context.Request.Query["optionname"].FirstOrDefault();

            string? result = null;
            if (!string.IsNullOrEmpty(login_id))
            {
                char optionChar = !string.IsNullOrEmpty(option_name) ? char.ToUpper(option_name[0]) : '\0';
                result = await RecordAnswer(login_id, question_id, optionChar, connectionString);
            }

            await context.Response.WriteAsync(result ?? string.Empty);
        });




        //  Update user to quiz users table. Using query parameters in the URL eg: http://localhost:5000/updateuser?login_id=&first_name=&last_name=&password_hash=
            app.MapPut("/updateuser", async (context) =>
            {
            // Get the value of the "loginid" parameter from the request query string.
            string? LoginId = context.Request.Query["login_id"];

            // Get the value of the "firstname" parameter from the request query string.
            string? FirstName = context.Request.Query["first_name"];

            // Get the value of the "lastname" parameter from the request query string.
            string? LastName = context.Request.Query["last_name"];

            // Get the value of the "lastname" parameter from the request query string.
            string? Password = context.Request.Query["password_hash"];

            // Check if any of the required parameters (loginid, firstname, lastname) are missing or empty.
            if (string.IsNullOrEmpty(LoginId)||string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName) || string.IsNullOrEmpty(Password))
            {
                // Set the HTTP response status code to 400 (Bad Request).
                context.Response.StatusCode = 400;

                // Write an error message to the response.
                await context.Response.WriteAsync("One or more parameters are missing.");

                // Exit the function early.
                return;
            }

            // Call the "UpdateUser" method, passing the "loginid", "firstname", "lastname", and "connectionString" parameters.
            // Await the method's asynchronous execution and assign the returned value to "result".
            string? result = await UpdateUser( LoginId, FirstName, LastName, Password, connectionString);

            // Write the value of "result" to the response, or an empty string if "result" is null.
            await context.Response.WriteAsync(result ?? string.Empty);
            });


            //Activate users to get access to quiz http://localhost:5000/activateUserStatus?loginid=
             app.MapPut("/activateuserstatus", (string LoginId) => ActivateUserStatus (LoginId ?? string.Empty,connectionString));

           
           //UserLogin endpoint URL eg: http://localhost:5000/userlogin?login_id=&password_hash=
           
             //app.MapPost("/userlogin", (string LoginId, string Password) => UserLogin (LoginId ?? string.Empty, Password ?? string.Empty, connectionString));

            app.MapPost("/userlogin", async(context) => 
             {
            //Get the value of the "LoginId"
            string? LoginId = context.Request.Query["login_id"];

            //Get the value of password
            string? Password = context.Request.Query["password_hash"];
            

            //check if LoginId or password is Null or empty
            if(string.IsNullOrEmpty(LoginId) || string.IsNullOrEmpty(Password))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Login ID or Password is missing");
                return;
            }

            bool LoginSuccessful = await UserLogin(LoginId, Password,  connectionString);
           

            await context.Response.WriteAsync(LoginSuccessful ? "Login Successful" : "Login Failed");
            

            
            if (!LoginSuccessful && LoginId !=null)
            {
                bool UserStatus = await GetUserStatus(LoginId, connectionString);
                if (!UserStatus)
                {
                    await context.Response.WriteAsync("\nYour Account is deactivated");
                }
            }
            });

             

        app.Run();
    }
}