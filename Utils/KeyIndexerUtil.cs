using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;

namespace VocalKnight.Utils
{
    internal static class KeyIndexerUtil
    {
        private static DocsService service;

        public async static Task GetCredentials(Stream jsonStream)
        {
            if (jsonStream == null)
            {
                Logger.LogError("Json credentials read as null");
                return;
            }

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                              GoogleClientSecrets.FromStream(jsonStream).Secrets,
                                              new[] { DocsService.Scope.Documents },
                                              "user", CancellationToken.None);

            service = new DocsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "VocalKnight",
            });
        }

        public static void WriteToFile()
        {
            Document kwDoc = new Document();
            string[] commands = RecognizerUtil.GetCommands();

            kwDoc.Title = "VocalKnight Keywords " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

            List<Request> requests = new List<Request>();

            InsertTableRequest insertTableRQ = new InsertTableRequest();
            insertTableRQ.EndOfSegmentLocation = new EndOfSegmentLocation();
            insertTableRQ.Rows = commands.Count() + 1;
            insertTableRQ.Columns = 5;
            requests.Add(new Request());
            requests[0].InsertTable = insertTableRQ;

            //TABLE INDEXING RULES:
            //These rules apply when the table is empty
            //Adding values to the table shifts indices forward
            //
            // Table begins at index 5
            // Next column add 2
            // Go to next row (from last column) add 3
            // Index of last column: 5 + Rs*((Cs - 1)*2 + 3) - 3
            int index = 2 + (int)insertTableRQ.Rows * 11;
            for (int c = commands.Count() - 1; c >= 0; c--)
            {
                foreach (string kw in RecognizerUtil.GetKeywords(commands[c]).Reverse<string>())
                {
                    CreateTextRQ(ref requests, kw, index);
                    index -= 2;
                }
                CreateTextRQ(ref requests, commands[c], index);
                index -= 3;
            }
            string[] colTitles = new string[] { "Keyword 4", "Keyword 3", "Keyword 2", "Keyword 1", "Effect" };
            foreach (string title in colTitles)
            {
                CreateTextRQ(ref requests, title, index);
                index -= 2;
            }

            BatchUpdateDocumentRequest body = new BatchUpdateDocumentRequest();
            body.Requests = requests;
            BatchUpdateDocumentResponse response = new BatchUpdateDocumentResponse();
            kwDoc = service.Documents.Create(kwDoc).Execute();
            service.Documents.BatchUpdate(body, kwDoc.DocumentId).Execute();
        }

        private static void CreateTextRQ(ref List<Request> requests, string text, int loc)
        {
            requests.Add(new Request());
            InsertTextRequest textRQ = new InsertTextRequest();
            textRQ.Text = text;
            textRQ.Location = new Location();
            textRQ.Location.Index = loc;
            requests[requests.Count() - 1].InsertText = textRQ;
        }
    }
}
