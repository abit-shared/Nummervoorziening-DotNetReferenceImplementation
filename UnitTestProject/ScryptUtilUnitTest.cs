﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NVA_DotNetReferenceImplementation.SCrypter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject
{
    /// <summary>
    /// Demonstrates correct usage of the ScryptUtil class.
    /// </summary>
    [TestClass]
    public class ScryptUtilUnitTest : AbstractUnitTest
    {
        string inputValue = "063138219";

        /// <summary>
        /// Tests that generated scrypt hash in Base64 notation is correct.
        /// </summary>
        [TestMethod]
        public void GenerateBase64HashTest()
        {
            string expectedValue = "lSN80glj5jADRiAyRVCAmj35i74HdKNsNWv128imXns=";
            ScryptUtil scryptUtil = new ScryptUtil();

            Assert.AreEqual(expectedValue, scryptUtil.GenerateBase64Hash(inputValue));
        }

        /// <summary>
        /// Tests that generated scrypt hash in hexadecimal notation is correct.
        /// </summary>
        [TestMethod]
        public void GenerateHexHashTest()
        {
            string expectedValue = "95237cd20963e630034620324550809a3df98bbe0774a36c356bf5dbc8a65e7b";
            ScryptUtil scryptUtil = new ScryptUtil();

            Assert.AreEqual(expectedValue, scryptUtil.GenerateHexHash(inputValue));
        }
    }
}
