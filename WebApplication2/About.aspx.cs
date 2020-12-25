using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.UI;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text;
using Amazon.S3.Model;

namespace WebApplication2
{
    public partial class About : Page
    {
        private static IAmazonS3 s3Client;
        private static AmazonDynamoDBClient ddbClient;
        private const string bucketName = "sonlam32-bucket";
        private static readonly RegionEndpoint clientRegion = RegionEndpoint.USWest2;

        private const string sourceBucket = "css490";
        private const string destinationBucket = "sonlam32-bucket";
        private const string objectKey = "input.txt";
        private const string destObjectKey = "input.txt";

        private static List<string> lines = new List<string>();
        private static string tableName = "Employee";
        private static string queryOutput = "";
        int counter = 0;
        protected void Page_Load(object sender, EventArgs e)
        {
            s3Client = new AmazonS3Client(clientRegion);
            ddbClient = new AmazonDynamoDBClient(clientRegion);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            CopyingObjectAsync();

            if (!GetTableInformation())
            {
                 CreateTable().Wait();
            }
            ParseFile();
            ParseList().Wait();

            Label2.Text = "Data is loaded: BucketURL: https://sonlam32-bucket.s3-us-west-2.amazonaws.com/input.txt";



        }
        private static async Task CopyingObjectAsync()
        {
            try
            {
                CopyObjectRequest request = new CopyObjectRequest
                {
                    SourceBucket = sourceBucket,
                    SourceKey = objectKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destObjectKey
                };
                CopyObjectResponse response = s3Client.CopyObject(request);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        private static void ParseFile()
        {
            using (var client = new WebClient())
            {
                string s = client.DownloadString("https://s3-us-west-2.amazonaws.com/css490/input.txt");
                using (StringReader sr = new StringReader(s))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }
        }
        private static async Task ParseList()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Dictionary<string, AttributeValue> dictionary = new Dictionary<string, AttributeValue>();
                string[] words = lines[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                dictionary.Add("LastName", new AttributeValue { S = words[0] });
                dictionary.Add("FirstName", new AttributeValue { S = words[1] });
                for (int j = 2; j < words.Length; j++)
                {
                    string[] splitAttributes = words[j].Split('=');
                    dictionary.Add(splitAttributes[0], new AttributeValue { S = splitAttributes[1] });
                }
                ExecuteAsync(dictionary).Wait();
                dictionary.Clear();
            }
        }
        private static async Task ExecuteAsync(Dictionary<string, AttributeValue> inputDictionary)
        {
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = inputDictionary
            };
            ddbClient.PutItemAsync(request);
        }
        private static async Task UploadFileAsync()
        {
            try
            {
                var fileTransferUtility =
                    new TransferUtility(s3Client);

                // Option 1. Upload a file. The file name is used as the object key name.
                await fileTransferUtility.UploadAsync("https://s3-us-west-2.amazonaws.com/css490/input.txt", bucketName);
                Console.WriteLine("Upload 1 completed");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }

        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            DeleteTable();
            DeleteObjectNonVersionedBucketAsync().Wait();
            Label3.Text = "Cleared data";
        }

        private static void DeleteTable()
        {
            var request = new DeleteTableRequest
            {
                TableName = tableName
            };

            try 
            {
                var response = ddbClient.DeleteTable(request);
            }
            catch (ResourceNotFoundException)
            {

            }

        }
        private static async Task DeleteObjectNonVersionedBucketAsync()
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "input.txt"
                };

                s3Client.DeleteObject(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
        }

    protected void Button3_Click(object sender, EventArgs e)
        {
            string lastNameText = "";
            string firstNameText = "";
            lastNameText = TextBox1.Text;
            firstNameText = TextBox2.Text;
            QueryDB(lastNameText, firstNameText).Wait();
            string newOutput = queryOutput.Replace("\n", Environment.NewLine);
            if(String.IsNullOrEmpty(queryOutput))
            {
                Label1.Text = "Incorrect data or data not in DynamoDB Table";
            }
            else
            {
                Label1.Text = newOutput.Replace(Environment.NewLine, "<br />");
            }
            queryOutput = "";
        }

        private static bool GetTableInformation()
        {
            Console.WriteLine("\n*** Retrieving table information ***");
            var request = new DescribeTableRequest
            {
                TableName = "Employee"
            };
            try
            {
                var response = ddbClient.DescribeTable(request);
                TableDescription description = response.Table;
                return true;
            }
            catch(Amazon.DynamoDBv2.Model.ResourceNotFoundException ex)
            {
                return false;
            }
        }

