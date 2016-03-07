﻿using NVA_DotNetReferenceImplementation.SchoolID.Operations;
using System;

namespace NVA_DotNetReferenceImplementation.SchoolID
{
    /// <summary>
    /// SchoolIDServiceUtil is a Singleton implementation giving access to several functionalities as presented by the School ID services.
    /// For each functionality, the appropriate Service Operation is invoked to perform the operation. Optionally, the Singleton can be 
    /// used to cache some of the retrieved data (such as the Chains and Sectors), although storage in an external database is preferred.
    /// Be sure to check often if the stored data still matches the live data, as chains and sectors may have been added.
    /// </summary>
    public class SchoolIDServiceUtil
    {
        /// <summary>
        /// The SOAP proxy class which can directly be used to communicate with the School ID SOAP service
        /// </summary>
        private SchoolIDClient schoolIDClient;
       
        private static SchoolIDServiceUtil instance;

        private SchoolIDServiceUtil() {
            schoolIDClient = new SchoolIDClient();
        }

        public static SchoolIDServiceUtil Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SchoolIDServiceUtil();
                }
                return instance;
            }
        }

        /// <summary>
        /// Checks whether the School ID service is up and running
        /// </summary>
        /// <returns>TRUE if all systems are up</returns>
        public bool isSchoolIDAvailable()
        {
            PingOperation pingOperation = new PingOperation(schoolIDClient);
            return pingOperation.isAvailable();
        }

        /// <summary>
        /// Retrieves the current DateTime on the School ID server.
        /// </summary>
        /// <returns>DataTime.Now of the School ID server</returns>
        public DateTime? getSchoolIDDateTime()
        {
            PingOperation pingOperation = new PingOperation(schoolIDClient);
            return pingOperation.getSchoolIDDateTime();
        }

        /// <summary>
        /// Retrieves the current version number of the School ID service.
        /// </summary>
        /// <returns>A string containing the current version number of the School ID service</returns>
        public string getSchoolIDVersion()
        {
            PingOperation pingOperation = new PingOperation(schoolIDClient);
            return pingOperation.getSchoolIDVersion();
        }

        /// <summary>
        /// Retrieves a list of currently available Chains present in the School ID service.
        /// </summary>
        /// <returns>A Chain[] containing all active chains</returns>
        public Chain[] getChains()
        {
            RetrieveChainsOperation retrieveChainsOperation = new RetrieveChainsOperation(schoolIDClient);
            return retrieveChainsOperation.getChains();
        }

        /// <summary>
        /// Retrieves a list of currently available Sectors present in the School ID service.
        /// </summary>
        /// <returns>A Sector[] containing all active sectors</returns>
        public Sector[] getSectors()
        {
            RetrieveSectorsOperation retrieveSectorsOperation = new RetrieveSectorsOperation(schoolIDClient);
            return retrieveSectorsOperation.getSectors();
        }
    }
}