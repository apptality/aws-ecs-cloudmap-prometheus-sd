using Amazon.Runtime;
using Serilog;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

/// <summary>
/// Contains extension methods for working with AWS paginated responses
/// </summary>
public static class AwsExtensions
{
    /// <summary>
    /// Method to fetch all pages of a paginated AWS response
    /// </summary>
    public static async Task<List<TResponse>> FetchAllPagesAsync<TRequest, TResponse>(
        this IAmazonService _,
        TRequest request,
        Func<TRequest, Task<TResponse>> operation,
        Func<TResponse, string> getNextToken,
        Action<TRequest, string> setNextToken)
        where TRequest : AmazonWebServiceRequest
        where TResponse : AmazonWebServiceResponse
    {
        List<TResponse> allResults = [];
        string? nextToken = null;

        do
        {
            if (nextToken != null)
            {
                setNextToken(request, nextToken);
            }

            var response = await _.ExecuteWithRetryAsync(async () => await operation(request));

            allResults.Add(response);
            nextToken = getNextToken(response);
        } while (!string.IsNullOrWhiteSpace(nextToken));

        return allResults;
    }

    /// <summary>
    /// Executes an asynchronous operation with retry logic for AWS throttling exceptions.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the operation.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="maxRetries">The maximum number of retry attempts. Default is 3.</param>
    /// <param name="retryDelayMilliseconds">The base delay between retries in milliseconds. Default is 500ms.</param>
    /// <returns>The response from the operation.</returns>
    /// <exception cref="AmazonServiceException">Thrown if the operation continues to fail after the maximum number of retries.</exception>
    public static async Task<TResponse> ExecuteWithRetryAsync<TResponse>(
        this IAmazonService _,
        Func<Task<TResponse>> operation,
        short maxRetries = 3,
        short retryDelayMilliseconds = 500)
        where TResponse : AmazonWebServiceResponse
    {
        short retries = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (AmazonServiceException ex) when (IsThrottlingException(ex))
            {
                if (retries >= maxRetries)
                {
                    throw;
                }

                var requestDelay = retryDelayMilliseconds * retries;
                Log.Warning("Throttling exception caught. Retrying in {RequestDelay}ms", requestDelay);
                retries++;
                await Task.Delay(requestDelay);
            }
        }
    }

    /// <summary>
    /// Method to determine if an exception is a throttling exception.
    /// </summary>
    public static bool IsThrottlingException(this AmazonServiceException ex)
    {
        return ex.ErrorCode is "Throttling" or "RequestLimitExceeded";
    }
}