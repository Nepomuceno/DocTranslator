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
            Console.WriteLine("Bla");
            // full path to file in temp location
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    if (formFile.FileName.EndsWith(".docx"))
                    {
                        var filePath = await _translator.Translate(formFile);
                        //await UploadFileToStorage(stream, $"{formFile.FileName}");
                        using (FileStream streamfile = System.IO.File.Open(filePath, FileMode.Open))
                        {
                            await UploadFileToStorage(streamfile, $"{DateTime.UtcNow.Ticks}.docx");
                        }

                    }
                }
            }
            return "OK";
        }
        private static Task UploadFileToStorage(Stream fileStream, string fileName)
        {
            StorageCredentials storageCredentials = new StorageCredentials("ACCOUNT-NAME", "STORAGE-ACCOUNT-KEY");
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("docs");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            return blockBlob.UploadFromStreamAsync(fileStream);
        }
    }

}