        private static async Task CreateTable()
        {
            Console.WriteLine("\n*** Creating table ***");
            var request = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>()
            {
                new AttributeDefinition
                {
                    AttributeName = "LastName",
                    AttributeType = "S"
                },
                new AttributeDefinition
                {
                    AttributeName = "FirstName",
                    AttributeType = "S"
                }
            },
                KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "LastName",
                    KeyType = "HASH" //Partition key
                },
                new KeySchemaElement
                {
                    AttributeName = "FirstName",
                    KeyType = "RANGE" //Sort key
                }
            },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 6
                },
                TableName = tableName
            };

            var response = ddbClient.CreateTableAsync(request);
            WaitUntilTableReady(tableName);

        }
        private static void WaitUntilTableReady(string tableName)
        {
            string status = null;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = ddbClient.DescribeTable(new DescribeTableRequest
                    {
                        TableName = tableName
                    });
                    status = res.Table.TableStatus;
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }

        private static async Task QueryDB(string lastName, string firstName)
        {
            AmazonDynamoDBClient client2 = new AmazonDynamoDBClient(clientRegion);
            var request = new ScanRequest
            {
                TableName = "Employee"
            };
            try
            {
                var response = client2.Scan(request);
                string temp = "";
                foreach (Dictionary<string, AttributeValue> item in response.Items)
                {

                    if (!String.IsNullOrEmpty(lastName) && !String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByBoth(item, lastName, firstName);
                    }
                    if (!String.IsNullOrEmpty(lastName) && String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByLastName(item, lastName);
                    }
                    if (String.IsNullOrEmpty(lastName) && !String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByFirstName(item, firstName);
                    }
                }
            }
            catch (ResourceNotFoundException)
            {

            }
        }
        private static void PrintItemByBoth(
            Dictionary<string, AttributeValue> attributeList, string inputLastName, string inputFirstName)
        {
            bool checker = false;
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            if (temp.Equals(inputLastName) && temp2.Equals(inputFirstName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += attributeName +
                            (value.S == null ? "" : "=" + value.S + " ");                          
                    }
                }
                queryOutput += "\n";
            }
        }
        private static void PrintItemByFirstName(
            Dictionary<string, AttributeValue> attributeList, string inputFirstName)
        {
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            bool checker = false;
            if (temp2.Equals(inputFirstName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += attributeName + (value.S == null ? "" : "=" + value.S + " ");
                    }
                }
                queryOutput += " \n";
            }
        }
        private static void PrintItemByLastName(
            Dictionary<string, AttributeValue> attributeList, string inputLastName)
        {
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            bool checker = false;
            if (temp.Equals(inputLastName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += (attributeName + (value.S == null ? "" : "=" + value.S + " "));
                        Console.Write(
                            attributeName +
                            (value.S == null ? "" : "=" + value.S + " ")
                            );
                    }
                }
                queryOutput += " \n";
            }
        }
        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        protected void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }       
=======
﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.UI;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text;
using Amazon.S3.Model;

namespace WebApplication2
{
    public partial class About : Page
    {
        private static IAmazonS3 s3Client;
        private static AmazonDynamoDBClient ddbClient;
        private const string bucketName = "sonlam32-bucket";
        private static readonly RegionEndpoint clientRegion = RegionEndpoint.USWest2;

        private const string sourceBucket = "css490";
        private const string destinationBucket = "sonlam32-bucket";
        private const string objectKey = "input.txt";
        private const string destObjectKey = "input.txt";

        private static List<string> lines = new List<string>();
        private static string tableName = "Employee";
        private static string queryOutput = "";
        int counter = 0;
        protected void Page_Load(object sender, EventArgs e)
        {
            s3Client = new AmazonS3Client(clientRegion);
            ddbClient = new AmazonDynamoDBClient(clientRegion);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            CopyingObjectAsync();

            if (!GetTableInformation())
            {
                 CreateTable().Wait();
            }
            ParseFile();
            ParseList().Wait();

            Label2.Text = "Data is loaded: BucketURL: https://sonlam32-bucket.s3-us-west-2.amazonaws.com/input.txt";



        }
        private static async Task CopyingObjectAsync()
        {
            try
            {
                CopyObjectRequest request = new CopyObjectRequest
                {
                    SourceBucket = sourceBucket,
                    SourceKey = objectKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destObjectKey
                };
                CopyObjectResponse response = s3Client.CopyObject(request);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        private static void ParseFile()
        {
            using (var client = new WebClient())
            {
                string s = client.DownloadString("https://s3-us-west-2.amazonaws.com/css490/input.txt");
                using (StringReader sr = new StringReader(s))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }
        }
        private static async Task ParseList()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Dictionary<string, AttributeValue> dictionary = new Dictionary<string, AttributeValue>();
                string[] words = lines[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                dictionary.Add("LastName", new AttributeValue { S = words[0] });
                dictionary.Add("FirstName", new AttributeValue { S = words[1] });
                for (int j = 2; j < words.Length; j++)
                {
                    string[] splitAttributes = words[j].Split('=');
                    dictionary.Add(splitAttributes[0], new AttributeValue { S = splitAttributes[1] });
                }
                ExecuteAsync(dictionary).Wait();
                dictionary.Clear();
            }
        }
        private static async Task ExecuteAsync(Dictionary<string, AttributeValue> inputDictionary)
        {
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = inputDictionary
            };
            ddbClient.PutItemAsync(request);
        }
        private static async Task UploadFileAsync()
        {
            try
            {
                var fileTransferUtility =
                    new TransferUtility(s3Client);

                // Option 1. Upload a file. The file name is used as the object key name.
                await fileTransferUtility.UploadAsync("https://s3-us-west-2.amazonaws.com/css490/input.txt", bucketName);
                Console.WriteLine("Upload 1 completed");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }

        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            DeleteTable();
            DeleteObjectNonVersionedBucketAsync().Wait();
            Label3.Text = "Cleared data";
        }

