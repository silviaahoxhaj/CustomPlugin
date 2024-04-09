using Microsoft.Xrm.Sdk;
using System;

namespace CustomPlugin
{
    public class Lead : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                Entity target = null;
                target = (Entity)context.InputParameters["Target"];

                string topic = target.GetAttributeValue<string>("subject");
                topic += " " + DateTime.UtcNow.ToString("dd/MM/yyyy");

                if (context.Stage == 40)
                {  
                    Entity task = new Entity("task");
                    task["subject"] = "Follow Up";
                    task["regardingobjectid"] = new EntityReference(target.LogicalName, target.Id);
                    service.Create(task);

                    target["subject"] = topic;
                    service.Update(target);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
