using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Security;
using Umbraco.Core.Services;
using System.IO;

namespace uSCORM.uSCORMCode.Events
{
    public class uSCORMEvents : ApplicationEventHandler
    {
        private string rootAssetFolderPath = "uSCORM-Assets";

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            base.ApplicationStarted(umbracoApplication, applicationContext);

            Umbraco.Core.Services.ContentService.Saved += ContentService_Saved;
            Umbraco.Core.Services.ContentService.Deleted += ContentService_Deleted; // fires when an individual node is deleted but won't fire if the node is in the recycle bin and the user chooses to empty the bin.
            Umbraco.Core.Services.ContentService.EmptyingRecycleBin += ContentService_EmptyingRecycleBin; // fires when the recycle bin is emptied.  Deleted event does NOT fire when this event fires.

        }

        /// <summary>
        /// If the user creates a starConfiguration node, create some default child nodes to save the user some time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentService_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {
            foreach (var entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias.Equals("eLearningAsset", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!AssetFolderAction(entity, "CREATE"))
                    {
                        e.Messages.Add(new EventMessage("uSCORM", "Error creating uSCORM asset folder", EventMessageType.Error));

                    }
                }
            }

        }

        private void ContentService_Deleted(IContentService sender, DeleteEventArgs<IContent> e)
        {
            foreach (var entity in e.DeletedEntities)
            {
                if (entity.ContentType.Alias.Equals("eLearningAsset", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!AssetFolderAction(entity, "DELETE"))
                    {
                        e.Messages.Add(new EventMessage("uSCORM", "Error deleting uSCORM asset folder - " + entity.Name.ToString(), EventMessageType.Error));

                    }
                }
            }

        }

        private void ContentService_EmptyingRecycleBin(IContentService sender, RecycleBinEventArgs e)
        {
            
            var CS = ApplicationContext.Current.Services.ContentService;

            foreach (var node in CS.GetByIds(e.Ids))
            {
                if (node.ContentType.Alias.Equals("eLearningAsset", StringComparison.InvariantCultureIgnoreCase))
                {
                    AssetFolderAction(node, "DELETE");
                }
            }

        }

        /// <summary>
        /// A series of actions to do on an assets physical folder.
        /// CREATE - will check if the asset folder exists and if it doesn't, it will create it.
        /// DELETE - will check if the asset folder exists and delete it.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="e"></param>
        private bool AssetFolderAction(IContent entity, string action)
        {
            var errors = new List<string>();

            try
            {

                var assetFolder = HttpContext.Current.Server.MapPath("~/" + rootAssetFolderPath + "/" + entity.Id);
                var pathExists = false;

                // if the folder doesn't exist, create it.
                if (Directory.Exists(assetFolder))
                {
                    pathExists = true;

                }

                switch (action.ToUpper())
                {
                    case "CREATE":

                        if (!pathExists)
                        {
                            Directory.CreateDirectory(assetFolder);
                        }

                        break;

                    case "DELETE":

                        if (pathExists)
                        {
                            Directory.Delete(assetFolder, true);
                        }

                        break;

                    default:
                        throw new Exception("Unrecognised action parameter - " + action);

                }


            }
            catch (Exception err)
            {

                LogHelper.Error(typeof(uSCORMEvents), "Failed to reference and/or perform action on assets folder." + err.Message, err);
                errors.Add("Failed to reference and/or perform action on assets folder.");

                return false;
            }

            return true;

        }

    }
}