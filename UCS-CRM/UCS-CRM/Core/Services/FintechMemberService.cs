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
using System.Collections.Concurrent;
using System.Linq;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly SemaphoreSlim _dbContextSemaphore = new SemaphoreSlim(1, 1); // Add this as class field

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

        [DisableConcurrentExecution(timeoutInSeconds: 6000)]
        public async Task<List<long>> SyncMissingFintechMembers(CancellationToken cancellationToken = default)
        {
            const int INITIAL_BATCH_SIZE = 500;
            const int MIN_BATCH_SIZE = 1;
            const int MAX_BATCH_SIZE = 500;
            const int CHUNK_SIZE = 100;
            const int DELAY_MS = 1000;
            const int MAX_RETRY_ATTEMPTS = 3;
            const int PARALLEL_CHUNKS = 2;
            const long MEMORY_THRESHOLD = 500L * 1024L * 1024L; // 500MB
            
            var metrics = new ConcurrentDictionary<string, long>();
            var processedFidxnos = new ConcurrentDictionary<long, byte>();
            var errorFidxnos = new ConcurrentBag<long>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var circuitBreaker = new CircuitBreaker(failureThreshold: 5, recoveryTime: TimeSpan.FromMinutes(1));
            var semaphore = new SemaphoreSlim(PARALLEL_CHUNKS);
            
            long fidxno = 0;
            int batchSize = INITIAL_BATCH_SIZE;
            int totalProcessed = 0;
            int batchNumber = 0;
            DateTime startTime = DateTime.UtcNow;
            bool hasMoreRecords = true;

            try
            {
                while (!cancellationToken.IsCancellationRequested && hasMoreRecords)
                {
                    batchNumber++;
                    
                    if (IsHighMemoryPressure(MEMORY_THRESHOLD))
                    {
                        batchSize = Math.Max(MIN_BATCH_SIZE, batchSize / 2);
                        await LogMessageAsync($"High memory pressure detected. Reducing batch size to {batchSize}").ConfigureAwait(false);
                        GC.Collect(2, GCCollectionMode.Optimized, true);
                        await Task.Delay(DELAY_MS * 2, cancellationToken).ConfigureAwait(false);
                    }

                    try 
                    {
                        var (fintechMemberData, _, apiErrorOccurred) = await RetryWithExponentialBackoff(
                            () => GetFintechMembersAsync(batchSize, fidxno),
                            MAX_RETRY_ATTEMPTS,
                            cancellationToken
                        ).ConfigureAwait(false);

                        if (apiErrorOccurred)
                        {
                            await ProcessFailedBatchAsync(
                                fidxno,
                                batchSize,
                                CHUNK_SIZE,
                                processedFidxnos,
                                errorFidxnos,
                                metrics,
                                semaphore,
                                cancellationToken
                            ).ConfigureAwait(false);
                                
                            fidxno += batchSize;
                            continue;
                        }

                        if (fintechMemberData?.Any() != true)
                        {
                            hasMoreRecords = false;
                            continue;
                        }

                        await ProcessSuccessfulBatchAsync(
                            fintechMemberData,
                            CHUNK_SIZE,
                            processedFidxnos,
                            errorFidxnos,
                            metrics,
                            semaphore,
                            batchNumber,
                            cancellationToken
                        ).ConfigureAwait(false);

                        totalProcessed += fintechMemberData.Count;
                        await LogMessageAsync($"Processed batch {batchNumber}, Total records: {totalProcessed}").ConfigureAwait(false);

                        fidxno = fintechMemberData.Max(fm => fm.FIdxno) + 1;
                        
                        // Adaptive batch sizing based on success
                        if (batchSize < MAX_BATCH_SIZE)
                        {
                            batchSize = Math.Min(MAX_BATCH_SIZE, (int)(batchSize * 1.5));
                        }

                        await Task.Delay(DELAY_MS, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        metrics.AddOrUpdate("errors", 1, (_, count) => count + 1);
                        await LogErrorWithContextAsync(ex, fidxno, batchNumber, metrics).ConfigureAwait(false);
                        errorFidxnos.Add(fidxno++);
                        
                        // Reduce batch size on error
                        batchSize = Math.Max(MIN_BATCH_SIZE, batchSize / 2);
                    }
                }

                // Recovery phase
                if (!errorFidxnos.IsEmpty)
                {
                    await RecoverFailedRecordsAsync(
                        errorFidxnos.ToList(),
                        CHUNK_SIZE,
                        processedFidxnos,
                        metrics,
                        semaphore,
                        cancellationToken
                    ).ConfigureAwait(false);
                }

                // Log final metrics
                await LogMetricsAsync(metrics, startTime).ConfigureAwait(false);
                return errorFidxnos.ToList();
            }
            finally
            {
                semaphore.Dispose();
                memoryCache.Dispose();
            }
        }

        private async Task ProcessFailedBatchAsync(
            long startFidxno,
            int batchSize,
            int chunkSize,
            ConcurrentDictionary<long, byte> processedFidxnos,
            ConcurrentBag<long> errorFidxnos,
            ConcurrentDictionary<string, long> metrics,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            var failedBatches = Enumerable.Range(0, batchSize)
                .Chunk(chunkSize)
                .Select(chunk => chunk.Select(i => startFidxno + i));

            foreach (var failedBatch in failedBatches)
            {
                var tasks = failedBatch.Select(async currentFidxno =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var (singleMemberData, _, singleApiError) = 
                            await GetFintechMembersAsync(1, currentFidxno - 1)
                            .ConfigureAwait(false);

                        if (!singleApiError && singleMemberData?.Any() == true)
                        {
                            var processed = await ProcessBatchAsync(
                                singleMemberData,
                                processedFidxnos,
                                errorFidxnos,
                                0,
                                cancellationToken
                            ).ConfigureAwait(false);
                            
                            metrics.AddOrUpdate("processed", processed, (_, count) => count + processed);
                        }
                        else
                        {
                            errorFidxnos.Add(currentFidxno);
                            metrics.AddOrUpdate("errors", 1, (_, count) => count + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorFidxnos.Add(currentFidxno);
                        metrics.AddOrUpdate("errors", 1, (_, count) => count + 1);
                        await LogMessageAsync($"Error processing record {currentFidxno}: {ex.Message}")
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        private async Task ProcessSuccessfulBatchAsync(
            List<Datum> fintechMemberData,
            int chunkSize,
            ConcurrentDictionary<long, byte> processedFidxnos,
            ConcurrentBag<long> errorFidxnos,
            ConcurrentDictionary<string, long> metrics,
            SemaphoreSlim semaphore,
            int batchNumber,
            CancellationToken cancellationToken)
        {
            var chunks = fintechMemberData.Chunk(chunkSize);
            var chunkTasks = chunks.Select(async chunk =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var processed = await ProcessBatchAsync(
                        chunk.ToList(),
                        processedFidxnos,
                        errorFidxnos,
                        batchNumber,
                        cancellationToken
                    ).ConfigureAwait(false);
                    
                    metrics.AddOrUpdate("processed", processed, (_, count) => count + processed);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(chunkTasks).ConfigureAwait(false);
        }

        private async Task RecoverFailedRecordsAsync(
            List<long> errorList,
            int chunkSize,
            ConcurrentDictionary<long, byte> processedFidxnos,
            ConcurrentDictionary<string, long> metrics,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            var recoveryBatches = errorList.Chunk(chunkSize);
            var remainingErrors = new ConcurrentBag<long>();
            
            foreach (var batch in recoveryBatches)
            {
                var recoveryTasks = batch.Select(async errorFidxno =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var (singleMemberData, _, apiError) = 
                            await GetFintechMembersAsync(1, errorFidxno - 1)
                            .ConfigureAwait(false);

                        if (!apiError && singleMemberData?.Any() == true)
                        {
                            var processed = await ProcessBatchAsync(
                                singleMemberData,
                                processedFidxnos,
                                remainingErrors,
                                0,
                                cancellationToken
                            ).ConfigureAwait(false);
                            
                            metrics.AddOrUpdate("recovered", processed, (_, count) => count + processed);
                            return (errorFidxno, recovered: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        metrics.AddOrUpdate("recovery_errors", 1, (_, count) => count + 1);
                        await LogMessageAsync($"Failed to recover record {errorFidxno}: {ex.Message}")
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                    return (errorFidxno, recovered: false);
                });

                var results = await Task.WhenAll(recoveryTasks).ConfigureAwait(false);
                foreach (var (fidxno, recovered) in results.Where(r => !r.recovered))
                {
                    remainingErrors.Add(fidxno);
                }
            }
            
            await LogErrorFidxnosToFileAsync(remainingErrors.ToList()).ConfigureAwait(false);
        }

        private static bool IsHighMemoryPressure(long threshold)
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            return gcMemoryInfo.HeapSizeBytes > threshold;
        }

        private async Task LogErrorWithContextAsync(
            Exception ex,
            long fidxno,
            int batchNumber,
            ConcurrentDictionary<string, long> metrics)
        {
            var context = new
            {
                Fidxno = fidxno,
                BatchNumber = batchNumber,
                Metrics = metrics,
                Timestamp = DateTime.UtcNow,
                Exception = new
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.Message
                }
            };

            await _errorService.LogErrorAsync(ex).ConfigureAwait(false);
            await LogMessageAsync($"Error Context: {JsonConvert.SerializeObject(context, Formatting.Indented)}")
                .ConfigureAwait(false);
        }

        private async Task LogMetricsAsync(
            ConcurrentDictionary<string, long> metrics,
            DateTime startTime)
        {
            var duration = DateTime.UtcNow - startTime;
            var summary = new
            {
                Duration = duration.ToString(),
                ProcessedRecords = metrics.GetValueOrDefault("processed"),
                Errors = metrics.GetValueOrDefault("errors"),
                RecoveredRecords = metrics.GetValueOrDefault("recovered"),
                RecoveryErrors = metrics.GetValueOrDefault("recovery_errors"),
                ProcessingRate = metrics.GetValueOrDefault("processed") / duration.TotalSeconds
            };

            await LogMessageAsync($"Sync Summary: {JsonConvert.SerializeObject(summary, Formatting.Indented)}")
                .ConfigureAwait(false);
        }

        public class CircuitBreaker
        {
            private readonly int _failureThreshold;
            private readonly TimeSpan _recoveryTime;
            private int _failureCount;
            private DateTime? _lastFailureTime;
            private readonly object _lock = new object();

            public CircuitBreaker(int failureThreshold, TimeSpan recoveryTime)
            {
                _failureThreshold = failureThreshold;
                _recoveryTime = recoveryTime;
            }

            public async Task<bool> ExecuteAsync(Func<Task<bool>> action)
            {
                if (IsOpen())
                    return false;

                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch
                {
                    RecordFailure();
                    throw;
                }
            }

            private bool IsOpen()
            {
                lock (_lock)
                {
                    if (_failureCount >= _failureThreshold && 
                        _lastFailureTime.HasValue && 
                        DateTime.Now - _lastFailureTime.Value < _recoveryTime)
                        return true;

                    if (_lastFailureTime.HasValue && 
                        DateTime.Now - _lastFailureTime.Value >= _recoveryTime)
                    {
                        _failureCount = 0;
                        _lastFailureTime = null;
                    }

                    return false;
                }
            }

            private void RecordFailure()
            {
                lock (_lock)
                {
                    _failureCount++;
                    _lastFailureTime = DateTime.UtcNow;
                }
            }
        }

        private async Task<int> ProcessBatchAsync(
            List<Datum> batch,
            ConcurrentDictionary<long, byte> processedFidxnos,
            ConcurrentBag<long> errorFidxnos,
            int batchNumber,
            CancellationToken cancellationToken)
        {
            var membersToAdd = new List<Member>(batch.Count);
            var processedCount = 0;

            foreach (var datum in batch)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!processedFidxnos.TryAdd(datum.FIdxno, 1))
                    continue;

                try
                {
                    var mappedMember = MemberMapper.MapToMember(datum);
                    
                    // Synchronize DbContext access
                    await _dbContextSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (await _memberRepository.ExistsAsync(mappedMember).ConfigureAwait(false) == null)
                        {
                            membersToAdd.Add(mappedMember);
                            processedCount++;
                        }
                    }
                    finally
                    {
                        _dbContextSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    errorFidxnos.Add(datum.FIdxno);
                    processedFidxnos.TryRemove(datum.FIdxno, out _);
                    
                    var errorDetails = new StringBuilder()
                        .AppendLine($"Failed to process Fidxno {datum.FIdxno}")
                        .AppendLine($"Error: {ex.Message}")
                        .AppendLine($"Raw Data: {JsonConvert.SerializeObject(datum, Formatting.Indented)}")
                        .AppendLine($"Stack Trace: {ex.StackTrace}")
                        .ToString();
                    
                    await LogMessageAsync(errorDetails).ConfigureAwait(false);
                }
            }

            if (membersToAdd.Any())
            {
                try
                {
                    // Synchronize DbContext access for batch save
                    await _dbContextSemaphore.WaitAsync(cancellationToken);
                    try 
                    {
                        await _memberRepository.AddRangeAsync(membersToAdd).ConfigureAwait(false);
                        await _unitOfWork.SaveToDataStoreSync().ConfigureAwait(false);
                    }
                    finally
                    {
                        _dbContextSemaphore.Release();
                    }
                    
                    if (batchNumber > 0)
                    {
                        await LogMessageAsync($"Batch {batchNumber}: Added {processedCount} members")
                            .ConfigureAwait(false);
                    }
                    
                    return processedCount;
                }
                catch (Exception ex)
                {
                    await _errorService.LogErrorAsync(ex).ConfigureAwait(false);
                    var saveErrorDetails = $"Batch {batchNumber}: Failed to save changes - {ex.Message}\nStack Trace: {ex.StackTrace}";
                    await LogMessageAsync(saveErrorDetails).ConfigureAwait(false);
                    
                    foreach (var member in membersToAdd)
                    {
                        processedFidxnos.TryRemove(member.Fidxno, out _);
                        errorFidxnos.Add(member.Fidxno);
                    }
                    throw;
                }
            }

            return 0;
        }

        private static async Task<T> RetryWithExponentialBackoff<T>(
            Func<Task<T>> operation,
            int maxAttempts,
            CancellationToken cancellationToken,
            int initialDelayMs = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception) when (i < maxAttempts - 1)
                {
                    var delayMs = initialDelayMs * Math.Pow(2, i);
                    await Task.Delay((int)delayMs, cancellationToken).ConfigureAwait(false);
                }
            }
            return await operation().ConfigureAwait(false);
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
            if (!errorFidxnos.Any()) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logsDirectory = Path.Combine(baseDirectory, "ErrorLogs");
            var filePath = Path.Combine(logsDirectory, $"failed_records_{timestamp}.json");

            try
            {
                Directory.CreateDirectory(logsDirectory);

                var errorLog = new
                {
                    Timestamp = DateTime.Now,
                    TotalFailedRecords = errorFidxnos.Count,
                    FailedRecordIds = errorFidxnos.OrderBy(x => x),
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    ExecutionContext = new
                    {
                        ApplicationPath = AppDomain.CurrentDomain.BaseDirectory,
                        UserName = Environment.UserName,
                        OSVersion = Environment.OSVersion.ToString()
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(errorLog, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonContent);

                // Write a summary to the console
                await LogMessageAsync($"Error Summary:");
                await LogMessageAsync($"- Total Failed Records: {errorFidxnos.Count}");
                await LogMessageAsync($"- Error Log File: {filePath}");
                
                // Create an index file that lists all error logs
                var indexPath = Path.Combine(logsDirectory, "error_logs_index.json");
                var indexEntry = new
                {
                    LogFile = Path.GetFileName(filePath),
                    Timestamp = DateTime.Now,
                    RecordCount = errorFidxnos.Count
                };

                List<object> existingIndex = new();
                if (File.Exists(indexPath))
                {
                    var existingContent = await File.ReadAllTextAsync(indexPath);
                    existingIndex = JsonConvert.DeserializeObject<List<object>>(existingContent) ?? new List<object>();
                }

                existingIndex.Insert(0, indexEntry);
                // Keep only last 100 entries
                if (existingIndex.Count > 100)
                    existingIndex = existingIndex.Take(100).ToList();

                await File.WriteAllTextAsync(indexPath, JsonConvert.SerializeObject(existingIndex, Formatting.Indented));
            }
            catch (Exception ex)
            {
                await LogMessageAsync($"Error saving failed records log: {ex.Message}");
                
                // Fallback to simple file logging if JSON serialization fails
                var fallbackPath = Path.Combine(logsDirectory, $"failed_records_{timestamp}.txt");
                try
                {
                    await File.WriteAllLinesAsync(fallbackPath, errorFidxnos.Select(id => id.ToString()));
                    await LogMessageAsync($"Fallback error log saved to: {fallbackPath}");
                }
                catch (Exception fallbackEx)
                {
                    await LogMessageAsync($"Critical: Failed to save error log in any format: {fallbackEx.Message}");
                }
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

        public async Task<(bool success, string message)> AddMemberByAccountNumber(string accountNumber)
        {
            try
            {
                // Authenticate API
                string token = await ApiAuthenticate();
                if (string.IsNullOrEmpty(token))
                    return (false, "Authentication failed");

                // Set HTTP request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Send GET request
                var response = await _httpClient.GetAsync($"{_configuration["APIURL:link"]}MemberBiodataByAccountNo?account_number={accountNumber}");
                
                if (!response.IsSuccessStatusCode)
                    return (false, $"API request failed with status {response.StatusCode}");

                string responseBody = await response.Content.ReadAsStringAsync();
                var memberData = JsonConvert.DeserializeObject<FintechMember>(responseBody);

                if (memberData?.Data == null || !memberData.Data.Any())
                    return (false, "No member found with provided account number");

                var datum = memberData.Data.First();
                var mappedMember = MemberMapper.MapToMember(datum);
                
                // Set default AccountStatus if not available
                if (string.IsNullOrEmpty(mappedMember.AccountStatus))
                    mappedMember.AccountStatus = "Credit Only";

                // Check for duplicates
                var duplicates = await FindDuplicatesAsync(new List<Member> { mappedMember });
                if (duplicates.Any())
                    return (false, "Member already exists in the system");

                _memberRepository.Add(mappedMember);
                await _unitOfWork.SaveToDataStoreSync();

                return (true, "Member added successfully");
            }
            catch (Exception ex)
            {
                await _errorService.LogErrorAsync(ex);
                return (false, $"Error adding member: {ex.Message}");
            }
        }
    }
}
