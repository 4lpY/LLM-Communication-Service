using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLMCommService.Interfaces;
using LLMCommService.Models;

namespace LLMCommService.Services
{
    public class ClaudeService : IClaudeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _promptTemplate = @"
        You will receive a user message related to mobile utility billing actions (such as bill query, payment, registration, etc).

        Please extract the intent and available parameters and return ONLY a valid JSON object with the following format:
        {
        ""intent"": ""MakePayment"",
        ""parameters"": {
            ""subscriberId"": ""1234"",
            ""month"": ""5"",
            ""year"": ""2024"",
            ""amount"": ""150""
        }
        }

        Here are the expected parameters for common intents:

        - QueryBill / QueryBillDetailed → subscriberId, month, year
        - MakePayment → subscriberId, month, year, amount

        Only include parameters if you are confident the user mentioned them. Do NOT include placeholder or invented data.

        User message:
        ";

        public ClaudeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            string apiKey = _configuration["ClaudeApi:ApiKey"] ?? throw new ArgumentNullException("Claude API key is not found");
            _httpClient.BaseAddress = new Uri(_configuration["ClaudeApi:BaseUrl"] ?? "https://api.anthropic.com/v1/messages");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        public async Task<string> ExtractMessageAsync(string userMessage)
        {
            var ClaudeRequest = new ClaudeRequest
            {
                Model = _configuration["ClaudeApi:ModelName"] ?? "claude-3-5-haiku-20241022",
                MaxTokens = 256,
                Messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage
                    {
                        Role = "user",
                        Content = new List<ClaudeContent>
                        {
                            new ClaudeContent
                            {
                                Type = "text",
                                Text = _promptTemplate + userMessage
                            }
                        }
                    }
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(ClaudeRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("", requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent)
                ?? throw new JsonException("Failed to deserialize Claude response");

            string rawText = claudeResponse.Contents.FirstOrDefault(c => c.Type == "text")?.Text?.Trim()
                ?? throw new InvalidOperationException("No valid text content from Claude");

            // JSON içeriğini ayıkla (açıklamaları hariç tut)
            var match = Regex.Match(rawText, @"\{[\s\S]*\}", RegexOptions.Singleline);
            if (!match.Success)
                throw new JsonException("Claude response did not contain valid JSON.");

            var cleanJson = match.Value;
            var parsedResponse = JsonSerializer.Deserialize<LLMResponse>(cleanJson) ?? throw new JsonException("Failed to deserialize clean JSON from Claude");

            return await DispatchIntentAsync(parsedResponse);
        }

        private async Task<string> DispatchIntentAsync(LLMResponse response)
        {
            var intent = response.Intent;
            var parameters = JsonSerializer.Serialize(response.Parameters);

            string endpoint = intent switch
            {
                "MakePayment" => "/mobile-billing-service/pay",
                "QueryBill" => "/mobile-billing-service/bills",
                "QueryBillDetailed" => "/mobile-billing-service/bills/detailed?page=1&pageSize=10",
                _ => throw new InvalidOperationException($"Unknown intent: {intent}")
            };

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://alpys-gateway.azurewebsites.net/");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration["JWTToken"]);

            var content = new StringContent(parameters, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var apiresponse = await httpClient.PostAsync(endpoint, content);
            apiresponse.EnsureSuccessStatusCode();

            var result = await apiresponse.Content.ReadAsStringAsync();
            return result;
        }
    }
    
}
