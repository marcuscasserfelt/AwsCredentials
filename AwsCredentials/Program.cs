using Amazon;
using Amazon.Runtime;
using IniParser;
using IniParser.Model;
using MhLabs.AwsCliSso;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AwsCredentials
{
    /// <summary>
    /// Authenticate using <see cref="MhLabs.AwsCliSso"/> and updated credentials in process environment variables, user enviornment variables and .aws credentails file
    /// </summary>
    class Program
    {
        static async Task  Main(string[] args)
        {
            var region = RegionEndpoint.EUWest1;
            var awsConfig = GetAwsConfig();
            var credentials = await Authenticate(awsConfig, region);
            
            UpdateCredentialsFile(credentials.AccessKey, credentials.SecretKey, credentials.Token);
            UpdateEnvironmentVariables(credentials.AccessKey, credentials.SecretKey, credentials.Token, region.SystemName);
            
            Console.WriteLine("Done");
        }

        static async Task<ImmutableCredentials> Authenticate(AwsConfig awsConfig, RegionEndpoint region)
        {
            var service = new AwsCliSsoService(region);
            
            var credentials = await service.GetCredentials(awsConfig.StartUrl, awsConfig.AccountId, awsConfig.RoleName);
            return await credentials.GetCredentialsAsync();
        }

        static void UpdateEnvironmentVariables(string accessKey, string secretKey, string token, string region)
        {
            UpdateEnviornmentVariable("AWS_ACCESS_KEY_ID", accessKey);
            UpdateEnviornmentVariable("AWS_SECRET_ACCESS_KEY", secretKey);
            UpdateEnviornmentVariable("AWS_SESSION_TOKEN", token);
            UpdateEnviornmentVariable("AWS_DEFAULT_REGION", region);
            UpdateEnviornmentVariable("AWS_REGION", region);
        }

        static void UpdateEnviornmentVariable(string key, string value)
        {
            UpdateEnviornmentVariable(key, value, EnvironmentVariableTarget.Process);
            UpdateEnviornmentVariable(key, value, EnvironmentVariableTarget.User);
        }

        static void UpdateEnviornmentVariable(string key, string value, EnvironmentVariableTarget target)
        {
            if (Environment.GetEnvironmentVariable(key, target) != value)
            {
                Environment.SetEnvironmentVariable(key, value, target);
            }
        }

        static void UpdateCredentialsFile(string accessKey, string secretKey, string token)
        {
            var credentialsFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.aws/credentials";
            if (!File.Exists(credentialsFile))
                throw new FileNotFoundException();

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(credentialsFile);

            var awsAccessKeyId = data["default"]["aws_access_key_id"];
            var awsSecretAccessKey = data["default"]["aws_secret_access_key"];
            var awsSessionToken = data["default"]["aws_session_token"];

            // Check if credentials is updated
            if(accessKey != awsAccessKeyId || secretKey != awsSecretAccessKey || token != awsSessionToken)
            {
                data["default"]["aws_access_key_id"] = accessKey;
                data["default"]["aws_secret_access_key"] = secretKey;
                data["default"]["aws_session_token"] = token;
                //parser.WriteFile(credentialsFile, data);

                // fix to avoid UTF-8 BOM
                File.WriteAllText(credentialsFile, data.ToString(), System.Text.Encoding.ASCII);
            }
        }

        static AwsConfig GetAwsConfig()
        {
            var awsConfig = new AwsConfig();
            var configFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.aws/config";
            if (!File.Exists(configFile))
                throw new FileNotFoundException();

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(configFile);

            awsConfig.AccountId = data["default"]["sso_account_id"];
            awsConfig.RoleName = data["default"]["sso_role_name"];
            awsConfig.StartUrl = data["default"]["sso_start_url"];

            if (!awsConfig.IsValid())
            {
                throw new Exception("Credentials not set. Have you run aws configure sso?");
            }

            return awsConfig;
        }

        
    }


}
