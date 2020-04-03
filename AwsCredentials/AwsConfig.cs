using System;
using System.Collections.Generic;
using System.Text;

namespace AwsCredentials
{
    internal class AwsConfig
    {
        public string StartUrl { get; set; }
        public string AccountId { get; set; }
        public string RoleName { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(StartUrl)
                && !string.IsNullOrWhiteSpace(AccountId)                
                && !string.IsNullOrWhiteSpace(RoleName);
        }
    }
}
