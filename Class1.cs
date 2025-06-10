using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RegexPlugInDataverse
{
    public class DynamicRegexValidationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get Plugin Execution Context
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Check if operation is Create or Update
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
            {
                string tableName = entity.LogicalName; // Get the table name dynamically

                foreach (var fieldName in entity.Attributes.Keys) // Loop through all fields
                {
                    string fieldValue = entity.GetAttributeValue<string>(fieldName);

                    // Retrieve all regex patterns and error messages for this field & table from Dataverse
                    QueryExpression query = new QueryExpression("ValidationConfig")
                    {
                        ColumnSet = new ColumnSet("RegexPattern", "ErrorMessage"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                {
                    new ConditionExpression("FieldName", ConditionOperator.Equal, fieldName),
                    new ConditionExpression("TableName", ConditionOperator.Equal, tableName)
                }
                        }
                    };

                    EntityCollection results = service.RetrieveMultiple(query);

                    // If no validation rules exist for this field, continue to the next
                    if (results.Entities.Count == 0)
                    {
                        continue;
                    }

                    // Validate the field against all matching regex rules
                    foreach (Entity rule in results.Entities)
                    {
                        string regexPattern = rule.GetAttributeValue<string>("RegexPattern");
                        string errorMessage = rule.GetAttributeValue<string>("ErrorMessage");

                        if (!Regex.IsMatch(fieldValue, regexPattern))
                        {
                            throw new InvalidPluginExecutionException(errorMessage);
                        }
                    }
                }
            }

        }

    }
}