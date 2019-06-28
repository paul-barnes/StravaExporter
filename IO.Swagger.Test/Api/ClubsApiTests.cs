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
    ///  Class for testing ClubsApi
    /// </summary>
    /// <remarks>
    /// This file is automatically generated by Swagger Codegen.
    /// Please update the test case below to test the API endpoint.
    /// </remarks>
    [TestFixture]
    public class ClubsApiTests
    {
        private ClubsApi instance;

        /// <summary>
        /// Setup before each unit test
        /// </summary>
        [SetUp]
        public void Init()
        {
            instance = new ClubsApi();
        }

        /// <summary>
        /// Clean up after each unit test
        /// </summary>
        [TearDown]
        public void Cleanup()
        {

        }

        /// <summary>
        /// Test an instance of ClubsApi
        /// </summary>
        [Test]
        public void InstanceTest()
        {
            // TODO uncomment below to test 'IsInstanceOfType' ClubsApi
            //Assert.IsInstanceOfType(typeof(ClubsApi), instance, "instance is a ClubsApi");
        }

        
        /// <summary>
        /// Test GetClubActivitiesById
        /// </summary>
        [Test]
        public void GetClubActivitiesByIdTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? id = null;
            //int? page = null;
            //int? perPage = null;
            //var response = instance.GetClubActivitiesById(id, page, perPage);
            //Assert.IsInstanceOf<List<SummaryActivity>> (response, "response is List<SummaryActivity>");
        }
        
        /// <summary>
        /// Test GetClubAdminsById
        /// </summary>
        [Test]
        public void GetClubAdminsByIdTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? id = null;
            //int? page = null;
            //int? perPage = null;
            //var response = instance.GetClubAdminsById(id, page, perPage);
            //Assert.IsInstanceOf<List<SummaryAthlete>> (response, "response is List<SummaryAthlete>");
        }
        
        /// <summary>
        /// Test GetClubById
        /// </summary>
        [Test]
        public void GetClubByIdTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? id = null;
            //var response = instance.GetClubById(id);
            //Assert.IsInstanceOf<DetailedClub> (response, "response is DetailedClub");
        }
        
        /// <summary>
        /// Test GetClubMembersById
        /// </summary>
        [Test]
        public void GetClubMembersByIdTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? id = null;
            //int? page = null;
            //int? perPage = null;
            //var response = instance.GetClubMembersById(id, page, perPage);
            //Assert.IsInstanceOf<List<SummaryAthlete>> (response, "response is List<SummaryAthlete>");
        }
        
        /// <summary>
        /// Test GetLoggedInAthleteClubs
        /// </summary>
        [Test]
        public void GetLoggedInAthleteClubsTest()
        {
            // TODO uncomment below to test the method and replace null with proper value
            //int? page = null;
            //int? perPage = null;
            //var response = instance.GetLoggedInAthleteClubs(page, perPage);
            //Assert.IsInstanceOf<List<SummaryClub>> (response, "response is List<SummaryClub>");
        }
        
    }

}
