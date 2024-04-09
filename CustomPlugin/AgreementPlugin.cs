using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CustomPlugin
{
    public class AgreementPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.MessageName.ToLower() == "create")
            {
                Create(service, context);
            }
            else if (context.MessageName.ToLower() == "update")
            {
                Update(service, context);
            }
        
        }

        private void Create(IOrganizationService service, IPluginExecutionContext context)
        {
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];
                if (targetEntity.Attributes.Contains("lft_account"))
                {
                    EntityReference accountReference = targetEntity.GetAttributeValue<EntityReference>("lft_account");

                    if (targetEntity.Attributes.Contains("lft_agreementtype"))
                    {
                        int agreementType = targetEntity.GetAttributeValue<OptionSetValue>("lft_agreementtype").Value;

                        var fetchXml = $@"<fetch version='1.0' mapping='logical' no-lock='false' distinct='true'>
                                     <entity name='lft_agreement'>
                                         <attribute name='lft_account'/>
                                         <attribute name='lft_agreementtype'/>
                                         <filter type='and'>
                                             <condition attribute='statecode' operator='eq' value='0'/>
                                             <condition attribute='lft_account' operator='eq' value='{accountReference.Id}'/>
                                             <condition attribute='lft_agreementtype' operator='eq' value='{agreementType}'/>
                                         </filter>
                                     </entity>
                                 </fetch>";

                        EntityCollection agreements = service.RetrieveMultiple(new FetchExpression(fetchXml));

                        if (agreementType == 1)
                        {
                            if (agreements.Entities.Count > 0)
                            {
                                throw new InvalidPluginExecutionException($"An agreement of type {(agreementType == 1 ? "Onboarding" : "NDA")} already exists for this account.");
                            }
                            UpdateOpportunities(service, targetEntity.GetAttributeValue<EntityReference>("lft_account"));
                        }
                        if (agreementType == 3)
                        {
                            if (agreements.Entities.Count > 0)
                            {
                                throw new InvalidPluginExecutionException($"An agreement of type {(agreementType == 1 ? "Onboarding" : "NDA")} already exists for this account.");
                            }
                        }
                    }

                }
               
               
            }
        }


        private void Update(IOrganizationService service, IPluginExecutionContext context)
        {
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                Entity agreement = service.Retrieve(targetEntity.LogicalName, targetEntity.Id, new ColumnSet(true));

                if (agreement != null)
                {
                    EntityReference accountReference = agreement.GetAttributeValue<EntityReference>("lft_account");

                    if (targetEntity.Attributes.Contains("lft_agreementtype"))
                    {
                        int agreementType = agreement.GetAttributeValue<OptionSetValue>("lft_agreementtype").Value;

                        var fetchXml = $@"<fetch version='1.0' mapping='logical' no-lock='false' distinct='true'>
                             <entity name='lft_agreement'>
                                 <attribute name='lft_account'/>
                                 <attribute name='lft_agreementtype'/>
                                 <filter type='and'>
                                     <condition attribute='statecode' operator='eq' value='0'/>
                                     <condition attribute='lft_account' operator='eq' value='{accountReference.Id}'/>
                                     <condition attribute='lft_agreementtype' operator='eq' value='{agreementType}'/>
                                 </filter>
                             </entity>
                         </fetch>";

                        EntityCollection agreements = service.RetrieveMultiple(new FetchExpression(fetchXml));

                        if (agreementType == 1)
                        {
                            if (agreements.Entities.Count > 1)
                            {
                                throw new InvalidPluginExecutionException($"An agreement of type {(agreementType == 1 ? "Onboarding" : "NDA")} already exists for this account.");
                            }

                            if (accountReference != null)
                            {
                                UpdateOpportunities(service, accountReference);
                            }
                        }
                        if (agreementType == 3)
                        {
                            if (agreements.Entities.Count > 0)
                            {
                                throw new InvalidPluginExecutionException($"An agreement of type {(agreementType == 1 ? "Onboarding" : "NDA")} already exists for this account.");
                            }
                        }
                    }
                }
            }
        }


        private void UpdateOpportunities(IOrganizationService service, EntityReference account)
        {
            QueryExpression query = new QueryExpression("opportunity");
            query.ColumnSet = new ColumnSet("parentaccountid");

            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            ConditionExpression accountCondition = new ConditionExpression("parentaccountid", ConditionOperator.Equal, account.Id);
            filter.AddCondition(accountCondition);

            query.Criteria = filter;

            EntityCollection opportunities = service.RetrieveMultiple(query);

            foreach (var opportunity in opportunities.Entities)
            {
                Entity updatedOpportunity = new Entity(opportunity.LogicalName, opportunity.Id);

                updatedOpportunity["lft_tcs"] = true;
                service.Update(updatedOpportunity);
            }
        }

    }
}
