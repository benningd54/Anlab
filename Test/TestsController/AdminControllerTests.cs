using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Domain;
using AnlabMvc.Controllers;
using AnlabMvc.Models.Roles;
using AnlabMvc.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Shouldly;
using Test.Helpers;
using TestHelpers.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.TestsController
{
    [Trait("Category", "ControllerTests")]
    public class AdminControllerTests
    {
        public Mock<ApplicationDbContext> MockDbContext { get; set; }
        public Mock<HttpContext> MockHttpContext { get; set; }

        public Mock<FakeUserManager> MockUserManager { get; set; }

        public Mock<FakeRoleManager> MockRolemanager { get; set; }

        //Setup Data
        public List<User> UserData { get; set; }


        //Controller
        public AdminController Controller { get; set; }

        public AdminControllerTests()
        {
            MockDbContext = new Mock<ApplicationDbContext>();
            MockHttpContext = new Mock<HttpContext>();

            MockUserManager = new Mock<FakeUserManager>();
            MockRolemanager = new Mock<FakeRoleManager>();


            var mockDataProvider = new Mock<SessionStateTempDataProvider>();


            //Default Data
            UserData = new List<User>();
            for (int i = 0; i < 5; i++)
            {
                var user = CreateValidEntities.User(i + 1, true);
                UserData.Add(user);
            }


            //Setups
            MockDbContext.Setup(a => a.Users).Returns(UserData.AsQueryable().MockDbSet().Object);

            Controller = new AdminController(MockDbContext.Object, MockUserManager.Object, MockRolemanager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = MockHttpContext.Object
                },
                TempData = new TempDataDictionary(MockHttpContext.Object, mockDataProvider.Object)
            };
        }

        #region Index
        
        [Fact(Skip = "Changing how these are done")]
        public async Task TestIndexReturnsViewWithExpectedResults1()
        {
            // Arrange
            //All 5 users are in all roles.
            MockUserManager.Setup(a => a.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(true);
            
            // Act
            var controllerResult = await Controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(controllerResult);
            var modelResult = Assert.IsType<List<UserRolesModel>>(viewResult.Model);
            modelResult.ShouldNotBeNull();
            modelResult.Count.ShouldBe(5);
            foreach (var userRolesModel in modelResult)
            {
                userRolesModel.IsAdmin.ShouldBeTrue();
                userRolesModel.IsLabUser.ShouldBeTrue();
                userRolesModel.IsReports.ShouldBeTrue();
            }
        }

        [Fact(Skip = "Changing how these are done")]
        public async Task TestIndexReturnsViewWithExpectedResults2()
        {
            // Arrange            
            MockUserManager.Setup(a => a.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(false);
            MockUserManager.Setup(a => a.IsInRoleAsync(UserData[1], It.IsAny<string>())).ReturnsAsync(true);
            MockUserManager.Setup(a => a.IsInRoleAsync(UserData[2], RoleCodes.Admin)).ReturnsAsync(true);

            // Act
            var controllerResult = await Controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(controllerResult);
            var modelResult = Assert.IsType<List<UserRolesModel>>(viewResult.Model);
            modelResult.ShouldNotBeNull();
            modelResult.Count.ShouldBe(5);
            modelResult.Single(a => a.User.Id == UserData[1].Id).IsLabUser.ShouldBeTrue();
            modelResult.Single(a => a.User.Id == UserData[1].Id).IsAdmin.ShouldBeTrue();
            modelResult.Single(a => a.User.Id == UserData[1].Id).IsReports.ShouldBeTrue();

            modelResult.Single(a => a.User.Id == UserData[2].Id).IsLabUser.ShouldBeFalse();
            modelResult.Single(a => a.User.Id == UserData[2].Id).IsAdmin.ShouldBeTrue();
            modelResult.Single(a => a.User.Id == UserData[2].Id).IsReports.ShouldBeFalse();

            foreach (var ur in modelResult.Where(a => a.User.Id != UserData[1].Id && a.User.Id != UserData[2].Id).ToArray())
            {
                ur.IsReports.ShouldBeFalse();
                ur.IsAdmin.ShouldBeFalse();
                ur.IsLabUser.ShouldBeFalse();
            }

            MockUserManager.Verify(a => a.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Exactly(15));
        }
        #endregion Index

        #region EditAdmin

        [Fact]
        public async Task TestEditAdminReturnsNotFound()
        {
            // Arrange
            
            // Act
            var controllerResult = await Controller.EditAdmin("XXX");

            // Assert
            Assert.IsType<NotFoundResult>(controllerResult);
        }

        [Fact]
        public async Task TestEditAdminReturnsExpectedResults()
        {
            // Arrange
            MockUserManager.Setup(a => a.IsInRoleAsync(UserData[1], RoleCodes.LabUser)).ReturnsAsync(true);

            // Act
            var controllerResult = await Controller.EditAdmin(UserData[1].Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(controllerResult);
            var modelResult = Assert.IsType<UserRolesModel>(viewResult.Model);
            modelResult.User.Id.ShouldBe(UserData[1].Id);
            modelResult.IsLabUser.ShouldBeTrue();
            modelResult.IsAdmin.ShouldBeFalse();
            modelResult.IsReports.ShouldBeFalse();

            MockUserManager.Verify(a => a.IsInRoleAsync(UserData[1], It.IsAny<string>()), Times.Exactly(3));
        }
        

        #endregion EditAdmin
    }

    [Trait("Category", "Controller Reflection")]
    public class AdminControllerReflectionTests
    {
        private readonly ITestOutputHelper output;
        public ControllerReflection ControllerReflection;

        public AdminControllerReflectionTests(ITestOutputHelper output)
        {
            this.output = output;
            ControllerReflection = new ControllerReflection(this.output, typeof(AdminController));
        }

        [Fact]
        public void TestControllerClassAttributes()
        {
            ControllerReflection.ControllerInherits("ApplicationController");
            var authAttribute = ControllerReflection.ClassExpectedAttribute<AuthorizeAttribute>(3);
            authAttribute.ElementAt(0).Roles.ShouldBe($"{RoleCodes.Admin},{RoleCodes.LabUser}");
            
            ControllerReflection.ClassExpectedAttribute<AutoValidateAntiforgeryTokenAttribute>(3);
            ControllerReflection.ClassExpectedAttribute<ControllerAttribute>(3);
        }

        [Fact]
        public void TestControllerMethodCount()
        {
            ControllerReflection.ControllerPublicMethods(9);
        }

        [Fact]
        public void TestControllerMethodAttributes()
        {

#if DEBUG
            var countAdjustment = 1;
#else
            var countAdjustment = 0;
#endif
            //1
            var indexAuth = ControllerReflection.MethodExpectedAttribute<AuthorizeAttribute>("Index", 2 + countAdjustment, "Index-1", false, showListOfAttributes: false);
            indexAuth.ElementAt(0).Roles.ShouldBe(RoleCodes.Admin);
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("Index", 2 + countAdjustment, "Index-1", false, showListOfAttributes: false);

            //2
            var searchAminUserAuth = ControllerReflection.MethodExpectedAttribute<AuthorizeAttribute>("SearchAminUser", 3 + countAdjustment, "SearchAminUser-1", false, showListOfAttributes: false);
            searchAminUserAuth.ElementAt(0).Roles.ShouldBe(RoleCodes.Admin);
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("SearchAminUser", 3 + countAdjustment, "SearchAminUser-2", false, showListOfAttributes: false);
            ControllerReflection.MethodExpectedAttribute<HttpGetAttribute>("SearchAminUser", 3 + countAdjustment, "SearchAminUser-3", false, showListOfAttributes: false);

            //3
            var editAdminAminUserAuth = ControllerReflection.MethodExpectedAttribute<AuthorizeAttribute>("EditAdmin", 3 + countAdjustment, "EditAdmin-1", false, showListOfAttributes: false);
            editAdminAminUserAuth.ElementAt(0).Roles.ShouldBe(RoleCodes.Admin);
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("SearchAminUser", 3 + countAdjustment, "EditAdmin-2", false, showListOfAttributes: false);
            ControllerReflection.MethodExpectedAttribute<HttpGetAttribute>("EditAdmin", 3 + countAdjustment, "EditAdmin-3", false, showListOfAttributes: false);

            //4
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("ListClients", 1 + countAdjustment, "ListClients-1", false, showListOfAttributes: false);

            //5 & 6
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("EditUser", 1 + countAdjustment, "EditUserGet-1", false, showListOfAttributes: false);

            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("EditUser", 2 + countAdjustment, "EditUserPost-1", true, showListOfAttributes: false);
            ControllerReflection.MethodExpectedAttribute<HttpPostAttribute>("EditUser", 2 + countAdjustment, "EditUserPost-2", true, showListOfAttributes: false);

            //7
            var addUserToRoleAuth = ControllerReflection.MethodExpectedAttribute<AuthorizeAttribute>("AddUserToRole", 3 + countAdjustment, "AddUserToRole-1", false, showListOfAttributes: false);
            addUserToRoleAuth.ElementAt(0).Roles.ShouldBe(RoleCodes.Admin);
            ControllerReflection.MethodExpectedAttribute<HttpPostAttribute>("AddUserToRole", 3 + countAdjustment, "AddUserToRole-2", false, showListOfAttributes: false);
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("AddUserToRole", 3 + countAdjustment, "AddUserToRole-3", false, showListOfAttributes: false);

            //8 
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("MailQueue", 1 + countAdjustment, "MailQueue-1", false, showListOfAttributes: false);

            //9
            ControllerReflection.MethodExpectedAttribute<AsyncStateMachineAttribute>("ViewMessage", 1 + countAdjustment, "ViewMessage-1", false, showListOfAttributes: false);
        }
    }
}
