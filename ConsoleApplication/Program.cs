#region License
/*
Copyright 2016, Stichting Kennisnet

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#endregion

namespace ConsoleNVAClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using EckID;
    using EckID.SCrypter;

    /// <summary>
    /// The main entry point for the program. This function demonstrates work with Web Services via EckID project.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Object to store the proxy class which is used to communicate with the Nummervoorziening service
        /// </summary>
        private static EckIDServiceUtil _eckIdServiceUtil;

        /// <summary>
        /// Chains retrieved from the Nummervoorziening service
        /// </summary>
        private static Chain[] _chains;

        /// <summary>
        /// Sectors retrieved from the Nummervoorziening service
        /// </summary>
        private static Sector[] _sectors;

        /// <summary>
        /// An example PGN of a student
        /// </summary>
        private static string _studentPgn = "063138219";

        /// <summary>
        /// An example PGN of a teacher
        /// </summary>
        private static string _teacherPgn = "20DP teacher@school.com";

        /// <summary>
        /// Console application entrance of the Reference implementation to demonstrate how a Nummervoorziening client may communicate 
        /// with the Nummervoorziening service.
        /// </summary>
        /// <param name="args">Optional parameters (not used)</param>
        /// 

        /*
         Deze is als volgt opgebouwd: 0000000700099GP40146 (00000007000 + BRIN + laatste 5 cijfers van de OIN van het certificaat 40146).
         Ik heb voor jullie een testschool gemaakt met BRIN 99GP, dit BRIN moet je dus meegeven in het OIN dat je stuurt in het Address: 0000000700099GP40146

         */
        public static void Main(string[] args)//0000000700012TB34567
        {
            // Disable SSL checks for now
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            // Setup the Service Utility for School ID 
            _eckIdServiceUtil = EckIDServiceUtil.Instance;

            Console.WriteLine("Current server information:");

            try
            {
                // Status information
                if (_eckIdServiceUtil.IsEckIdAvailable())
                {
                    // Print some information about the service
                    WritePingStatusOutput();

                    // List available chains
                    WriteAvailableChains();

                    // List available sectors
                    WriteAvailableSectors();

                    // Retrieve a Stampseudonym
                    Console.WriteLine("\nRetrieving Stampseudonym:");
                    Console.WriteLine("Pgn:\t\t\t\t" + _studentPgn);
                    string studentHpgn = GenerateScryptHash(_studentPgn);
                    Console.WriteLine("HPgn:\t\t\t\t" + studentHpgn);
                    string studentStampseudonym = ExecuteCreateStampseudonymTest(studentHpgn);
                    Console.WriteLine("Retrieved Stampseudonym:\t" + studentStampseudonym + "\n");
                    Console.WriteLine("Pgn:\t\t\t\t" + _teacherPgn);
                    string teacherHpgn = GenerateScryptHash(_teacherPgn);
                    Console.WriteLine("HPgn:\t\t\t\t" + teacherHpgn);
                    string teacherStampseudonym = ExecuteCreateStampseudonymTest(teacherHpgn);
                    Console.WriteLine("Retrieved Stampseudonym:\t" + teacherStampseudonym + "\n");

                    // Execute a batch operation for retrieving Stampseudonyms
                    Dictionary<int, string> listedHpgnDictionary = new Dictionary<int, string>();
                    listedHpgnDictionary.Add(0, studentHpgn);
                    listedHpgnDictionary.Add(1, teacherHpgn);

                    Console.WriteLine("Submitting Stampseudonym batch (with the same input)");
                    ExecuteStampseudonymBatchTest(listedHpgnDictionary);

                    // Retrieve a EckID
                    Console.WriteLine("\nRetrieving EckID for first active sector and first active chain:");
                    Console.WriteLine("Chain Guid:\t\t\t" + _chains[0].id);
                    Console.WriteLine("Sector Guid:\t\t\t" + _sectors[0].id);
                    
                    ExecuteCreateEckIdTest(studentStampseudonym, _chains[0].id, _sectors[0].id);
                    ExecuteCreateEckIdTest(teacherStampseudonym, _chains[0].id, _sectors[0].id);

                    // Execute a batch operation for retrieving EckIDs
                    Dictionary<int, string> listedStampseudonymDictionary = new Dictionary<int, string>();
                    listedStampseudonymDictionary.Add(0, studentStampseudonym);
                    listedStampseudonymDictionary.Add(1, teacherStampseudonym);

                    Console.WriteLine("Submitting EckId batch (with the same input)");
                    ExecuteEckIdBatchTest(_chains[0].id, _sectors[0].id, listedStampseudonymDictionary);
                }
                else
                {
                    Console.WriteLine("School ID service is offline or you are not authorized to use it.");
                }
            }
            catch (FaultException fe)
            {
                Console.WriteLine("Fault received: " + fe.Message);
            }
            catch (EndpointNotFoundException enfe)
            {
                Console.WriteLine("Configured Endpoint not found: " + enfe.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        /// <summary>
        /// Displays status information regarding the Nummervoorziening service
        /// </summary>
        private static void WritePingStatusOutput()
        {
            Console.WriteLine("Application version:\t\t" + _eckIdServiceUtil.GetEckIdVersion());
            Console.WriteLine("System time:\t\t\t" + _eckIdServiceUtil.GetEckIdDateTime());
            Console.WriteLine("Available:\t\t\ttrue");
        }

        /// <summary>
        /// Displays the available active chains
        /// </summary>
        private static void WriteAvailableChains()
        {
            _chains = _eckIdServiceUtil.GetChains();

            // List available Chains            
            Console.WriteLine("Count of active chains:\t\t" + _chains.Length);
        }

        /// <summary>
        /// Displays the available active sectors
        /// </summary>
        private static void WriteAvailableSectors()
        {
            _sectors = _eckIdServiceUtil.GetSectors();

            // List available Sectors
            Console.WriteLine("Count of active sectors:\t" + _sectors.Length);
        }

        /// <summary>
        /// Executes tests for retrieving Stampseudonym based on PGN
        /// </summary>
        /// <param name="hpgn">The Pgn that is hashed and send to Nummervoorziening</param>
        /// <returns>The Stampseudonym for the HPgn</returns>
        private static string ExecuteCreateStampseudonymTest(string hpgn)
        {
            // Retrieve Stampseudonym from Nummervoorziening service
            return _eckIdServiceUtil.GenerateStampseudonym(hpgn);
        }

        /// <summary>
        /// Executes tests submitting and retrieving Stampseudonym batch
        /// </summary>
        /// <param name="listedHpgnDictionary">An indexed list of HPGNs</param>
        private static void ExecuteStampseudonymBatchTest(Dictionary<int, string> listedHpgnDictionary)
        {
            try
            {
                string batchIdentifier = _eckIdServiceUtil.SubmitStampseudonymBatch(listedHpgnDictionary);
                Console.WriteLine("Batch identifier:\t\t" + batchIdentifier);
                Console.WriteLine("Waiting for processing...");
                EckIDBatch stampseudonymBatch = _eckIdServiceUtil.RetrieveBatch(batchIdentifier);

                if (stampseudonymBatch != null)
                {
                    string successList = stampseudonymBatch.GetSuccessList().Count > 0
                                             ? stampseudonymBatch.GetSuccessList()
                                                 .Select(x => x.Key + "=" + x.Value)
                                                 .Aggregate((s1, s2) => s1 + ", " + s2)
                                             : string.Empty;
                    string failedList = stampseudonymBatch.GetFailedList().Count > 0
                                             ? stampseudonymBatch.GetSuccessList()
                                                 .Select(x => x.Key + "=" + x.Value)
                                                 .Aggregate((s1, s2) => s1 + ", " + s2)
                                             : string.Empty;
                    Console.WriteLine("Generated Stampseudonyms:\t{" + successList + "}");
                    Console.WriteLine("Failed Stampseudonyms:\t{" + failedList + "}");
                }
                else
                {
                    Console.WriteLine("Error occured: StampseudonymBatch is null.");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception has been thrown: " + e.Message);
            }
        }

        /// <summary>
        /// Executes tests submitting and retrieving EckId batch
        /// </summary>
        /// <param name="chainGuid">The Guid of the Chain</param>
        /// <param name="sectorGuid">The Guid of the Sector</param>
        /// /// <param name="listedStampseudonymDictionary">An indexed list of Stampseudonyms</param>
        private static void ExecuteEckIdBatchTest(
            string chainGuid, string sectorGuid, Dictionary<int, string> listedStampseudonymDictionary)
        {
            try
            {
                string batchIdentifier = _eckIdServiceUtil.SubmitEckIdBatch(listedStampseudonymDictionary, chainGuid, sectorGuid);
                Console.WriteLine("Batch identifier:\t\t" + batchIdentifier);
                Console.WriteLine("Waiting for processing...");
                EckIDBatch eckIdBatch = _eckIdServiceUtil.RetrieveBatch(batchIdentifier);

                if (eckIdBatch != null)
                {
                    string successList = eckIdBatch.GetSuccessList().Count > 0
                        ? eckIdBatch.GetSuccessList()
                            .Select(x => x.Key + "=" + x.Value)
                            .Aggregate((s1, s2) => s1 + ", " + s2)
                        : string.Empty;
                    string failedList = eckIdBatch.GetFailedList().Count > 0
                        ? eckIdBatch.GetSuccessList()
                            .Select(x => x.Key + "=" + x.Value)
                            .Aggregate((s1, s2) => s1 + ", " + s2)
                        : string.Empty;
                    Console.WriteLine("Generated EckIds:\t{" + successList + "}");
                    Console.WriteLine("Failed EckIds:\t{" + failedList + "}");
                }
                else
                {
                    Console.WriteLine("Error occured: EckIdBatch is null.");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception has been thrown: " + e.Message);
            }
        }

        /// <summary>
        /// Executes test cases
        /// </summary>
        /// <param name="stampseudonym">The Stampseudonym input</param>
        /// <param name="chainGuid">A valid Chain Guid</param>
        /// <param name="sectorGuid">A valid Sector Guid</param>
        private static void ExecuteCreateEckIdTest(string stampseudonym, string chainGuid, string sectorGuid)
        {
            try
            {
                Console.WriteLine("Retrieved EckID:\t\t" + GenerateEckId(stampseudonym, chainGuid, sectorGuid));
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception has been thrown: " + e.Message);
            }
        }

        /// <summary>
        /// Uses the scrypt library to provide a hexadecimal scrypt hash of the input
        /// </summary>
        /// <param name="input">The input for the hash</param>
        /// <returns>A scrypt hash in hexadecimal notation</returns>
        private static string GenerateScryptHash(string input)
        {
            ScryptUtil scryptUtil = new ScryptUtil();

            // Get the hash from the scrypt library
            return scryptUtil.GenerateHexHash(input);
        }
        
        /// <summary>
        /// Wrapper function to retrieve a EckID
        /// </summary>
        /// <param name="hpgn">A scrypt hash of a PGN</param>
        /// <param name="chainGuid">A valid Chain Guid</param>
        /// <param name="sectorGuid">A valid Sector Guid</param>
        /// <returns>The generated Stampseudonym</returns>
        private static string GenerateEckId(string hpgn, string chainGuid, string sectorGuid)
        {
            return _eckIdServiceUtil.GenerateEckId(hpgn, chainGuid, sectorGuid);
        }
    }
}
