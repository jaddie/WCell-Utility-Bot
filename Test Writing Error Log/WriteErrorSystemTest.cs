using Jad_Bot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Test_Writing_Error_Log
{
    
    
    /// <summary>
    ///This is a test class for WriteErrorSystemTest and is intended
    ///to contain all WriteErrorSystemTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WriteErrorSystemTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for WriteError
        ///</summary>
        [TestMethod()]
        [DeploymentItem("Jad_Bot.exe")]
        public void WriteErrorTest()
        {
            List<string> error = new List<string>{ "Testing the failure reporting System" };
            bool expected = true;
            bool actual;
            actual = WriteErrorSystem_Accessor.WriteError(error);
            Assert.AreEqual(expected, actual);
        }
    }
}
