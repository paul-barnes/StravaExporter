/* 
 * Strava API v3
 *
 * Strava API
 *
 * OpenAPI spec version: 3.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using RestSharp;
using NUnit.Framework;

using IO.Swagger.Client;
using IO.Swagger.Api;
using IO.Swagger.Model;

namespace IO.Swagger.Test
{
    /// <summary>
    ///  Class for testing AthletesApi
    /// </summary>
    /// <remarks>
    /// This file is automatically generated by Swagger Codegen.
    /// Please update the test case below to test the API endpoint.
    /// </remarks>
    [TestFixture]
    public class AthletesApiTests
    {
        private AthletesApi instance;

        /// <summary>
        /// Setup before each unit test
        /// </summary>
        [SetUp]
        public void Init()
        {
            instance = new AthletesApi();
        }

        /// <summary>
        /// Clean up after each unit test
        /// </summary>
        [TearDown]
        public void Cleanup()
        {

        }

        /// <summary>
        /// Test an instance of AthletesApi
        /// </summary>
        [Test]
        public void InstanceTest()
        {
            // TODO uncomment below to test 'IsInstanceOfType' AthletesApi
            //Assert.IsInstanceOfType(typeof(AthletesApi), instance, "instance is a AthletesApi");
        }

        
        /// <summary>
        /// Test GetLoggedInAthlete
        /// </summary>
        [Test]
        public void GetLoggedInAthleteTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //var response = instance.GetLoggedInAthlete();
            //Assert.IsInstanceOf<DetailedAthlete> (response, "response is DetailedAthlete");
        }
        
        /// <summary>
        /// Test GetLoggedInAthleteZones
        /// </summary>
        [Test]
        public void GetLoggedInAthleteZonesTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //var response = instance.GetLoggedInAthleteZones();
            //Assert.IsInstanceOf<Zones> (response, "response is Zones");
        }
        
        /// <summary>
        /// Test GetStats
        /// </summary>
        [Test]
        public void GetStatsTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? id = null;
            //int? page = null;
            //int? perPage = null;
            //var response = instance.GetStats(id, page, perPage);
            //Assert.IsInstanceOf<ActivityStats> (response, "response is ActivityStats");
        }
        
        /// <summary>
        /// Test UpdateLoggedInAthlete
        /// </summary>
        [Test]
        public void UpdateLoggedInAthleteTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //float? weight = null;
            //var response = instance.UpdateLoggedInAthlete(weight);
            //Assert.IsInstanceOf<DetailedAthlete> (response, "response is DetailedAthlete");
        }
        
    }

}
