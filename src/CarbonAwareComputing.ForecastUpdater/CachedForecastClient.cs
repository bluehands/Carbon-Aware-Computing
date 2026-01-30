using Azure.Identity;
using Azure.Storage.Blobs;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater;

public class CachedForecastClient
{
    private readonly string m_ContainerName;
    private readonly Uri m_BaseUri;

    public CachedForecastClient(string storageAccountName, string containerName)
    {
        m_ContainerName = containerName;
        m_BaseUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
    }
    public async Task<Result<Unit>> UpdateForecastData(ComputingLocation location, (string sdk, string minimized) content, bool writeHistory)
    {
        try
        {
            var credentials = new DefaultAzureCredential();
            var blobServiceClient = new BlobServiceClient(m_BaseUri, credentials);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(m_ContainerName);
            var blobClient = containerClient.GetBlobClient($"{location.Name}.json");
            await blobClient.UploadAsync(new BinaryData(content.sdk), true);
            if (writeHistory)
            {
                blobClient = containerClient.GetBlobClient($"{location.Name}.{DateTimeOffset.Now:s}.json");
                await blobClient.UploadAsync(new BinaryData(content.sdk), true);
            }

            blobClient = containerClient.GetBlobClient($"{location.Name}.min.json");
            await blobClient.UploadAsync(new BinaryData(content.minimized), true);
            return No.Thing;
        }
        catch (Exception ex)
        {
            return Result.Error<Unit>(ex.Message);
        }
    }
}