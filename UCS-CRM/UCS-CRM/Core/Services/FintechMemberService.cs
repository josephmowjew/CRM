using Newtonsoft.Json;
using System.Text.Json;
using System.Text;
using UCS_CRM.Core.DTOs.Login;
using UCS_CRM.Core.Models;
using Azure.Core;
using System.Net.Http.Headers;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Hangfire;

namespace UCS_CRM.Core.Services
{
    public class FintechMemberService : IFintechMemberService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly IErrorLogService _errorService;
        public IConfiguration _configuration { get; }

        public FintechMemberService(HttpClient httpClient, 
                                    IConfiguration configuration,
                                    IMemberRepository memberRepository,
                                    IUnitOfWork unitOfWork,
                                    IUserRepository userRepository,
                                    IErrorLogService errorLogService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _errorService = errorLogService;

        }

        public async Task<List<Datum>> GetAllFintechMembersAsync()
        {
            try
            {
                // Authenticate API
                string token = await ApiAuthenticate();

                if (string.IsNullOrEmpty(token))
                    return new List<Datum>(); // Return empty list if authentication fails

                // Set up payload
                var jsonPayload = new { take = 100, Fidxno = "" };

                // Set HTTP request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //_httpClient.Timeout = TimeSpan.FromMinutes(5); // Timeout set to 5 minutes

                // Serialize payload
                var json = JsonConvert.SerializeObject(jsonPayload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await _httpClient.PostAsync(_configuration["APIURL:link"]+ "MemberBioDataPaginated", httpContent);

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    // Read and deserialize the response content
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<FintechMember>(responseBody);

                    return responseData.Data;
                }
                else
                {
                    // Log or handle unsuccessful response
                    // For example: Log.Error($"Failed to fetch fintech members. Status code: {response.StatusCode}");
                    return new List<Datum>(); // Return empty list on failure
                }
            }
            catch (Exception ex)
            {
                // Log or handle any exceptions
                // For example: Log.Error($"An error occurred: {ex.Message}");
                return new List<Datum>(); // Return empty list on exception
            }
        }


        private async Task<List<Member>> FindDuplicatesAsync(List<Member> members)
        {
            List<Member> duplicates = new List<Member>();

            await foreach (var fintechMemberRecord in members.ToAsyncEnumerable())
            {
                var existingMember = await _memberRepository.ExistsAsync(fintechMemberRecord);
                if (existingMember != null)
                {
                    duplicates.Add(fintechMemberRecord);
                }
            }

            return duplicates;
        }
        [DisableConcurrentExecution(timeoutInSeconds: 6000)]
        public async Task<List<long>> SyncFintechMembersWithLocalDataStore()
        {
            int initialBatchSize = 500;
            int maxBatchSize = 500; // Maximum batch size to prevent too large requests
            int batchSize = initialBatchSize;
            long fidxno;
            bool fetchMore = true;
            int totalProcessed = 0;
            int batchNumber = 0;
            List<long> errorFidxnos = new List<long>(); // List to store fidxno with errors
            string accountNumberErrorMessage = "Account Number Does Not Match any Identification Details";

            // Get the last member sorted by Fidxno 
            Member? member = await this._memberRepository.GetLastMemberByFidxno();
            fidxno = member?.Fidxno ?? 0;

            while (fetchMore)
            {
                batchNumber++;
                List<Datum> fintechMemberData;
                bool apiErrorOccurred;
                string responseBody = "";
                bool batchProcessedSuccessfully = false; // Flag to check if the batch was processed successfully

                // Get Fintech members asynchronously
                (fintechMemberData, responseBody, apiErrorOccurred) = await GetFintechMembersAsync(batchSize, fidxno);

                // Handle API error
                if (apiErrorOccurred)
                {
                    if (responseBody.Contains(accountNumberErrorMessage))
                    {
                        await LogMessageAsync($"Batch {batchNumber}: Error message '{accountNumberErrorMessage}' received. Stopping fetching.");
                        fetchMore = false;
                        break;
                    }

                    string errorMessage = "Process Failed:          0 String or binary data would be truncated.";
                    if (batchSize == 1 && responseBody.Contains(errorMessage))
                    {
                        // Record fidxno with the error
                        errorFidxnos.Add(fidxno);
                        await LogMessageAsync($"Batch {batchNumber}: API error occurred with batchSize=1. Error message: {errorMessage}. Recording fidxno {fidxno}.");
                        fidxno++; // Increment fidxno to fetch the next record
                        continue;
                    }

                    // Reduce batchSize and retry if batchSize is greater than 1
                    if (batchSize > 1)
                    {
                        batchSize = Math.Max(1, batchSize / 2);
                        await LogMessageAsync($"Batch {batchNumber}: API error occurred. Reducing batchSize to {batchSize}.");
                    }
                    continue;
                }

                // If no data fetched, exit loop
                if (fintechMemberData == null || fintechMemberData.Count == 0)
                {
                    await LogMessageAsync($"Batch {batchNumber}: No more data to fetch. Exiting loop.");
                    fetchMore = false;
                    break;
                }

                // Process each record individually
                foreach (var datum in fintechMemberData)
                {
                    try
                    {
                        // Map Datum to member
                        var mappedMember = MemberMapper.MapToMember(datum);

                        // Check for duplicates
                        var duplicates = await FindDuplicatesAsync(new List<Member> { mappedMember });
                        if (duplicates.Count > 0)
                        {
                            await LogMessageAsync($"Batch {batchNumber}: Record with fidxno {datum.FIdxno} is a duplicate and was not processed.");
                            continue;
                        }

                        // Insert record to local data store
                        this._memberRepository.Add(mappedMember);
                        totalProcessed++;
                        batchProcessedSuccessfully = true; // Mark batch as successfully processed
                    }
                    catch (Exception ex)
                    {
                        // Log the error with specific record details
                        await this._errorService.LogErrorAsync(ex);
                        await LogMessageAsync($"Batch {batchNumber}: Error processing record with fidxno {datum.FIdxno} - {ex.Message}");
                        // Continue processing remaining records even if one fails
                    }
                }

                // Save changes to the database
                try
                {
                    int savedCount = await _unitOfWork.SaveToDataStoreSync();
                    await LogMessageAsync($"Batch {batchNumber}: Processed {totalProcessed} members. Saved {savedCount} to database.");
                }
                catch (Exception ex)
                {
                    // Save the error to the database
                    await this._errorService.LogErrorAsync(ex);
                    await LogMessageAsync($"Batch {batchNumber}: Error saving to database - {ex.Message}");
                }

                // Update fidxno to the latest one fetched
                if (fintechMemberData.Count > 0)
                {
                    fidxno = fintechMemberData.Max(fm => fm.FIdxno);
                    await LogMessageAsync($"Batch {batchNumber}: Updated fidxno to {fidxno}");
                }

                // Increase batch size if batch processed successfully and it's below the maximum size
                if (batchProcessedSuccessfully && batchSize < maxBatchSize)
                {
                    batchSize = Math.Min(maxBatchSize, batchSize * 2);
                    await LogMessageAsync($"Batch {batchNumber}: Increasing batchSize to {batchSize}");
                }

                
                // Add a small delay to prevent overwhelming the database
                await Task.Delay(5000);
            }

            // Log the list of fidxnos with errors to a file
            await LogErrorFidxnosToFileAsync(errorFidxnos);

            await LogMessageAsync($"Sync completed. Total records processed: {totalProcessed}");
            return errorFidxnos; // Return the list of fidxnos with errors
        }

