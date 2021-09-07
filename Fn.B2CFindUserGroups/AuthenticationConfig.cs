// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace Fn.B2CFindUserGroups
{

    public class AuthenticationConfig
    {
        public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";

        public string ApiUrl { get; set; } = "https://graph.microsoft.com/";

        public string Tenant { get; set; }

        public string ClientId { get; set; }

        public string AADGroupId { get; set; }

        public string Authority
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, Instance, Tenant);
            }
        }

        public string ClientSecret { get; set; }

        public string CertificateName { get; set; }

        public AuthenticationConfig()
        {       
            this.ClientId = System.Environment.GetEnvironmentVariable("Dest_ClientId", EnvironmentVariableTarget.Process);
            this.ClientSecret = System.Environment.GetEnvironmentVariable("Dest_ClientSecret", EnvironmentVariableTarget.Process);
            this.Tenant = System.Environment.GetEnvironmentVariable("Dest_Tenant", EnvironmentVariableTarget.Process);
        }
    }



}

