using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CustomPlugin
{
    public class WorkOrder : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity target = (Entity)context.InputParameters["Target"];

                if (context.MessageName.ToLower() == "create")
                {
                    Create(service, target);
                }
                else if (context.MessageName.ToLower() == "update")
                {
                    Update(service, target, context.PreEntityImages.Contains("preimage") ? (Entity)context.PreEntityImages["preimage"] : null);
                }
            }
        }

        private void Create(IOrganizationService service, Entity target)
        {
           
            if (target.Attributes.Contains("lft_assignedagent"))
            {
                Entity getAssignedAgent = service.Retrieve(target.GetAttributeValue<EntityReference>("lft_assignedagent").LogicalName, target.GetAttributeValue<EntityReference>("lft_assignedagent").Id, new ColumnSet(true));
                if (getAssignedAgent != null)
                {
                    string AgentName = getAssignedAgent.GetAttributeValue<string>("lft_agentname");
                    bool WorksOnMonday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledmonday");
                    bool WorksOnTuesday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledtuesday");
                    bool WorksOnWednesday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledwednesday");
                    bool WorksOnThursday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledthursday");
                    bool WorksOnFriday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledfriday");
                    int scheduleDay = target.GetAttributeValue<OptionSetValue>("lft_scheduledon").Value;

                    bool ItMatches = true;
                    switch (scheduleDay)
                    {
                        case 1:
                            ItMatches = WorksOnMonday;
                            break;
                        case 2:
                            ItMatches = WorksOnTuesday;
                            break;
                        case 3:
                            ItMatches = WorksOnWednesday;
                            break;
                        case 4:
                            ItMatches = WorksOnThursday;
                            break;
                        case 5:
                            ItMatches = WorksOnFriday;
                            break;
                    }

                    if (!ItMatches)
                    {
                        throw new InvalidPluginExecutionException("The Agent " + AgentName + " isn't available on that day");
                    }
                }
            }
        }

        private void Update(IOrganizationService service, Entity target, Entity preImage)
        {
            EntityReference assignedAgentRef = null;
            if (target.Attributes.Contains("lft_assignedagent"))
            {
                assignedAgentRef = target.GetAttributeValue<EntityReference>("lft_assignedagent");
            }
            else if (preImage != null && preImage.Attributes.Contains("lft_assignedagent"))
            {
                assignedAgentRef = preImage.GetAttributeValue<EntityReference>("lft_assignedagent");
            }
            
            if (assignedAgentRef != null && target.Attributes.Contains("lft_scheduledon"))
            {
                int? scheduleDayValue = target.GetAttributeValue<OptionSetValue>("lft_scheduledon")?.Value;

                Entity getAssignedAgent = service.Retrieve(assignedAgentRef.LogicalName, assignedAgentRef.Id, new ColumnSet("lft_isscheduledmonday", "lft_isscheduledtuesday", "lft_isscheduledwednesday", "lft_isscheduledthursday", "lft_isscheduledfriday", "lft_agentname"));

                if (getAssignedAgent != null)
                {
                    bool WorksOnMonday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledmonday");
                    bool WorksOnTuesday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledtuesday");
                    bool WorksOnWednesday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledwednesday");
                    bool WorksOnThursday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledthursday");
                    bool WorksOnFriday = getAssignedAgent.GetAttributeValue<bool>("lft_isscheduledfriday");
                    string AgentName = getAssignedAgent.GetAttributeValue<string>("lft_agentname");

                    int scheduleDay = scheduleDayValue.Value;

                    bool ItMatches = true;
                    switch (scheduleDay)
                    {
                        case 1:
                            ItMatches = WorksOnMonday;
                            break;
                        case 2:
                            ItMatches = WorksOnTuesday;
                            break;
                        case 3:
                            ItMatches = WorksOnWednesday;
                            break;
                        case 4:
                            ItMatches = WorksOnThursday;
                            break;
                        case 5:
                            ItMatches = WorksOnFriday;
                            break;
                    }

                    if (!ItMatches)
                    {
                        throw new InvalidPluginExecutionException("The Agent " + AgentName + " isn't available on that day");
                    }
                }
            }
        }


    }
}
