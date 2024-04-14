using System.Web.Http;
using Azure.Identity;
using CarbonAwareComputing.ExecutionForecast.Function;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace CarbonAwareComputing.Functions;

public class ApiRegistration
{
    public static async Task<IActionResult> Register(Stream body, string apiKeyPassword, string mailTemplate, string mailFrom, string tenantId, string clientId, string clientSecret, ILogger log)
    {
        try
        {
            using var streamReader = new StreamReader(body);
            var requestBody = await streamReader.ReadToEndAsync();
            var registration = JsonConvert.DeserializeObject<RegistrationData>(requestBody);
            var mailAddress = registration?.MailAddress;
            if (string.IsNullOrWhiteSpace(mailAddress))
            {
                return new BadRequestObjectResult(new ProblemDetails()
                {
                    Detail = "Invalid or empty mail address",
                    Status = 400
                });
            }

            var apiKey = await StringCipher.EncryptAsync(mailAddress, apiKeyPassword);

            if (!await SendApiKeyAsync(log, mailAddress, mailFrom, apiKey, tenantId, clientId, clientSecret, mailTemplate))
            {
                return new BadRequestObjectResult(new ProblemDetails()
                {
                    Detail = "Could not send the API-Key",
                    Status = 400
                });
            }

            return new NoContentResult();
        }
        catch (Exception ex)
        {
            log.LogError($"Unexpected Error. {ex}");
            return new InternalServerErrorResult();
        }
    }
    private static async Task<bool> SendApiKeyAsync(ILogger log, string mailAddress, string mailFrom, string apiKey, string tenantId, string clientId, string clientSecret, string template)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential);

        Message message = new()
        {
            Subject = "Your API-Key for Carbon Aware Computing Execution Forecast ",
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = template.Replace("{{APIKEY}}", apiKey)
            },
            ToRecipients = new List<Recipient>()
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = mailAddress
                    }
                }
            }
        };

        bool saveToSentItems = true;

        try
        {
            await graphClient.Users[mailFrom]
                .SendMail(message, saveToSentItems)
                .Request()
                .PostAsync();
            return true;
        }
        catch (Exception ex)
        {
            log.LogError($"Could not send mail to {mailAddress}. Error: {ex.Message}");
            return false;
        }
    }
}