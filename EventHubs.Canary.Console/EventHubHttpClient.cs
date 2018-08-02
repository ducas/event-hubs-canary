using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace EventHubs.Canary.Console
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EventHubHttpClient : IClient
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _httpClientFactory;
        
        private  string _uri;
        private string _sasToken;

        public EventHubHttpClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            var connectionString = configuration.GetConnectionString("EventHub");
            
            _connectionString = connectionString;
            _httpClientFactory = httpClientFactory;

            _uri = GetHttpEndpoint(connectionString);
            _sasToken = GenerateToken(_uri, connectionString);
        }

        public async Task SendAsync(byte[] data, string partitionKey = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(_uri);
                var request = new HttpRequestMessage(HttpMethod.Post, _uri);
                request.Headers.TryAddWithoutValidation("Authorization", _sasToken);
                if (!string.IsNullOrEmpty(partitionKey))
                {
                    request.Headers.Add("BrokerProperties", JsonConvert.SerializeObject(new { PartitionKey = partitionKey }));
                }
                request.Content = new ByteArrayContent(data);
                
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new ApplicationException($"Received {(int)response.StatusCode} ({response.StatusCode}) when publishing message: {content}");
                }
            }
            catch
            {
                _uri = GetHttpEndpoint(_connectionString);
                _sasToken = GenerateToken(_uri, _connectionString);
                throw;
            }
        }
        
        private static string GenerateToken(string resourceUri, string connectionString)
        {
            var policyName = GetPolicyName(connectionString);
            var accessKey = GetAccessKey(connectionString);
            var expirySeconds = DateTime.UtcNow.AddDays(7) - new DateTime(1970, 1, 1);
            var expiry = Convert.ToString((int)expirySeconds.TotalSeconds);
            var stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(accessKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, policyName);
            return sasToken;
        }

        private static string GetHttpEndpoint(string connectionString)
        {
            var endpointMatches = Regex.Match(connectionString, @"Endpoint=sb://(.+)\.servicebus");
            var endpoint = endpointMatches.Groups[1].ToString();
            
            var pathMatches = Regex.Match(connectionString, @"EntityPath=([^;]+)");
            var path = pathMatches.Groups[1].ToString();
            
            return $"https://{endpoint}.servicebus.windows.net/{path}/messages?timeout=60&api-version=2014-01";
        }

        private static string GetPolicyName(string connectionString)
        {
            var matches = Regex.Match(connectionString, @"SharedAccessKeyName=([^;]+);");
            return matches.Groups[1].ToString();
        }

        private static string GetAccessKey(string connectionString)
        {
            var matches = Regex.Match(connectionString, @"SharedAccessKey=([^;]+);");
            return matches.Groups[1].ToString();
        }
    }
}