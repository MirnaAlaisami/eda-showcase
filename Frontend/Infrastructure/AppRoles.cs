using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Frontend.Infrastructure
{
    /// <summary>
    /// Contains a list of all the Azure AD app roles this app depends on and works with.
    /// </summary>
    public static class AppRole
    {
        /// <summary>
        /// User can upload to Azure Blob Storage.
        /// </summary>
        public const string AzureUpload = "AzureUpload";
    }
    /// <summary>
    /// Wrapper class that contain all the authorization policies available in this application.
    /// </summary>
    public static class AuthorizationPolicies
    {
        public const string AssignmentToAzureUploadRoleRequired = "AssignmentToAzureUploadRoleRequired";
    }
}
