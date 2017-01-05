// Copyright(c) 2017 Nikki Chadwick
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3Uploader
{
    /// <summary>
    /// This is an example program for my own use, without unit-tests and unstructured.
    /// </summary>
    public static class Program
    {
        private enum ExitCode
        {
            Ok = 0,
            BadUsage = 1,
            BadRegion = 2,
            PermissionDenied = 3,
            AwsError = 4,
            Exception = 127
        }
        

        public static void Main(string[] arguments)
        {
            if (arguments == null || arguments.Length < 5)
            {
                Console.WriteLine("S3Uploader - visit: https://github.com/NyxChadwick/S3Uploader");
                Console.WriteLine($"Usage: {Process.GetCurrentProcess().ProcessName} accessKey secretKey regionName bucket path...");
                Environment.Exit((int)ExitCode.BadUsage);
            }

            var accessKey = arguments[0];
            var secretKey = arguments[1];
            var regionName = arguments[2];
            var bucket = arguments[3];
            
            var region = (RegionEndpoint)(typeof(RegionEndpoint)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(p => p?.FieldType == typeof(RegionEndpoint) && (regionName?.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase) ?? false))
                ?.GetValue(null));

            if (region == null)
            {
                Console.WriteLine($"Unknown AWS region specified: {regionName}");
                Environment.Exit((int)ExitCode.BadRegion);
            }

            var currentFile = "(before files)";

            try
            {
                var credentials = new BasicAWSCredentials(accessKey, secretKey);

                using (var client = new AmazonS3Client(credentials, region))
                {
                    foreach (var path in arguments.Skip(4))
                    {
                        currentFile = path;

                        client.PutObject(new PutObjectRequest {
                            BucketName = bucket,
                            Key = Path.GetFileName(path),
                            FilePath = path
                        });

                        Console.WriteLine($"Successfully wrote file: {path}");
                    }
                    currentFile = "(after files)";
                    Environment.Exit((int)ExitCode.Ok);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && 
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine($"Permission denied putting {currentFile}: {amazonS3Exception.ErrorCode}");
                    Environment.Exit((int)ExitCode.PermissionDenied);
                }
                else
                {
                    Console.WriteLine($"AWS error occurred when putting {currentFile}: {amazonS3Exception.Message}");
                    Environment.Exit((int)ExitCode.AwsError);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception occurred: {exception.Message}");
                Environment.Exit((int)ExitCode.Exception);
            }
        }
    }
}
