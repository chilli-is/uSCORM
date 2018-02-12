using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Security;
using Umbraco.Web.WebApi;
using System.IO.Compression;
using Umbraco.Core.Publishing;

namespace uSCORM.uSCORMCode.Controllers
{
    public class uSCORMController : UmbracoApiController
    {
        private string baseUploadFolder = "~/uSCORM-Assets/";

        public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
        {
            public CustomMultipartFormDataStreamProvider(string path) : base(path) { }

            public override string GetLocalFileName(HttpContentHeaders headers)
            {
                return headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            }
        }

        public async Task<HttpResponseMessage> UploadFileToServer()
        {
            
            var US = ApplicationContext.Current.Services.UserService;
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.BadRequest);

            // get the user id
            int userId = 0;

            try
            {

                var userTicket = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current).GetUmbracoAuthTicket();

                if (userTicket != null)
                {

                    var currentUser = US.GetByUsername(userTicket.Name);
                    userId = US.GetByUsername(currentUser.Username).Id;
                }

            }
            catch (Exception err)
            {

                LogHelper.Error(typeof(uSCORMController), "Failed to retrieve user identifier. " + err.Message, err);
                throw (err);
            }

            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string userUploadFolder = HttpContext.Current.Server.MapPath(baseUploadFolder + "user-" + userId);

            if (!Directory.Exists(userUploadFolder))
            {
                Directory.CreateDirectory(userUploadFolder);
            }

            // save uploaded file to user folder
            var provider = new CustomMultipartFormDataStreamProvider(userUploadFolder);
            var result = await Request.Content.ReadAsMultipartAsync(provider);
            var fileName = Path.GetFileName(result.FileData.First().LocalFileName);
            var nodeId = 0;

            // Show all the key-value pairs, looking for the the key nodeId from the form data posted up
            foreach (var key in provider.FormData.AllKeys)
            {
                foreach (var val in provider.FormData.GetValues(key))
                {
                    
                    if (key.ToUpper() == "NODEID")
                    {
                        nodeId = Int32.Parse(val);

                        // we need a clean, new assets folder, any pre-existing folder needs to be deleted as it's contents are now out of date
                        // TO DO: in future we could zip up the old folder and date stamp it to allow roll backs.
                        var assetFolder = HttpContext.Current.Server.MapPath(baseUploadFolder + "/" + nodeId);
                        var assetArchive = HttpContext.Current.Server.MapPath(baseUploadFolder + "/archive/" + nodeId);

                        // if the archive folder doesn't exist, create it
                        if (!Directory.Exists(assetArchive))
                        {
                            Directory.CreateDirectory(assetArchive);
                        }

                        // if the asset folder exists, zip up the contents and put in the archive folder,
                        // then delete the asset folder and recreate it, making it a clean folder
                        if (Directory.Exists(assetFolder))
                        {
                            
                            // zip up contents and archive, e.g. SCORM-Assets/archive/1070/1070_20180123130522.zip
                            ZipFile.CreateFromDirectory(assetFolder, assetArchive + "/" + nodeId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".zip");

                            Directory.Delete(assetFolder, true);
                            Directory.CreateDirectory(assetFolder);

                        }
                        else  // first time this asset has been uploaded, so create a folder for it
                        {
                            Directory.CreateDirectory(assetFolder);
                            
                        }

                        // find the destination zip file path
                        var destZipFile = Path.Combine(assetFolder, fileName);

                        // move new asset from user upload folder, into asset folder
                        File.Copy(Path.Combine(userUploadFolder, fileName), destZipFile, true);

                        // delete old upload folder
                        Directory.Delete(userUploadFolder, true);

                        // extract new asset zip into folder
                        using (ZipArchive archive = ZipFile.Open(destZipFile, ZipArchiveMode.Read))
                        {
                            archive.ExtractToDirectory(assetFolder);
                        }

                        // delete uploaded zip as it's not needed anymore
                        File.Delete(destZipFile);

                        var status = PopulateAssetUrlProperty(nodeId, userId, assetFolder);

                        if (status.StatusType == PublishStatusType.Success || status.StatusType == PublishStatusType.SuccessAlreadyPublished)
                        {
                            response = Request.CreateResponse(HttpStatusCode.Created);
                            response.Content = new StringContent(fileName);
                            return response;
                        }
                        else
                        {
                            response = Request.CreateResponse(HttpStatusCode.Conflict);
                            response.Content = new StringContent(fileName);
                            return response;
                        }
                    }

                }
            }
            
            response = Request.CreateResponse(HttpStatusCode.BadRequest);
            return response;
            
        }

        private PublishStatus PopulateAssetUrlProperty(int nodeId, int userId, string assetFolder)
        {
            var CS = ApplicationContext.Current.Services.ContentService;

            // populate asset folder url and launch file
            var assetNode = CS.GetById(nodeId);

            var defaultFile = "";

            string[] defaultFiles = { "index.html", "index.htm", "session.html", "session.htm" };

            foreach (string fileName in defaultFiles)
            {
                if (File.Exists(Path.Combine(assetFolder, fileName)))
                {
                    defaultFile = fileName;
                    break;

                }
            }

            assetNode.Properties["assetPackage"].Value = defaultFile;
            
            return CS.SaveAndPublishWithStatus(assetNode, userId, true).Result;
        }
    }
}