        private static void DeleteTable()
        {
            var request = new DeleteTableRequest
            {
                TableName = tableName
            };

            try 
            {
                var response = ddbClient.DeleteTable(request);
            }
            catch (ResourceNotFoundException)
            {

            }

        }
        private static async Task DeleteObjectNonVersionedBucketAsync()
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "input.txt"
                };

                s3Client.DeleteObject(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
        }

    protected void Button3_Click(object sender, EventArgs e)
        {
            string lastNameText = "";
            string firstNameText = "";
            lastNameText = TextBox1.Text;
            firstNameText = TextBox2.Text;
            QueryDB(lastNameText, firstNameText).Wait();
            string newOutput = queryOutput.Replace("\n", Environment.NewLine);
            if(String.IsNullOrEmpty(queryOutput))
            {
                Label1.Text = "Incorrect data or data not in DynamoDB Table";
            }
            else
            {
                Label1.Text = newOutput.Replace(Environment.NewLine, "<br />");
            }
            queryOutput = "";
        }

        private static bool GetTableInformation()
        {
            Console.WriteLine("\n*** Retrieving table information ***");
            var request = new DescribeTableRequest
            {
                TableName = "Employee"
            };
            try
            {
                var response = ddbClient.DescribeTable(request);
                TableDescription description = response.Table;
                return true;
            }
            catch(Amazon.DynamoDBv2.Model.ResourceNotFoundException ex)
            {
                return false;
            }
        }

        private static async Task CreateTable()
        {
            Console.WriteLine("\n*** Creating table ***");
            var request = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>()
            {
                new AttributeDefinition
                {
                    AttributeName = "LastName",
                    AttributeType = "S"
                },
                new AttributeDefinition
                {
                    AttributeName = "FirstName",
                    AttributeType = "S"
                }
            },
                KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "LastName",
                    KeyType = "HASH" //Partition key
                },
                new KeySchemaElement
                {
                    AttributeName = "FirstName",
                    KeyType = "RANGE" //Sort key
                }
            },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 6
                },
                TableName = tableName
            };

            var response = ddbClient.CreateTableAsync(request);
            WaitUntilTableReady(tableName);

        }
        private static void WaitUntilTableReady(string tableName)
        {
            string status = null;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = ddbClient.DescribeTable(new DescribeTableRequest
                    {
                        TableName = tableName
                    });
                    status = res.Table.TableStatus;
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }

        private static async Task QueryDB(string lastName, string firstName)
        {
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials("AKIA6CB2CFGZMA6KHAMD", "MLzGNigEgcSaGhkmUDKugwgjD1/Xf8f8ip03YRzF");
            AmazonDynamoDBClient client2 = new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.USWest2);
        var request = new ScanRequest
            {
                TableName = "Employee"
            };
            try
            {
                var response = client2.Scan(request);
                string temp = "";
                foreach (Dictionary<string, AttributeValue> item in response.Items)
                {

                    if (!String.IsNullOrEmpty(lastName) && !String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByBoth(item, lastName, firstName);
                    }
                    if (!String.IsNullOrEmpty(lastName) && String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByLastName(item, lastName);
                    }
                    if (String.IsNullOrEmpty(lastName) && !String.IsNullOrEmpty(firstName))
                    {
                        PrintItemByFirstName(item, firstName);
                    }
                }
            }
            catch (ResourceNotFoundException)
            {

            }
        }
        private static void PrintItemByBoth(
            Dictionary<string, AttributeValue> attributeList, string inputLastName, string inputFirstName)
        {
            bool checker = false;
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            if (temp.Equals(inputLastName) && temp2.Equals(inputFirstName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += attributeName +
                            (value.S == null ? "" : "=" + value.S + " ");                          
                    }
                }
                queryOutput += "\n";
            }
        }
        private static void PrintItemByFirstName(
            Dictionary<string, AttributeValue> attributeList, string inputFirstName)
        {
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            bool checker = false;
            if (temp2.Equals(inputFirstName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += attributeName + (value.S == null ? "" : "=" + value.S + " ");
                    }
                }
                queryOutput += " \n";
            }
        }
        private static void PrintItemByLastName(
            Dictionary<string, AttributeValue> attributeList, string inputLastName)
        {
            string temp = attributeList["LastName"].S;
            string temp2 = attributeList["FirstName"].S;
            bool checker = false;
            if (temp.Equals(inputLastName))
            {
                queryOutput += "LastName=" + temp + " FirstName=" + temp2 + " ";
                checker = true;
            }

            if (checker)
            {
                foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;
                    if (!attributeName.Equals("LastName") && !attributeName.Equals("FirstName"))
                    {
                        queryOutput += (attributeName + (value.S == null ? "" : "=" + value.S + " "));
                        Console.Write(
                            attributeName +
                            (value.S == null ? "" : "=" + value.S + " ")
                            );
                    }
                }
                queryOutput += " \n";
            }
        }
        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        protected void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }       
}
