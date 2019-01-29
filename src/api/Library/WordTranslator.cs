using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

public class WordTranslator
{
    private HttpClient _client;
    public WordTranslator()
    {
        _client = new HttpClient();
    }
    const string host = "https://api.cognitive.microsofttranslator.com";
    const string route = "/translate?api-version=3.0&to=en";
    const string subscriptionKey = "KEY_CONGNITIVE";
    public async Task<string> Translate(IFormFile file, bool translateHeader, bool translateFooter)
    {
        var filePath = Path.GetTempFileName();
        Console.WriteLine($"[FILE] {filePath}");
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            using (WordprocessingDocument worddoc = WordprocessingDocument.Open(ms, true))
            {
                OpenXmlPowerTools.SimplifyMarkupSettings settings = new OpenXmlPowerTools.SimplifyMarkupSettings
                {
                    AcceptRevisions = false,
                    NormalizeXml = false,         //setting this to false reduces translation quality, but if true some documents have XML format errors when opening
                    RemoveBookmarks = true,
                    RemoveComments = true,
                    RemoveContentControls = true,
                    RemoveEndAndFootNotes = true,
                    RemoveFieldCodes = true,
                    RemoveGoBackBookmark = true,
                    RemoveHyperlinks = false,
                    RemoveLastRenderedPageBreak = true,
                    RemoveMarkupForDocumentComparison = true,
                    RemovePermissions = false,
                    RemoveProof = true,
                    RemoveRsidInfo = true,
                    RemoveSmartTags = true,
                    RemoveSoftHyphens = true,
                    RemoveWebHidden = true,
                    ReplaceTabsWithSpaces = false
                };
                OpenXmlPowerTools.MarkupSimplifier.SimplifyMarkup(worddoc, settings);
                Body body = worddoc.MainDocumentPart.Document.Body;
                var texts = body.Descendants<Text>();
                foreach (var text in texts)
                {
                    if (!string.IsNullOrWhiteSpace(text.Text))
                        text.Text = await this.CallTranslator(text.Text);
                }
                var clone = worddoc.Clone(filePath);
                clone.Close();
                worddoc.Close();
            }
        }
        return filePath;
    }

    private async Task<string> CallTranslator(string sentence)
    {
        System.Object[] body = new System.Object[] { new { Text = sentence } };
        var requestBody = JsonConvert.SerializeObject(body);
        using (var request = new HttpRequestMessage())
        {
            // Set the method to POST
            request.Method = HttpMethod.Post;
            // Construct the full URI
            request.RequestUri = new Uri(host + route);
            // Add the serialized JSON object to your request
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            // Add the authorization header
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            // Send request, get response
            var response = await _client.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic content = JsonConvert.DeserializeObject(jsonResponse);
            // Print the response
            return content[0].translations[0].text;
        }
    }
}
