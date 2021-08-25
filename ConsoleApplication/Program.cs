namespace EckIdAccept
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel;
    using EckID;
    using EckID.SCrypter;

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
        private static string _studentPgn = "498839436";

        /// <summary>
        /// An example PGN of a teacher
        /// </summary>
        private static string _teacherPgn = "fietsbel";//"20DP teacher@school.com";

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

            try
            {
                // Status information
                if (_eckIdServiceUtil.IsEckIdAvailable())
                {
                    string testSet = "votestset-sb";
                    Qtest7();

                    // Execute a batch operation for retrieving Stampseudonyms
                /*    Dictionary<int, string> listedHpgnDictionary = new Dictionary<int, string>();
                    listedHpgnDictionary.Add(0, studentHpgn);
                    listedHpgnDictionary.Add(1, teacherHpgn);

                    Console.WriteLine("Submitting Stampseudonym batch (with the same input)");
                    ExecuteStampseudonymBatchTest(listedHpgnDictionary);

                    // Retrieve a EckID
                    Console.WriteLine("\nRetrieving EckID for first active sector and first active chain:");
                    Console.WriteLine("Chain Guid:\t\t\t" + _chains[0].id);
                    Console.WriteLine("Sector Guid:\t\t\t" + _sectors[0].id);
                    
                    ExecuteCreateEckIdTest(teacherStampseudonym, _chains[0].id, _sectors[0].id);

                    // Execute a batch operation for retrieving EckIDs
                    Dictionary<int, string> listedStampseudonymDictionary = new Dictionary<int, string>();
                    listedStampseudonymDictionary.Add(0, studentStampseudonym);
                    listedStampseudonymDictionary.Add(1, teacherStampseudonym);

                    Console.WriteLine("Submitting EckId batch (with the same input)");
                    ExecuteEckIdBatchTest(_chains[0].id, _sectors[0].id, listedStampseudonymDictionary);*/
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

        private static List<EckIdTestSetItem> ReadTestSet(string file)
        {
            using (var reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, file)))
            {
                List<EckIdTestSetItem> testSet = new List<EckIdTestSetItem>();
                
                while (!reader.EndOfStream)
                {
                    string[] values = reader.ReadLine().Split(';');

                    testSet.Add(new EckIdTestSetItem
                    {
                        Id = int.Parse(values[0]),
                        Pgn = values[1],
                        hPgn = values[2],
                        Stampseudoniem = values[3],
                        EckId = values[4]
                    });
                }
                return testSet;
            }
        }


        private static void Qtest1()
        {
            Console.WriteLine("Test 1: ping");
            WritePingStatusOutput();
        }

        private static void QtestSP(string file, string usertype)
        {
            var testSet = ReadTestSet(file);
            foreach (var user in testSet)
            {
                try
                {
                    Console.WriteLine($"---- {usertype} " + user.Id);
                    Console.WriteLine("Pgn:\t\t\t\t" + user.Pgn);

                    string generatedHpgn = GenerateScryptHash(user.Pgn);
                    if (generatedHpgn != user.hPgn)
                    {
                        Console.WriteLine("Generated hPGN NOT equal to hPGN in test set");
                        continue;
                    }
                    string generatedStampseudonym = GenerateStampseudonym(generatedHpgn);

                    Console.WriteLine("Retrieved Stampseudonym:\t" + generatedStampseudonym + "\n");

                    string equal = generatedStampseudonym == user.Stampseudoniem ? "Equal" : "NOT equal";
                    Console.WriteLine(equal + " to value in test set:\t" + user.Stampseudoniem + "\n");
                }
                catch (FaultException fe)
                {
                    Console.WriteLine("Fault received: " + fe.Message);
                }
                Console.WriteLine();
            }
        }

        private static void QtestEckId(string file, string usertype)
        {
            var testSet = ReadTestSet(file);

            var chains = _eckIdServiceUtil.GetChains();
            var chainGuid = chains.First().id;
            var sectors = _eckIdServiceUtil.GetSectors();
            var voSector = sectors.Where(item => item.name.StartsWith("Voortgezet")).FirstOrDefault();
            if (voSector is null)
            {
                Console.WriteLine("Error: no sector found for VO");
                return;
            }
            var sectorGuid = voSector.id;
            Console.WriteLine("Selected sector VO:\t\t" + sectorGuid + Environment.NewLine);

            foreach (var user in testSet)
            {
                try
                {
                    Console.WriteLine($"---- {usertype} " + user.Id);
                    Console.WriteLine("Pgn:\t\t\t\t" + user.Pgn);

                    string generatedHpgn = GenerateScryptHash(user.Pgn);
                    if (generatedHpgn != user.hPgn)
                    {
                        Console.WriteLine("Generated hPGN NOT equal to hPGN in test set");
                        continue;
                    }

                    string generatedStampseudonym = GenerateStampseudonym(generatedHpgn);
                    if (generatedStampseudonym != user.Stampseudoniem)
                    {
                        Console.WriteLine("Generated Stampseudoniem NOT equal to Stampseudoniem in test set");
                        continue;
                    }

                    string generatedEckId = GenerateEckId(generatedStampseudonym, chainGuid, sectorGuid);

                    Console.WriteLine("Retrieved Eck iD:\t\t" + generatedEckId + "\n");

                    string equal = generatedEckId == user.EckId ? "Equal" : "NOT equal";
                    Console.WriteLine(equal + " to value in test set:\t" + user.EckId + "\n");
                }
                catch (FaultException fe)
                {
                    Console.WriteLine("Fault received: " + fe.Message);
                }
                Console.WriteLine();
            }
        }

        private static void Qtest2(string file)
        {
            Console.WriteLine("Test 2: ophalen stampseudoniem voor leerlingen" + Environment.NewLine);
            QtestSP(file + "-students.csv", "student");
        }

        private static void Qtest3(string file)
        {
            Console.WriteLine("Test 3: ophalen ECK iD voor leerlingen" + Environment.NewLine);
            var testSet = ReadTestSet(file + "-students.csv");
            QtestEckId(file + "-students.csv", "student");
        }

        private static void Qtest4(string file)
        {
            Console.WriteLine("Test 4: ophalen stampseudoniem voor docenten" + Environment.NewLine);
            QtestSP(file + "-teachers.csv", "teacher");
        }

        private static void Qtest5(string file)
        {
            Console.WriteLine("Test 5: ophalen ECK iD voor docenten" + Environment.NewLine);
            var testSet = ReadTestSet(file + "-teachers.csv");
            QtestEckId(file + "-teachers.csv", "teacher");
        }

        private static void Qtest7()
        {
            Console.WriteLine("Test 7: substitutie" + Environment.NewLine);

            var chains = _eckIdServiceUtil.GetChains();
            var chainGuid = chains.First().id;
            var sectors = _eckIdServiceUtil.GetSectors();
            var voSector = sectors.Where(item => item.name.StartsWith("Voortgezet")).FirstOrDefault();
            if (voSector is null)
            {
                Console.WriteLine("Error: no sector found for VO");
                return;
            }
            var sectorGuid = voSector.id;
            Console.WriteLine("Selected sector VO:\t\t" + sectorGuid + Environment.NewLine);

            string[] PGNs = new string[] { "abitPGN1", "abitPGN2", "abitPGN3", "abitPGN4", "abitPGN5" };

            try
            {
                //Subs 1
                string oldPGN = PGNs[0];
                string oldHpgn = GenerateScryptHash(oldPGN);
                string oldStamPs = GenerateStampseudonym(oldHpgn);
                string newPGN = PGNs[1];
                string newHpgn = GenerateScryptHash(newPGN);
                string newStamPs = GenerateStampseudonym(newHpgn);
                Console.WriteLine($"Old: {oldPGN}, stamdpseudonym: {oldStamPs}");
                Console.WriteLine($"New: {newPGN}, stamdpseudonym: {newStamPs}");

                ReplaceStampseudonym(newHpgn, oldHpgn);

                oldStamPs = GenerateStampseudonym(oldHpgn);
                newStamPs = GenerateStampseudonym(newHpgn);
            }
            catch (FaultException fe)
            {
                Console.WriteLine("Fault received: " + fe.Message);
            }

            try
            {
                //Subs 2
                string oldPGN = PGNs[2];
                string oldHpgn = GenerateScryptHash(oldPGN);
                string oldStamPs = GenerateStampseudonym(oldHpgn);
                string newPGN = PGNs[3];
                string newHpgn = GenerateScryptHash(newPGN);
                string newStamPs = GenerateStampseudonym(newHpgn);
                string otherPGN = PGNs[4];
                string otherHpgn = GenerateScryptHash(otherPGN);
                string otherStamPs = GenerateStampseudonym(otherHpgn);
                Console.WriteLine($"Old: {oldPGN}, stamdpseudonym: {oldStamPs}");
                Console.WriteLine($"New: {newPGN}, stamdpseudonym: {newStamPs}");

                ReplaceStampseudonym(newHpgn, oldHpgn);
                ReplaceStampseudonym(newHpgn, otherHpgn);
                ReplaceStampseudonym(newHpgn, oldHpgn);

                oldStamPs = GenerateStampseudonym(oldHpgn);
                newStamPs = GenerateStampseudonym(newHpgn);

                Console.WriteLine($"Old: {oldPGN}, stamdpseudonym: {oldStamPs}");
                Console.WriteLine($"New: {newPGN}, stamdpseudonym: {newStamPs}");
            }
            catch (FaultException fe)
            {
                Console.WriteLine("Fault received: " + fe.Message);
            }
            Console.WriteLine();
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
        private static string GenerateStampseudonym(string hpgn)
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

        private static string ReplaceStampseudonym(string hpgnNew, string hpgnOld)
        {
            return _eckIdServiceUtil.ReplaceStampseudonym(hpgnNew, hpgnOld, null);
        }

        public static void MainBackup(string[] args)//0000000700012TB34567
        {
            // Disable SSL checks for now
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            // Setup the Service Utility for School ID 
            _eckIdServiceUtil = EckIDServiceUtil.Instance;

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

                    //--------------this is it for a student: --------------
                    WriteAvailableChains();

                    // List available sectors
                    WriteAvailableSectors();

                    // Steps for student, start with Pgn, end with eck id
                    Console.WriteLine("\nRetrieving Stampseudonym:");
                    Console.WriteLine("Pgn:\t\t\t\t" + _studentPgn);
                    string studentHpgn = GenerateScryptHash(_studentPgn);
                    Console.WriteLine("HPgn:\t\t\t\t" + studentHpgn);
                    string studentStampseudonym = GenerateStampseudonym(studentHpgn);
                    Console.WriteLine("Retrieved Stampseudonym:\t" + studentStampseudonym + "\n");
                    ExecuteCreateEckIdTest(studentStampseudonym, _chains[0].id, _sectors[0].id);
                    //--------------END this is it for a student --------------



                    Console.WriteLine("Pgn:\t\t\t\t" + _teacherPgn);
                    string teacherHpgn = GenerateScryptHash(_teacherPgn);
                    Console.WriteLine("HPgn:\t\t\t\t" + teacherHpgn);
                    string teacherStampseudonym = GenerateStampseudonym(teacherHpgn);
                    Console.WriteLine("Retrieved Stampseudonym:\t" + teacherStampseudonym + "\n");
                    ExecuteCreateEckIdTest(teacherStampseudonym, _chains[0].id, _sectors[0].id);

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
    }
}
