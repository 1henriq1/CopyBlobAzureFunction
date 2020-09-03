using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Utilities;

namespace TimerFunction
{
    public static class Backup
    {
        [FunctionName("Backup")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            CopyToAzure();
        }

        public static async void CopyToAzure()
        {
            var copyFrom = "prod";
            var copyTo = "bkp";
            var accountName = "alurahenriquestorage";
            var accountKey = "HZ+/nSCY8gtf4Hb5fZFYcgRDGHzuidoennov788vfPkYxDtxBVptRjU2JEtM7Smr8As21FVxY+Eba/o+SEryQg==";
            TransferBlob(accountName, accountKey, copyFrom, copyTo);            
        }

        static async void TransferBlob(string accountName, string accountKey, string sourceContainerName, string targetContainerName)
        {
            CloudBlobClient cloudBlobClient = CreateClient(accountName, accountKey);
            CloudBlobContainer sourceContainer = cloudBlobClient.GetContainerReference(sourceContainerName);
            CloudBlobContainer targetContainer = cloudBlobClient.GetContainerReference(targetContainerName);
            if (await CheckContainers(sourceContainer, targetContainer))
            {
                await IterateBlobs(sourceContainer, targetContainer, CopyBlob);
            }
        }

        private static async System.Threading.Tasks.Task<bool> CheckContainers(CloudBlobContainer sourceContainer, CloudBlobContainer targetContainer)
        {
            return await sourceContainer.ExistsAsync() && await targetContainer.ExistsAsync();
        }

        private static async System.Threading.Tasks.Task IterateBlobs(CloudBlobContainer sourceContainer, 
            CloudBlobContainer targetContainer, 
            Func<CloudBlobContainer, CloudBlobContainer, IListBlobItem, System.Threading.Tasks.Task> method)
        {
            foreach (IListBlobItem item in (await sourceContainer.ListBlobsSegmentedAsync(null)).Results)
            {
                await method(sourceContainer, targetContainer, item);
            }
        }

        private static async System.Threading.Tasks.Task CopyBlob(CloudBlobContainer sourceContainer, CloudBlobContainer targetContainer, IListBlobItem item)
        {
            var blob = item as CloudBlockBlob;
            if (blob != null)
            {
                CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(blob.Name);
                CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(blob.Name);
                await targetBlob.StartCopyAsync(sourceBlob);
            }
        }

        private static CloudBlobClient CreateClient(string accountName, string accountKey)
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            return cloudBlobClient;
        }
    }
}
