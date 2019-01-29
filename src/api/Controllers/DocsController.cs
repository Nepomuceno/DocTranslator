using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DocTranslatorApi.Controllers
{
    [Route("api/docs")]
    [ApiController]
    public class DocsController : ControllerBase
    {
        private WordTranslator _translator;
        public DocsController(WordTranslator translator)
        {
            _translator = translator;
        }
        [HttpPost("upload")]
        public async Task<string> Upload(List<IFormFile> files)
        {
            long size = files.Sum(f => f.Length);
            // full path to file in temp location
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    if (formFile.FileName.EndsWith(".docx"))
                    {
                        var filePath = await _translator.Translate(formFile, false, false);
                        //await UploadFileToStorage(stream, $"{formFile.FileName}");
                        using (FileStream streamfile = System.IO.File.Open(filePath, FileMode.Open))
                        {
                            var blob = await UploadFileToStorage(streamfile, "docs-en", $"{DateTime.UtcNow.Ticks}.docx");
                            return blob.Uri.ToString();
                        }
                    }
                }
            }
            return "OK";
        }
        [HttpGet("")]
        public async Task<List<DocDetails>> List()
        {
            List<DocDetails> items = new List<DocDetails>();
            StorageCredentials storageCredentials = new StorageCredentials("ACCOUNT_NAME", "KEY");
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("docs");
            BlobContinuationToken token = new BlobContinuationToken();
            for (
            BlobResultSegment list = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, null, null, null, null);
            token != null;
            list = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, null, token, null, null))
            {
                token = list.ContinuationToken;
                foreach (var item in list.Results)
                {
                    var itemb = item as CloudBlockBlob;
                    if (itemb != null)
                        items.Add(new DocDetails()
                        {
                            ContentMD5 = itemb.Properties.ContentMD5,
                            Created = itemb.Properties.Created,
                            LastModified = itemb.Properties.LastModified,
                            Name = itemb.Name,
                            Size = itemb.Properties.Length,
                            Uri = itemb.Uri
                        });
                }
            }
            return items;
        }

        [HttpPost("update/{filename}/{language}")]
        [HttpPost("update")]
        public async Task<string> Update(string filename, string language, IFormFile files)
        {
            using (var ms = new MemoryStream())
            {
                await files.CopyToAsync(ms);
                await UploadFileToStorage(ms,$"docs-{language}",filename);
            }
            return "OK";
        }

        private static async Task<CloudBlockBlob> UploadFileToStorage(Stream fileStream, string containerName, string fileName)
        {
            StorageCredentials storageCredentials = new StorageCredentials("ACCOUNT_NAME", "KEY");
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(fileStream);
            return blockBlob;
        }

    }

}

