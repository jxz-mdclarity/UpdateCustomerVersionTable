using System;
using System.Configuration;
using System.Data.SqlClient;

namespace UpdateCustomerVersionTable
{
    class Program
    {
        private static void UpdateCustomerVersionTable(string connStr, string version)
        {
            int updatedRows = 0;
            string basePath = @"C:\Builds\";

            version = version.StartsWith("V", StringComparison.OrdinalIgnoreCase) ? version : $"V{version}";
            string binaryPath = version + @"\Console\driver.exe";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                connection.Open();

                SqlCommand command = connection.CreateCommand();
                // Start a local transaction.
                SqlTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);

                // Must assign both transaction object and connection
                // to Command object for a pending local transaction
                command.Connection = connection;
                command.Transaction = transaction;

                try
                {
                    while (true)
                    { 
                        Console.Write("\nEnter customer to be updated or q to quit: ");
                        string customer = Console.ReadLine();

                        if (String.Compare(customer, "q", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            break;
                        }

                        command.CommandText = $"SELECT * FROM [CustomerVersion] WHERE [Customer] = '{customer}'";

                        object obj = command.ExecuteScalar();
                        {
                            if (obj == null)
                            {
                                Console.Write($"\nCustomer '{customer}' does not exist in table CustomerVersion.");
                                while (true)
                                {
                                    Console.Write($"\nAdd customer '{customer}'? (y/n): ");
                                    string answer = Console.ReadLine();
                                    if (String.Compare(answer, "y", StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        command.CommandText =
                                            $"INSERT INTO CustomerVersion VALUES ('{customer}', '{version}', '{basePath}', '{binaryPath}')";
                                        updatedRows += command.ExecuteNonQuery();
                                        break;
                                    }
                                    else if (String.Compare(answer, "n", StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        Console.Write("Invalid input!!!!");
                                    }
                                }
                            }
                            else
                            {
                                command.CommandText =
                                    $"UPDATE CustomerVersion SET [Version] = '{version}', [BasePath] = '{basePath}', [BinaryPath] = '{binaryPath}' WHERE CUSTOMER = '{customer}'";
                                updatedRows += command.ExecuteNonQuery();
                            }
                        }
                    } 

                    // Attempt to commit the transaction.
                    transaction.Commit();

                    Console.WriteLine();
                    string message = updatedRows + (updatedRows == 1 ? " row is " : " rows are ") + "updated in table CustmerVersion.";
                    Console.WriteLine(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);

                    // Attempt to roll back the transaction.
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // This catch block will handle any errors that may have occurred
                        // on the server that would cause the rollback to fail, such as
                        // a closed connection.
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                        throw;
                    }

                    throw;
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please specify the intended version; for example, \"UpdateCustomerVersionTable.exe 6.16.3\".");
            }
            else
            {
#if DEBUG
                string connStr = ConfigurationManager.AppSettings["connStr"].ToString();
#else
                string connStr = "Initial Catalog=DevOps;Data Source=mdclag_listen.mdclarity.local;Integrated Security=SSPI";
#endif
                connStr = connStr.Replace("{DATABASE}", "DevOps");
                UpdateCustomerVersionTable(connStr, args[0]);
            }
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
#endif
        }
    }
}