        private async Task LogMessageAsync(string message)
        {
            // Implement this method to log messages to a file or database
            // This will help in debugging and tracking the process
            Console.WriteLine($"{DateTime.Now}: {message}");
            // You might want to save this to a log file or database as well
        }

        public async Task<(List<Datum>, string, bool)> GetFintechMembersAsync(int take, long Fidxno)
        {
            try
            {
                // Authenticate API
                string token = await ApiAuthenticate();
                string fidxno = Fidxno < 1 ? "" : Fidxno.ToString();

                if (string.IsNullOrEmpty(token))
                    return (new List<Datum>(), "", false);

                // Set up payload
                var jsonPayload = new { take = take, Fidxno = fidxno };

                // Set HTTP request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Serialize payload
                var json = JsonConvert.SerializeObject(jsonPayload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await _httpClient.PostAsync(_configuration["APIURL:link"] + "MemberBioDataPaginated", httpContent);

                // Read the response content
                string responseBody = await response.Content.ReadAsStringAsync();

                // Check for the specific error response
                if (responseBody.Contains("Process Failed:          0 String or binary data would be truncated."))
                {
                    return (new List<Datum>(), responseBody, true);
                }

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<FintechMember>(responseBody);
                    return (responseData.Data, responseBody, false);
                }
                else
                {
                    // Return the response body with an error status
                    return (new List<Datum>(), responseBody, false);
                }
            }
            catch (Exception ex)
            {
                // Log or handle any exceptions
                return (new List<Datum>(), ex.Message, false);
            }
        }

        private async Task LogErrorFidxnosToFileAsync(List<long> errorFidxnos)
        {
            // Get the directory of the application's executable
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "errorFidxnos.txt");

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the list of fidxnos to the file
                using (StreamWriter writer = new StreamWriter(filePath, true)) // Append mode
                {
                    foreach (var fidxno in errorFidxnos)
                    {
                        await writer.WriteLineAsync(fidxno.ToString());
                    }
                }

                await LogMessageAsync("Error fidxnos logged to file successfully.");
            }
            catch (Exception ex)
            {
                // Log the error to console or another logging mechanism
                await LogMessageAsync($"Error logging fidxnos to file: {ex.Message}");
            }
        }



        public async Task<string> ApiAuthenticate()
        {

            // APIToken token = new APIToken();

            var username = _configuration["APICredentials:Username"];
            var password = _configuration["APICredentials:Password"];

            APILogin apiLogin = new APILogin()
            {
                Username = username,
                Password = password,
            };



            var jsonContent = JsonConvert.SerializeObject(apiLogin);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send POST request        
            var tokenResponse = await _httpClient.PostAsync(_configuration["APIURL:link"] + $"Token", stringContent);

            var json = await tokenResponse.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var status = document.RootElement.GetProperty("status").GetInt32();

            if (status == 404)
            {

                //
                Console.WriteLine("Failed to login");
                return "Failed to login";
            }

            var token = document.RootElement.GetProperty("token").GetString();

            return token;
        }

        public async Task<KeyValuePair<bool, string>> CreateAllMemberUserAccounts()
        {

            string UserId = string.Empty;
            //get the system user

            ApplicationUser? systemUser = (await this._userRepository.GetUsersInRole("system"))?.FirstOrDefault();

            if (systemUser != null)
            {
                UserId = systemUser.Id;

                //loop through member records which have no associated user accounts 

                List<Member> members = await this._memberRepository.GetMembersWithNoUserAccount();

                if(members.Count > 0)
                {
                   await foreach(var member in members.ToAsyncEnumerable())
                    {
                        //add create a user account for the member



                    }
                }
            }
            else
            {
                return new KeyValuePair<bool, string>(true, "system user not found");
            }

            return new KeyValuePair<bool, string> ( true, "users created successfully");
        }
    }
}
