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

        [DisableConcurrentExecution(timeoutInSeconds: 6000)]
        public async Task SyncFintechMembersWithLocalDataStore()
        
        {
            int take = 5000;
            long fidxno;
            bool fetchMore = true;

            // Get the last member sorted by Fidxno 
            Member? member = await this._memberRepository.GetLastMemberByFidxno();
            fidxno = member is not null ? member.Fidxno : 0;
           // if (member is not null)
           // {
                // Update the fidxno 
                //fidxno = member.Fidxno;

                while (fetchMore)
                {

                    List<Datum> fintechMemberData = new List<Datum>();
                    List<Member> mappedMembers = new List<Member>();

                    // Get Fintech members asynchronously
                    fintechMemberData = await GetFintechMembersAsync(take, fidxno);

                    // If no data fetched, exit loop
                    if (fintechMemberData == null  || fintechMemberData?.Count == 0)
                    {
                        fetchMore = false;
                        break;
                    }

                    // Map Datum to member
                    mappedMembers = MemberMapper.MapToMembers(fintechMemberData.OrderBy(fm => fm.FIdxno).ToList());

                    //reoder in asscending order 

                    mappedMembers = mappedMembers.OrderBy(mm => mm.Fidxno).ToList();

                    // Check for duplicates
                    List<Member> duplicates = await FindDuplicatesAsync(mappedMembers);

                    // If duplicates found, stop fetching more records
                    if (duplicates.Count > 0)
                    {

                        //remove the duplicates 
                        mappedMembers = mappedMembers.Except(duplicates).ToList();

                        fetchMore = false;
                        
                    }

                    if(mappedMembers.Count > 0)
                    {
                         // Insert records to local data store
                        await this._memberRepository.AddRangeAsync(mappedMembers);

                         if(fintechMemberData.Count > 0)
                        {
                            // Original line of code
                            fidxno = fintechMemberData.OrderByDescending(fm => fm.FIdxno).FirstOrDefault()?.FIdxno ?? 0;

                            // defaultValue is the default value you want to assign if fintechMemberData is empty or if all items have a null FIdxno

                        }


                        // Save changes to the database
                        try
                        {
                            await _unitOfWork.SaveToDataStore();
                        }
                        catch (Exception ex)
                        {
                        
                            // Save the error to the database
                            await this._errorService.LogErrorAsync(ex);
                        }
                        
                    }

                   
                }
            //}
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


        public async Task<List<Datum>> GetFintechMembersAsync(int take, long Fidxno)
        {
            try
            {

                
                // Authenticate API
                string token = await ApiAuthenticate();
                string fidxno = string.Empty;

                if (string.IsNullOrEmpty(token))
                    return new List<Datum>(); // Return empty list if authentication fails

                if(Fidxno < 1)
                {
                    fidxno = "";
                }
                else
                {
                    fidxno = Fidxno.ToString();
                }
                // Set up payload
                var jsonPayload = new { take = take, Fidxno = fidxno };

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
