using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Masters;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Pipelines;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loopholes
{
public class InsertOptionsCheck
{
    // The code used for both pipelines is the same except for the  
    // naming of a single parameter in the args object. uiCopyItems  
    // uses "destination" and uiMoveItems uses "target".
        
    public void ProcessCopy(ClientPipelineArgs args)
    {
        Process(args, "destination");
    }

    public void ProcessMove(ClientPipelineArgs args)
    {
        Process(args, "target");
    }

    private void Process(ClientPipelineArgs args, String paramName)
    {
        Assert.ArgumentNotNull(args, "args");

        // If you see "Insert From Template" when you use
        // the Insert menu, then everything is ok.
        if (HasInsertFromTemplatePermissions())
            return;

        Database db = Factory.GetDatabase(args.Parameters["database"]);
        Assert.IsNotNull(db, "db");

        Item targetItem = GetTargetItem(args, paramName, db);
        Assert.IsNotNull(targetItem, "targetItem");

        List<Item> sourceItems = GetSourceItems(args, db);

        var permitted = GetPermittedTemplateIds(targetItem, db);
        var attempted = GetAttemptedTemplateIds(sourceItems, db);
            
        // If the all attempted items are allowed by the permitted 
        // Insert Options, then everything is ok.
        if (!attempted.Except(permitted).Any())
            return;

        // If you've got this far, then you've 
        // been trying to cheat the system
        args.AbortPipeline();
        SheerResponse.Alert("Your evil plan has been thwarted!", new String[0]);     
    }

    private bool HasInsertFromTemplatePermissions()
    {
        // We know if they have permission by checking if they can access
        // the "Insert From Template" item in the core database

        Database coreDb = Factory.GetDatabase("core");
        Assert.IsNotNull(coreDb, "coreDb");
        String insertFromTemplateId = "{F300EB7B-82ED-4E43-817C-6327E9BD1BD6}";
        var item = coreDb.GetItem(insertFromTemplateId);

        return item != null;     
    }

    private Item GetTargetItem(ClientPipelineArgs args, String name, Database db)
    {
        // This is where to pipelines differ. The ID of the target 
        // item is stored in differentlt named parameters.

        var targetId = args.Parameters[name];
        Assert.IsNotNullOrEmpty(targetId, "id");
        var targetItem = db.GetItem(targetId);
        return targetItem;
    }

    private List<Item> GetSourceItems(ClientPipelineArgs args, Database db)
    {
        var sourceIds = args.Parameters["items"].Split('|').ToList();
        Assert.IsTrue(sourceIds.Any(), "sourceIds.Any()");
        var sourceItems = sourceIds.Select(id => db.GetItem(id)).ToList();
        return sourceItems;
    }

    private List<String> GetPermittedTemplateIds(Item targetItem, Database db)
    {
        // We use the same method that Sitecore uses to get the 
        // Insert Options - GetMasters. 'Master' is an obsolete 
        // term, but it remains in use in the API. 

        List<Item> templates = Masters.GetMasters(targetItem);
        var templateIds = templates.Select(x => x.ID.ToString());
        return templateIds.ToList();
    }

    private List<String> GetAttemptedTemplateIds(List<Item> items, Database db)
    {          
        var templateIds = items.Select(item => item.TemplateID.ToString());
        return templateIds.ToList();
    }
}
}
