using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
//using System.ComponentModel.DataAnnotations.ValidationResult;
using System.Web.Security;
using System.Web.WebPages.Html;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure.DependencyResolution;
using ZTBL_VersionReleaseFormAutomation.Models;
using System.Data.Entity;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using System.Web.UI;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
//using SelectPdf;
using System.Threading;
namespace ZTBL_VersionReleaseFormAutomation.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

       // GET: /Home/
        public static String uname, upp, loginlast;
        public static String utype,urole;
        public static User selectedUser;
        public static String formidd = null;

        public static DatabaseZTBLEntities c;
        //<--------------------------------------                       ------------------------------------------->
        //                                           1st Page. Login.
        [HttpGet]
        public ActionResult Login()
        {
            if (Request.Cookies["VersionReleaseLogin"] != null)
            {
                String PPno, Password;
                PPno = Request.Cookies["VersionReleaseLogin"].Values["username"];
                Password = Request.Cookies["VersionReleaseLogin"].Values["password"];
                ViewBag.Uname = PPno;
                ViewBag.Pass = Password;
            }
             return View();
        }
        
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(Models.User Userr)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            try
            {
                if (IsValid(Userr.PPNumber, Userr.Password))
                {
                    FormsAuthentication.SetAuthCookie(Userr.PPNumber, true);
                    var db = new ZTBL_VersionReleaseFormAutomation.Models.DatabaseZTBLEntities();

                    c = new DatabaseZTBLEntities();
                    c.Database.Connection.Open();
                    User x = new User();
                    x = c.Users.Single(u => u.PPNumber == Userr.PPNumber);
                    loginlast = DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt");
                    x.LastLogin = DateTime.Now.ToString();

                    if (Userr.rememberMe == true)
                    {
                        HttpCookie cookie = new HttpCookie("VersionReleaseLogin");
                        cookie.Values.Add("username", Userr.PPNumber);
                        cookie.Values.Add("password", Userr.Password);
                        cookie.Expires = DateTime.Now.AddDays(1);
                        Response.Cookies.Add(cookie);
                    }

                    uname = c.Users.Single(u => u.PPNumber == Userr.PPNumber).Name;
                    utype = c.Users.Single(u => u.PPNumber == Userr.PPNumber).UserType;
                    urole = c.Users.Single(u => u.PPNumber == Userr.PPNumber).UserRole;
                    upp = Userr.PPNumber;
                    if (FormsAuthentication.FormsCookieName == User.ToString()) { }
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ModelState.AddModelError("Please Insert again.", "Login Details are wrong!");
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View(Userr);
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           2nd Page. Register User
        [HttpGet]
        
        public ActionResult Register()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {

                c = new DatabaseZTBLEntities();
                var f = c.Users.ToList().Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
                ViewBag.Users = f;

            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally 
            {
                c.Database.Connection.Close();
            }

        return View();
        }

        
        [HttpPost]
        public ActionResult Register(Models.User user)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (ModelState.IsValid)
                {
                    c = new DatabaseZTBLEntities();

                    var newUser = c.Users.Create();
                    newUser.Name = user.Name;
                    newUser.PPNumber = user.PPNumber;
                    newUser.Password = user.Password;
                    newUser.Designation = user.Designation;
                    newUser.UserType = user.UserType;
                    newUser.UserRole = user.UserRole;
                    newUser.Email = user.Email;
                    newUser.Boss = user.Boss;

                    c.Users.Add(newUser);
                    c.SaveChanges();
                    return RedirectToAction("AdminPanel", "Home");
                }

                else
                {
                    ModelState.AddModelError("", "Data is incorrect!");
                }

            }
            catch (Exception e)
            {
                if (e.Message.Contains("An error occurred while updating the entries. See the inner exception for details"))
                {
                    ViewBag.Error = "Cannot insert duplicate/incorrect data";
                }
                else
                {
                    ViewBag.Error = e.Message;
                }
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                 //error(e);
                 return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }

        //<--------------------------------------                         ------------------------------------------->
        //                                           Exception Error Page.
        
        public ActionResult error()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View();
        }
        public static int changeformHolderformid;

        //<--------------------------------------                        ------------------------------------------->
        //                                           Form Sending Page.

        public static string sendEmailToUser;
        
        public ActionResult SendForm(int sendformid)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }

            ViewBag.formId = sendformid;
            changeformHolderformid = sendformid;
            c = new DatabaseZTBLEntities();
            var x = c.Users.ToList();

            

            //ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });

            if (urole.Contains("Developer") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("QA") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
           
            c.Database.Connection.Close();
            return View();
        }

        [HttpPost]
        
        public ActionResult SendForm(Models.User u)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            try
            {
                if (u.PPNumber != null)
                {
                    c = new DatabaseZTBLEntities();
                    User ff = c.Users.Single(e => e.PPNumber == u.PPNumber);
                    //yyy.Contains(ff);

                    var process = c.FormDatas.SingleOrDefault(k => k.FormId.Equals(changeformHolderformid));
                    process.FormHolder = u.PPNumber;

                    sendEmailToUser = c.Users.SingleOrDefault(l => l.PPNumber.Equals(u.PPNumber)).Email;
                    c.SaveChanges();
                    sendEmail("form");
                    return Redirect("Dashboard");
                }
                else
                {
                    return Redirect("Dashboard");
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Failure sending mail"))
                {
                    ViewBag.Error = "Form sent but Email cannot be sent due to ZTBL's IP.";
                }
                else
                {
                    ViewBag.Error = e.Message;
                }
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }

        public ActionResult SubmitBugReport(int BugReportID)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            ViewBag.BugReportId = BugReportID;
            changeformHolderformid = BugReportID;

            c = new DatabaseZTBLEntities();
            var xxxxx = c.BugReports.Single(hh => hh.ID.Equals(BugReportID));
            var getformrecord = c.FormDatas.Single(j => j.FormId.Equals(xxxxx.FormId));

            string parser = getformrecord.ProjectId;
            //string parser = getforn.ToString();

            int formsprojectid = int.Parse(parser);
            var x = c.Projects.Single(h => h.Id.Equals(formsprojectid));

            var y = c.BugReports.Single(h => h.ID.Equals(BugReportID));
            ViewBag.ProjectName = y.ProjectName;
            ViewBag.TestingCycle = y.TestingCycle;
            ViewBag.Version = y.Version_Release;
            ViewBag.Type = y.TestingType;
            string submittothisuser = "0";

            if (urole.Contains("Developer") && urole.Contains("SDD"))
            {
                submittothisuser = x.UnitIncharge_SDD;
            }
            else if (urole.Contains("Team") && urole.Contains("SDD"))
            {
                submittothisuser = x.Manager_SDD;
            }
            //else if (urole.Contains("Manager") && urole.Contains("SDD"))
            //{
            //    submittothisuser = x.Manager_QA;
            //}
            else if (urole.Contains("Team") && urole.Contains("QA"))
            {
                submittothisuser = x.UnitIncharge_QA;
            }
            //else if (urole.Contains("Manager") && urole.Contains("QA"))
            //{
            //    submittothisuser = x.Manager_QA;
            //}
            else if (urole.Contains("Team") && urole.Contains("Operations"))
            {
                submittothisuser = x.UnitIncharge_Operations_;
            }
            //else if (urole.Contains("Manager") && urole.Contains("Operations"))
            //{
            //    submittothisuser = x.Manager_Operations;
            //}
            else if (urole.Contains("Manager") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }

            if (submittothisuser != "0")
            {
                var us = c.Users.Single(j => j.PPNumber.Equals(submittothisuser));

                ViewBag.SubmitUserName = us.Name;
                ViewBag.UserPP = us.PPNumber;
                ViewBag.SubmitUserRole = us.UserRole;
            }
            else
            {

            }
            return View();
        }

        [HttpPost]
        public ActionResult SubmitBugReport(Models.User u)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (u.PPNumber != null)
                {
                    c = new DatabaseZTBLEntities();
                    User ff = c.Users.Single(e => e.PPNumber == u.PPNumber);
                    //yyy.Contains(ff);
                    var process = c.FormDatas.SingleOrDefault(k => k.FormId.Equals(changeformHolderformid));
                    process.FormHolder = u.PPNumber;

                    sendEmailToUser = c.Users.SingleOrDefault(l => l.PPNumber.Equals(u.PPNumber)).Email;
                    c.SaveChanges();
                    sendEmail("form", u.submitNote);
                    return Redirect("Dashboard");
                }
                else
                {
                    return Redirect("Dashboard");
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Failure sending mail"))
                {
                    ViewBag.Error = "Form sent but Email cannot be sent due to ZTBL's IP.";
                }
                else
                {
                    ViewBag.Error = e.Message;
                }

                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            //return View();
        }

        public ActionResult SubmitForm(int formid)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            
            ViewBag.formId = formid;
            changeformHolderformid = formid;

            c = new DatabaseZTBLEntities();
            var getformrecord = c.FormDatas.Single(j => j.FormId.Equals(formid));
            string parser = getformrecord.ProjectId;
            int formsprojectid = int.Parse(parser);
            var x = c.Projects.Single(h=>h.Id.Equals(formsprojectid));

            var y = c.FormDatas.Single(h => h.FormId.Equals(formid));
            ViewBag.ProjectName = y.ProjectName;
            ViewBag.Description = y.VersionDescription;
            ViewBag.Version = y.VersionNo;
            ViewBag.Type = y.Type;
            string submittothisuser="0";

            if (urole.Contains("Developer") && urole.Contains("SDD"))
            {
                submittothisuser = x.UnitIncharge_SDD;
            }
            else if (urole.Contains("Team") && urole.Contains("SDD"))
            {
                submittothisuser = x.Manager_SDD;
            }
            //else if (urole.Contains("Manager") && urole.Contains("SDD"))
            //{
            //    submittothisuser = x.Manager_QA;
            //}
            else if (urole.Contains("Team") && urole.Contains("QA"))
            {
                submittothisuser = x.UnitIncharge_QA;
            }
            //else if (urole.Contains("Manager") && urole.Contains("QA"))
            //{
            //    submittothisuser = x.Manager_QA;
            //}
            else if (urole.Contains("Team") && urole.Contains("Operations"))
            {
                submittothisuser = x.UnitIncharge_Operations_;
            }
            //else if (urole.Contains("Manager") && urole.Contains("Operations"))
            //{
            //    submittothisuser = x.Manager_Operations;
            //}
            else if (urole.Contains("Manager") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }

            if (submittothisuser != "0")
            {
                var us = c.Users.Single(j => j.PPNumber.Equals(submittothisuser));

                ViewBag.SubmitUserName = us.Name;
                ViewBag.UserPP = us.PPNumber;
                ViewBag.SubmitUserRole = us.UserRole;
            }
            else
            {

            }
            return View();
        }

        [HttpPost]
        public ActionResult SubmitForm(Models.User u)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (u.PPNumber != null)
                {
                    c = new DatabaseZTBLEntities();
                    User ff = c.Users.Single(e => e.PPNumber == u.PPNumber);
                    //yyy.Contains(ff);
                    var process = c.FormDatas.SingleOrDefault(k => k.FormId.Equals(changeformHolderformid));
                    process.FormHolder = u.PPNumber;
                    
                    sendEmailToUser = c.Users.SingleOrDefault(l => l.PPNumber.Equals(u.PPNumber)).Email;
                    c.SaveChanges();
                    sendEmail("form",u.submitNote);
                    return Redirect("Dashboard");
                }
                else
                {
                    return Redirect("Dashboard");
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Failure sending mail"))
                {
                    ViewBag.Error = "Form sent but Email cannot be sent due to ZTBL's IP.";
                }
                else
                {
                    ViewBag.Error = e.Message;
                }

                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            //return View();
        }

        public ActionResult UserAcceptanceFormDash()
        {

            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    ViewBag.loginlast = loginlast;
                    c = new DatabaseZTBLEntities();
                    //ViewBag.FormName = c.FormDatas.Where(x => x.FormHolder.Equals(upp)).ToList();
                    //ViewBag.FormsCount = c.FormDatas.Where(x => x.FormHolder.Equals(upp)).Count();
                    ViewBag.UAForms = c.UATForms.Where(x => x.Holder_PPNo.Equals(upp)).ToList();

                    return View();
                }
            }
            catch (Exception e)
            {
                ViewBag.error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }

            
            return View();
        }

        public ActionResult CreateUserAcceptanceForm()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                ViewBag.Projects = c.Projects.ToList().Select(hh => new System.Web.Mvc.SelectListItem { Text = hh.ProjectName, Value = hh.Id.ToString() });
            }
            catch (Exception e)
            {
                ViewBag.error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("error");
            }
            finally 
            {
                c.Database.Connection.Close();
            }
            return View();
        }
        [HttpPost]
        public ActionResult CreateUserAcceptanceForm(UATForm uf)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var b = c.UATForms.Create();

                b.Date = uf.Date;
                b.ProjectName = uf.ProjectName;
                b.Version = uf.Version;
                b.Functionalities = uf.Functionalities;
                b.Department = uf.Department;
                b.SVP_PPNo = uf.SVP_PPNo;
                b.SVP_Name = uf.SVP_Name;
                b.SVP_Designation = uf.SVP_Designation;
                b.Holder_PPNo = upp;
                c.UATForms.Add(b);
                c.SaveChanges();
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();

            }
            return Redirect("UserAcceptanceFormDash");
        }

        [HttpGet]
        public ActionResult AddMemberToUAT()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
           
            return View();
        }

        [HttpPost]
        public ActionResult AddMemberToUAT(UAT_Team u)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try 
            {
                c = new DatabaseZTBLEntities();
                c.UAT_Teams.Add(u);
                c.SaveChanges();
            }
            catch(Exception e) 
            {
                ViewBag.Error = e.Message;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }
        public ActionResult ViewUserAcceptanceForm(int Uid)
        {


            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();

                var rec = c.UATForms.SingleOrDefault(u=> u.Id.Equals(Uid));
                ViewBag.UAT = rec;
            }
            catch
            {

            }
            finally
            {
                c.Database.Connection.Close();
            }
            
            return View();
        }

        //<--------------------------------------                                     ------------------------------------------->
        //                                           Email sending page(Bug Report)


        
        public void sendEmail(string selector, string note)
        {

            GMailer.GmailUsername = "versionreleaseztbl@gmail.com";
            GMailer.GmailPassword = "Ztbl1234";

            GMailer mailer = new GMailer();
            mailer.ToEmail = sendEmailToUser;

            if (selector == "form")
            {
                mailer.Subject = "ZTBL(SD & MD): You have recieved a Version Release Form";
                mailer.Body = "<b>Hi,</b><br>"
                + "You have recieved a new Version Release Form.<br><br><b>Form ID:</b> " + changeformHolderformid + ",<br><b> Sender:</b> " + uname + ",<br><b>PPNumber: </b>" + upp + ",<br><b>Role:</b> " + urole + ".<br>"+note+"<br><br>Thank You!<br>Team ZTBL(Version Release).";
            }
            else
            {
                mailer.Subject = "ZTBL(SD & MD)You have recieved a Bug Report";
                mailer.Body = "Hi,<br>"
                + "You have recieved a new Bug Report.<br><b>Bug Report ID: </b>" + changeBRHolderReportId + ", <br><b>Sender:</b> " + uname + ", <br><b>PPNumber:</b> " + upp + ",<br></b>Role: </b>" + urole + ".<br>"+note+"<br><br>Thank You!<br>Team ZTBL(Version Release).";
            }

            mailer.IsHtml = true;
            mailer.Send();
        }
        
        
        public void sendEmail(string selector)
        {

            GMailer.GmailUsername = "versionreleaseztbl@gmail.com";
            GMailer.GmailPassword = "Ztbl1234";

            GMailer mailer = new GMailer();
            mailer.ToEmail = sendEmailToUser;

            if (selector == "form")
            {
                mailer.Subject = "ZTBL(SD & MD): You have recieved a Version Release Form";
                mailer.Body = "Hi!<br>"
                + "You have recieved a new Version Release Form.<br><br>Form ID: " + changeformHolderformid + ",<br> Sender: " + uname + ",<br>PPNumber: " + upp +",<br>Role: "+urole+".<br><br>Thank You!<br>Team ZTBL(Version Release).";
            }
            else
            {
                mailer.Subject = "ZTBL(SD & MD)You have recieved a Bug Report";
                mailer.Body = "Hi!<br>"
                + "You have recieved a new Bug Report.<br>Bug Report ID: " + changeBRHolderReportId + ", <br>Sender: " + uname + ", <br>PPNumber: " + upp + ",<br>Role: " + urole + ".<br><br>Thank You!<br>Team ZTBL(Version Release).";
            }
            
            mailer.IsHtml = true;
            mailer.Send();
        }

        //<--------------------------------------                               ------------------------------------------->
        //                                           Version Release Form Page.
        

        public static int formid;
        public static String Userrole;
        
        public ActionResult VersionReleaseForm(int formId)
        {
            try
            {
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                ViewBag.formId = formId;
                formid = formId;
                if (ViewBag.Username == null)
                {
                    return Redirect("Logout");
                }

                c = new DatabaseZTBLEntities();

                ViewBag.fetchf = c.FormDatas.Single(d => d.FormId.Equals(formId));
                var userrole = c.Users.Single(u => u.PPNumber.Equals(upp)).UserRole;

                FormData ProjectDetails = c.FormDatas.Single(f => f.FormId == formid);
                ViewBag.projectInfo = ProjectDetails;
                ViewBag.ReadOny = "readonly";

                if (userrole.Contains("Manager(SDD)") || userrole.Contains("Team Lead(SDD)") || userrole.Contains("Lead Developer"))
                {
                    ViewBag.addAccess = "allowed";
                }
                else
                {
                    ViewBag.addAccess = "not";
                }
                ViewBag.FeaturesName = c.SoftwareFeatureLists.AsNoTracking().Where(u => u.FormId.Equals(formid)).ToList();

                int xx = c.ReleasedFilesLists.AsNoTracking().Where(u => u.FormId.Equals(formid)).Count();
                ViewBag.filesCount = xx;
                ViewBag.ReleasedFilesName = c.ReleasedFilesLists.AsNoTracking().Where(u => u.FormId.Equals(formid)).ToList();
                if (userrole.Contains("Manager(SDD)") || userrole.Contains("Team Lead(SDD)") || userrole.Contains("Lead Developer"))
                {
                    ViewBag.Access = "allowed";
                }
                else
                {
                    ViewBag.Access = "not";
                }

                var x = c.FormDatas.Single(u => u.FormId.Equals(formId)).FormHolder;
                if (x != upp)
                {
                    ViewBag.isBugReport = "yes";
                }
                c.Database.Connection.Close();
                return View();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
           
        }

        [HttpPost]
        
        public ActionResult VersionReleaseForm(Models.FormData form)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                ViewBag.formId = formid;

                c = new DatabaseZTBLEntities();
                int g = formid;
                FormData x = c.FormDatas.SingleOrDefault(k => k.FormId == g);
                x.FormId = formid;


                if (urole.Contains("SDD"))
                {
                    x.DateAndTime_SDD = form.DateAndTime_SDD;
                    if (form.DepartmentHeadName_SDD != null && form.DepartmentHeadSign_SDD== true)
                    {
                        x.DepartmentHeadName_SDD = form.DepartmentHeadName_SDD;
                        x.DepartmentHeadSign_SDD = form.DepartmentHeadSign_SDD;
                    }
                    else if (form.LeadDeveloperName_SDD != null && form.LeadDeveloperSign_SDD == true)
                    {
                        x.LeadDeveloperName_SDD = form.LeadDeveloperName_SDD;
                        x.LeadDeveloperSign_SDD = form.LeadDeveloperSign_SDD;
                    }
                    else if (form.UnitInchargeName_SDD != null && form.UnitInchargeSign_SDD == true)
                    {
                        x.UnitInchargeName_SDD = form.UnitInchargeName_SDD;
                        x.UnitInchargeSign_SDD = form.UnitInchargeSign_SDD;
                    }
                    x.CodingStdFollowed = form.CodingStdFollowed;
                    x.CpybaselineProvided = form.CpybaselineProvided;
                    
                    x.RelFilesLocation = form.RelFilesLocation;
                    x.RelFilesPlaced = form.RelFilesPlaced;
                    x.RelFoldersLabeled = form.RelFoldersLabeled;
                    x.SFLandRFLisAttached = form.SFLandRFLisAttached;
                    x.UnitTestingDone = form.UnitTestingDone;
                    x.VSSLabel = form.VSSLabel;
                
                }
                else if (urole.Contains("QA"))
                {
                    x.DiscoveredBugsReported = form.DiscoveredBugsReported;
                    x.DiscoveredBugsReportedRepNo = form.DiscoveredBugsReportedRepNo;
                    x.QATeamSatisfied = form.QATeamSatisfied;
                    if (form.LeadName_QA != null && form.LeadSign_QA == true)
                    {
                        x.LeadName_QA = form.LeadName_QA;
                        x.LeadSign_QA = form.LeadSign_QA;
                    }
                    else if (form.ManagerName_QA != null && form.ManagerSign_QA == true)
                    {
                        x.ManagerName_QA = form.ManagerName_QA;
                        x.ManagerSign_QA = form.ManagerSign_QA;
                    }
                    x.DateAndTime_QA = form.DateAndTime_QA;
                    x.SWStabilityComment_QA = form.SWStabilityComment_QA;
                    
                    x.PeerReview = form.PeerReview;
                    x.PeerReviewValues = form.PeerReviewValues;
                    x.FailoverSite_Deployment = form.FailoverSite_Deployment;
                    x.Production_Deployment = form.Production_Deployment;

                    x.C_CCA_ChecklistChecked_QA = form.C_CCA_ChecklistChecked_QA;
                    x.C_CCA_ChecklistChecked_SDD = form.C_CCA_ChecklistChecked_SDD;
                    x.C_CCA_ChecklistConfirmed_Deployment = form.C_CCA_ChecklistConfirmed_Deployment;
                    x.C_CCA_DateAndTime_Deployment = form.C_CCA_DateAndTime_Deployment;
                    x.C_CCA_DateAndTime_QA = form.C_CCA_DateAndTime_QA;
                    x.C_CCA_DateAndTime_SDD = form.C_CCA_DateAndTime_SDD;
                    x.C_CCA_RelFilesMovedOrNot_QA = form.C_CCA_RelFilesMovedOrNot_QA;
                    x.C_CCA_RelFilesMovedOrNot_SDD = form.C_CCA_RelFilesMovedOrNot_SDD;
                    x.C_CCA_RelFilesMovedToLoc_QA = form.C_CCA_RelFilesMovedToLoc_QA;
                    x.C_CCA_RelFilesMovedToLoc_SDD = form.C_CCA_RelFilesMovedToLoc_SDD;
                    x.C_CCA_Sign_Deployment = form.C_CCA_Sign_Deployment;
                    x.C_CCA_Sign_QA = form.C_CCA_Sign_QA;
                    x.C_CCA_Sign_SDD = form.C_CCA_Sign_SDD;
                }
                else if (urole.Contains("Operations"))
                {
                    x.DeployedByName_Deployment = form.DeployedByName_Deployment;
                    x.DeployedBySign_Deployment = form.DeployedBySign_Deployment;
                    if (form.DepartmentHead_Deployment_Name != null && form.DepartmentHead_Deployment_Sign == true)
                    {
                        x.DepartmentHead_Deployment_Name = form.DepartmentHead_Deployment_Name;
                        x.DepartmentHead_Deployment_Sign = form.DepartmentHead_Deployment_Sign;
                    }
                    else if (form.UnitInchargeName_Deployment != null && form.UnitInchargeSign_Deployment == true)
                    {
                        x.UnitInchargeName_Deployment = form.UnitInchargeName_Deployment;
                        x.UnitInchargeSign_Deployment = form.UnitInchargeSign_Deployment;
                    }
                    x.FilesCopiedToDestination_Deployment = form.FilesCopiedToDestination_Deployment;
                    x.FilesExecuted_Deployment = form.FilesExecuted_Deployment;
                    x.DateAndTime_Deployment = form.DateAndTime_Deployment;
                }
                
                c.SaveChanges();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return RedirectToAction("Dashboard");
        }


        //<--------------------------------------                       ------------------------------------------->
        //                                           Delete User page.
        
        public ActionResult Delete(int? pp)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;

            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                String p;
                User objUser = null;
                p = pp.ToString();
                c = new DatabaseZTBLEntities();

                objUser = c.Users.Single(u => u.PPNumber == p);
                if (objUser != null)
                {
                    c.Users.Remove(objUser);
                    c.SaveChanges();
                }
                return RedirectToAction("AdminPanel");
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           Delete Form Page.

        
        public ActionResult DeleteForm(int? id)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                FormData fObj;
                String x;
                int idx = 0;
                if (id != null)
                {
                    x = id.ToString();
                    idx = int.Parse(x);
                }
                c = new DatabaseZTBLEntities();
                fObj = c.FormDatas.Single(f => f.FormId.Equals(idx));

                if (fObj != null)
                {
                    c.FormDatas.Remove(fObj);
                    c.SaveChanges();
                }
                return RedirectToAction("Dashboard");
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
  
        }

        //<--------------------------------------                               ------------------------------------------->
        //                                           Delete Bug Report Page
        
        public ActionResult DeleteBugReport(int deleteBR)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var x = c.BugReports.Single(u=>u.ID.Equals(deleteBR));
                if (x != null)
                {
                    c.BugReports.Remove(x);
                    c.SaveChanges();
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("BugReport");
        }

        //------------------------------------------- Delete Bug ---------------------------------------------->>>>>
        
        public ActionResult DeleteBug(int deleteId)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var x = c.BugReportTables.Single(u => u.ID.Equals(deleteId));
                String red = x.BugReportId;
                if (x != null)
                {
                    int xxxx = c.BugReportTables.Count();
                    c.BugReportTables.Remove(x);
                    c.SaveChanges();
                }
                return Redirect("ViewBugReport?brID="+red);
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }




        //<--------------------------------------                       ------------------------------------------->
        //                                           Create Bug Report Page.

        
        public ActionResult CreateBugReport()
        {

            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var x = c.FormDatas.Where(u => u.FormHolder == upp).ToList();

                List<int> y = new List<int>();
                foreach(FormData j in x){
                    y.Add(j.FormId);
                }
//                ViewBag.formsAvailable = y;
                ViewBag.formsAvailable = c.FormDatas.Where(hh => hh.FormHolder == upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "Project: " + hh.ProjectName + " [" + "Version: " + hh.VersionNo + ", "+"Date: "+ hh.VersionDate+"]", Value = SqlFunctions.StringConvert((double)hh.FormId).Trim() });
                //ViewBag.formsAvailable = c.FormDatas.Where(hh => hh.FormHolder == upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "Id: " + hh.FormId+ " [" + hh.ProjectName + ", Version: " + hh.VersionNo+ "]", Value = hh.FormId.ToString()});

            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }

        [HttpPost]
        
        public ActionResult CreateBugReport(Models.BugReport BR)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var b = c.BugReports.Create();

                string brformid = BR.FormId.ToString();
                int brFormId = int.Parse(brformid);
                b.ProjectName = c.FormDatas.Single(u=>u.FormId.Equals(brFormId)).ProjectName;
                b.FormId = BR.FormId;
                b.BR_Holder = upp;
                //b.Bug_Report_ID = BR.Bug_Report_ID;
                b.Date = System.DateTime.Now.ToString("dd/MM/yyyy");
                b.Reviewer = BR.Reviewer;
                b.TestingType = BR.TestingType;
                b.TestingCycle = BR.TestingCycle;
                b.TesterName = BR.TesterName;
                b.Version_Release = c.FormDatas.Single(u=>u.FormId.Equals(brFormId)).VersionNo;
                b.Data_QAStatus = BR.Data_QAStatus;
                b.Data_Priority = BR.Data_Priority;
                b.Data_DevelopmentStatus = BR.Data_DevelopmentStatus;

                c.BugReports.Add(b);
                c.SaveChanges();
                
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("Dashboard");
        }


        //<--------------------------------------                       ------------------------------------------->
        //                                           Bug Report Sending Page.


        public static int changeBRHolderReportId;
        
        public ActionResult SendBugReport(int sendBRID)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            ViewBag.ReportId = sendBRID;
            changeBRHolderReportId = sendBRID;
            c = new DatabaseZTBLEntities();
            var x = c.Users.ToList();
            var y = c.BugReports.Single(u => u.ID == sendBRID);
            //var z = c.BugReports.Single(u => u.ID == sendBRID);
            ViewBag.ProjectName = y.ProjectName;
            ViewBag.Version = y.Version_Release;
            ViewBag.Date = y.Date;
            //ViewBag.Users = c.Users.Where(hh=>hh.PPNumber != upp).Where(hh=>hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });          
            if (urole.Contains("Developer") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("SDD"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("QA") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("QA"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Team") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp && hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            else if (urole.Contains("Manager") && urole.Contains("Operations"))
            {
                ViewBag.Users = c.Users.Where(hh => hh.PPNumber != upp).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            }
            c.Database.Connection.Close();

            return View();
        }

        [HttpPost]
        
        public ActionResult SendBugReport(Models.User u)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
          try
            {
                if (u.PPNumber != null) { 
                c = new DatabaseZTBLEntities();
                var process = c.BugReports.Single(k => k.ID.Equals(changeBRHolderReportId));
                process.BR_Holder = u.PPNumber;
                sendEmailToUser = c.Users.Single(l =>l.PPNumber.Equals(u.PPNumber)).Email;
                c.SaveChanges();
                sendEmail("report");
                return Redirect("BugReport");
                }
                else
                {
                    return Redirect("BugReport");
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           Bug Report Page.

        
        public ActionResult BugReport()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();

                ViewBag.BRName = c.BugReports.Where(u => u.BR_Holder.Equals(upp)).ToList();
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }

            return View();
        }

        //<--------------------------------------                                   ------------------------------------------->
        //                                           Add Bugs to the Bug Report Page.

        
        public ActionResult AddBugtoReport()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View();
        }

        [HttpPost]
        public ActionResult AddBugtoReport(Models.BugReportTable brt)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                brt.BugReportId = selectedbugreportid.ToString();
                string newWord = selectedbugreportid.ToString();
                int identity = c.BugReportTables.Where(u => u.BugReportId == newWord).Count() + 1;
                c.BugReportTables.Where(u => u.BugReportId ==newWord).Count();
                brt.Table_BugId = identity;
                c.BugReportTables.Add(brt);

                c.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("ViewBugReport?brID="+selectedbugreportid);
        }

        static int bugid = 0;
        public ActionResult EditBug(int BugID)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                bugid = BugID;

                string bid = selectedbugreportid.ToString();
                var BugReport = c.BugReportTables.Single(u => u.BugReportId == bid && u.Table_BugId.Equals(BugID));

                //var f = c.BugReports.Single(ff => ff.)
                
                var SeverityList = new List<System.Web.Mvc.SelectListItem> {
                       new System.Web.Mvc.SelectListItem { Value = "L0: I - Immediate" , Text = "L0: I - Immediate" },
                       new System.Web.Mvc.SelectListItem { Value = "L1: U - Urgent" , Text = "L1: U - Urgent" },
                       new System.Web.Mvc.SelectListItem { Value = "L2: Imp - Important" , Text = "L2: Imp - Important"},
                       new System.Web.Mvc.SelectListItem { Value = "L3: M - Minor" , Text = "L3: M - Minor" },
                       new System.Web.Mvc.SelectListItem { Value = "L4: C - Cosmetic" , Text = "L4: C - Cosmetic" }
                    };
                var DevStatusList = new List<System.Web.Mvc.SelectListItem> {
                       new System.Web.Mvc.SelectListItem { Value = "1-Fixed" , Text = "1-Fixed" },
                       new System.Web.Mvc.SelectListItem { Value = "2-Not a bug" , Text = "2-Not a bug" },
                       new System.Web.Mvc.SelectListItem { Value = "3-Pending" , Text = "3-Pending"},
                       new System.Web.Mvc.SelectListItem { Value = "4-Deffered" , Text = "4-Deffered" },
                       new System.Web.Mvc.SelectListItem { Value = "5-Tool Limitations" , Text = "5-Tool Limitations" },
                       new System.Web.Mvc.SelectListItem { Value = "6-Not to be fixed" , Text = "6-Not to be fixed" },
                       new System.Web.Mvc.SelectListItem { Value = "7-Not Reproducible" , Text = "7-Not Reproducible" },
                       new System.Web.Mvc.SelectListItem { Value = "8-Document Updated" , Text = "8-Document Updated" }
                    };
                var QAStatusList = new List<System.Web.Mvc.SelectListItem> {
                       new System.Web.Mvc.SelectListItem { Value = "L0: I - Immediate" , Text = "L0: I - Immediate" },
                       new System.Web.Mvc.SelectListItem { Value = "L1: U - Urgent" , Text = "L1: U - Urgent" },
                       new System.Web.Mvc.SelectListItem { Value = "L2: Imp - Important" , Text = "L2: Imp - Important"},
                       new System.Web.Mvc.SelectListItem { Value = "L3: M - Minor" , Text = "L3: M - Minor" },
                       new System.Web.Mvc.SelectListItem { Value = "L4: C - Cosmetic" , Text = "L4: C - Cosmetic" }
                    };

                foreach (var s in SeverityList)
                {
                    if (BugReport.Table_Severirty != null)
                    {
                        if (s.Value == BugReport.Table_Severirty.ToString())
                        {
                            s.Selected = true;
                        }
                    }
                }
                foreach (var t in DevStatusList)
                {
                    if (BugReport.Table_DevStatus != null)
                    {
                        if (t.Value == BugReport.Table_DevStatus.ToString())
                        {
                            t.Selected = true;
                        }
                    }
                }
                foreach (var u in QAStatusList)
                {
                    if (BugReport.Table_QAStatus != null)
                    {
                        if (u.Value == BugReport.Table_QAStatus.ToString())
                        {
                            u.Selected = true;
                        }
                    }
                }
                ViewBag.DevStatus = DevStatusList;
                ViewBag.QAStatus = QAStatusList;
                ViewBag.BugTitle = BugReport.Table_BugTitle;
                ViewBag.Description = BugReport.Table_BugDescription;
                ViewBag.Date = BugReport.Table_ReportingDate;
                ViewBag.Severity = SeverityList;
                
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }

        [HttpPost]
        public ActionResult EditBug(BugReportTable b)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                string bid = selectedbugreportid.ToString();
                c = new DatabaseZTBLEntities();
                var values = c.BugReportTables.Single(u => u.BugReportId == bid && u.Table_BugId.Equals(bugid));
                //values.Table_BugDescription = b.Table_BugDescription;
                //values.Table_BugTitle = b.Table_BugTitle;
                values.Table_Severirty = b.Table_Severirty;
                //values.Table_ReportingDate = b.Table_ReportingDate;
                values.Table_DevStatus = b.Table_DevStatus;
                values.Table_QAStatus = b.Table_QAStatus;
                c.SaveChanges();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("ViewBugReport?brID="+selectedbugreportid);
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           View Bug Report Page.


        public static int selectedbugreportid;
        
        public ActionResult ViewBugReport(int brID)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var br = c.BugReports.Single(u => u.ID.Equals(brID));
                selectedbugreportid = br.ID;
                ViewBag.BRId = br.ID;
                ViewBag.BugreportID = br.Bug_Report_ID;
                ViewBag.ProjectID = br.ProjectID;
                ViewBag.ProjectName = br.ProjectName;
                ViewBag.FormId = br.FormId;
                ViewBag.Version = br.Version_Release;
                ViewBag.TestingType = br.TestingType;
                ViewBag.TesterName = br.TesterName;
                ViewBag.TestingCycle = br.TestingCycle;
                ViewBag.Reviewer = br.Reviewer;
                string newWord = brID.ToString();
                
                if (urole.Contains("QA"))
                {
                    ViewBag.Access = "granted";
                }
                else
                {
                    ViewBag.Access = "";
                }
                ViewBag.ReportTableName = c.BugReportTables.Where(u=>u.BugReportId == newWord).ToList();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("Error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           Logout user Page.


        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                          Dashboard Page.

        
        public ActionResult Dashboard()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                //if (User.Identity.IsAuthenticated)
                //{
                    ViewBag.loginlast = loginlast;
                    c = new DatabaseZTBLEntities();
                    ViewBag.FormName = c.FormDatas.Where(x => x.FormHolder.Equals(upp)).ToList();
                    ViewBag.FormsCount = c.FormDatas.Where(x => x.FormHolder.Equals(upp)).Count();
                    
                    return View();
                //}
            }
            catch (Exception e)
            {
                ViewBag.error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return Redirect("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("Login");
        }

        
        //<--------------------------------------                       ------------------------------------------->
        //                                           Create New Version Release Form Page.

        //[HttpPost]
        //
        //public ActionResult CreateNewForm(Models.Form f)
        //{
        //    ViewBag.Username = uname;
        //    ViewBag.UserType = utype;
        //    ViewBag.UserRole = urole;
        //    ViewBag.UserType = utype;
        //    ViewBag.upp = upp;
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            c = new DatabaseZTBLEntities();
        //            var newForm = c.Forms.Create();
        //            newForm.ProjectName = f.ProjectName;
        //            newForm.VersionDate = f.VersionDate;
        //            newForm.FormHolder = upp;
        //            newForm.VersionDescription = f.VersionDescription;
        //            newForm.Type = f.Type;
        //            newForm.VersionNo = f.VersionNo;
        //            c.Forms.Add(newForm);

        //            c.SaveChanges();

        //            var FormDataRec = c.FormDatas.Create();
        //            var id = c.Forms.ToList();

        //            FormDataRec.FormId = id.Last().FormId;

        //            FormDataRec.C_CCA_ChecklistChecked_QA = false;
        //            FormDataRec.C_CCA_ChecklistChecked_SDD = false;
        //            FormDataRec.C_CCA_ChecklistConfirmed_Deployment = false;
        //            FormDataRec.C_CCA_DateAndTime_Deployment = null;
        //            FormDataRec.C_CCA_DateAndTime_QA = null;
        //            FormDataRec.C_CCA_DateAndTime_SDD = null;
        //            FormDataRec.C_CCA_RelFilesMovedOrNot_QA = false;
        //            FormDataRec.C_CCA_RelFilesMovedOrNot_SDD = false;
        //            FormDataRec.C_CCA_RelFilesMovedToLoc_QA = "";
        //            FormDataRec.C_CCA_RelFilesMovedToLoc_SDD = "";
        //            FormDataRec.C_CCA_Sign_Deployment = false;
        //            FormDataRec.C_CCA_Sign_QA = false;
        //            FormDataRec.C_CCA_Sign_SDD = false;
        //            FormDataRec.CodingStdFollowed = false;
        //            FormDataRec.CpybaselineProvided = false;
        //            FormDataRec.DateAndTime_Deployment = null;
        //            FormDataRec.DateAndTime_QA = null;
        //            FormDataRec.DateAndTime_SDD = null;
        //            FormDataRec.DepartmentHead_Deployment_Name = "";
        //            FormDataRec.DepartmentHead_Deployment_Sign = false;
        //            FormDataRec.DepartmentHeadName_SDD = "";
        //            FormDataRec.DepartmentHeadSign_SDD = false;
        //            FormDataRec.DeployedByName_Deployment = "";
        //            FormDataRec.DeployedBySign_Deployment = false;
        //            FormDataRec.DiscoveredBugsReported = false;
        //            FormDataRec.DiscoveredBugsReportedRepNo = "";
        //            FormDataRec.FailoverSite_Deployment = "No";
        //            FormDataRec.FilesCopiedToDestination_Deployment = false;
        //            FormDataRec.FilesExecuted_Deployment = false;
        //            FormDataRec.LeadDeveloperName_SDD = "";
        //            FormDataRec.LeadDeveloperSign_SDD = false;
        //            FormDataRec.LeadName_QA = "";
        //            FormDataRec.LeadSign_QA = false;
        //            FormDataRec.ManagerName_QA = "";
        //            FormDataRec.ManagerSign_QA = false;
        //            FormDataRec.PeerReview = false;
        //            FormDataRec.PeerReviewValues = "";
        //            FormDataRec.Production_Deployment = "No";
        //            FormDataRec.QATeamSatisfied = false;
        //            FormDataRec.RelFilesLocation = "";
        //            FormDataRec.RelFilesPlaced = false;
        //            FormDataRec.RelFoldersLabeled = false;
        //            FormDataRec.SFLandRFLisAttached = false;
        //            FormDataRec.SWStabilityComment_QA = "";
        //            FormDataRec.UnitInchargeName_Deployment = "";
        //            FormDataRec.UnitInchargeName_SDD = "";
        //            FormDataRec.UnitInchargeSign_Deployment = false;
        //            FormDataRec.UnitInchargeSign_SDD = false;
        //            FormDataRec.UnitTestingDone = false;
        //            FormDataRec.VSSLabel = "";

        //            c.FormDatas.Add(FormDataRec);

        //            c.SaveChanges();

        //            return RedirectToAction("Dashboard", "Home");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "Data is incorrect!");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ViewBag.Error = e.Message;
        //        return View("error");
        //    }
        //    finally
        //    {
        //        c.Database.Connection.Close();
        //    }
        //    return View();
        //}

        [HttpPost]
        
        public ActionResult CreateNewVersionRelease(Models.FormData form)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            ViewBag.upp = upp;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (ModelState.IsValid)
                {
                    c = new DatabaseZTBLEntities();
                    var FormDataRec = c.FormDatas.Create();
                    //var newForm = c.FormDatas.Create();
                    FormDataRec.ProjectId = form.ProjectId;
                    int gg = int.Parse(form.ProjectId);
                    var gh = c.Projects.Single(h=>h.Id.Equals(gg));

                    FormDataRec.ProjectName = gh.ProjectName;
                    FormDataRec.VersionDate = form.VersionDate;
                    FormDataRec.FormHolder = upp;
                    
                    FormDataRec.VersionDescription = form.VersionDescription;
                    FormDataRec.Type = form.Type;
                    FormDataRec.VersionNo = form.VersionNo;
                    //c.Forms.Add(newForm);

                    //c.SaveChanges();
                    ViewBag.Username = uname;
                    
                    var id = c.FormDatas.ToList();

                    //FormDataRec.FormId = id.Last().FormId;

                    if (urole.Contains("SDD"))
                {
                    FormDataRec.DateAndTime_SDD = form.DateAndTime_SDD;
                    FormDataRec.DepartmentHeadName_SDD = form.DepartmentHeadName_SDD;
                    FormDataRec.DepartmentHeadSign_SDD = form.DepartmentHeadSign_SDD;
                    FormDataRec.LeadDeveloperName_SDD = form.LeadDeveloperName_SDD;
                    FormDataRec.LeadDeveloperSign_SDD = form.LeadDeveloperSign_SDD;
                    FormDataRec.UnitInchargeName_SDD = form.UnitInchargeName_SDD;
                    FormDataRec.UnitInchargeSign_SDD = form.UnitInchargeSign_SDD;
                    FormDataRec.CodingStdFollowed = form.CodingStdFollowed;
                    FormDataRec.CpybaselineProvided = form.CpybaselineProvided;
                    FormDataRec.PeerReview = form.PeerReview;
                    FormDataRec.PeerReviewValues = form.PeerReviewValues;
                    FormDataRec.RelFilesLocation = form.RelFilesLocation;
                    FormDataRec.RelFilesPlaced = form.RelFilesPlaced;
                    FormDataRec.RelFoldersLabeled = form.RelFoldersLabeled;
                    FormDataRec.SFLandRFLisAttached = form.SFLandRFLisAttached;
                    FormDataRec.UnitTestingDone = form.UnitTestingDone;
                    FormDataRec.VSSLabel = form.VSSLabel;
                
                }
                else if (urole.Contains("QA"))
                {
                    FormDataRec.DiscoveredBugsReported = form.DiscoveredBugsReported;
                    FormDataRec.DiscoveredBugsReportedRepNo = form.DiscoveredBugsReportedRepNo;
                    FormDataRec.QATeamSatisfied = form.QATeamSatisfied;
                    FormDataRec.LeadName_QA = form.LeadName_QA;
                    FormDataRec.LeadSign_QA = form.LeadSign_QA;
                    FormDataRec.ManagerName_QA = form.ManagerName_QA;
                    FormDataRec.ManagerSign_QA = form.ManagerSign_QA;
                    FormDataRec.DateAndTime_QA = form.DateAndTime_QA;
                    FormDataRec.SWStabilityComment_QA = form.SWStabilityComment_QA;

                    FormDataRec.C_CCA_ChecklistChecked_QA = form.C_CCA_ChecklistChecked_QA;
                    FormDataRec.C_CCA_ChecklistChecked_SDD = form.C_CCA_ChecklistChecked_SDD;
                    FormDataRec.C_CCA_ChecklistConfirmed_Deployment = form.C_CCA_ChecklistConfirmed_Deployment;
                    FormDataRec.C_CCA_DateAndTime_Deployment = form.C_CCA_DateAndTime_Deployment;
                    FormDataRec.C_CCA_DateAndTime_QA = form.C_CCA_DateAndTime_QA;
                    FormDataRec.C_CCA_DateAndTime_SDD = form.C_CCA_DateAndTime_SDD;
                    FormDataRec.C_CCA_RelFilesMovedOrNot_QA = form.C_CCA_RelFilesMovedOrNot_QA;
                    FormDataRec.C_CCA_RelFilesMovedOrNot_SDD = form.C_CCA_RelFilesMovedOrNot_SDD;
                    FormDataRec.C_CCA_RelFilesMovedToLoc_QA = form.C_CCA_RelFilesMovedToLoc_QA;
                    FormDataRec.C_CCA_RelFilesMovedToLoc_SDD = form.C_CCA_RelFilesMovedToLoc_SDD;
                    FormDataRec.C_CCA_Sign_Deployment = form.C_CCA_Sign_Deployment;
                    FormDataRec.C_CCA_Sign_QA = form.C_CCA_Sign_QA;
                    FormDataRec.C_CCA_Sign_SDD = form.C_CCA_Sign_SDD;
                }
                else if (urole.Contains("Operations"))
                {
                    FormDataRec.DeployedByName_Deployment = form.DeployedByName_Deployment;
                    FormDataRec.DeployedBySign_Deployment = form.DeployedBySign_Deployment;
                    FormDataRec.DepartmentHead_Deployment_Name = form.DepartmentHead_Deployment_Name;
                    FormDataRec.DepartmentHead_Deployment_Sign = form.DepartmentHead_Deployment_Sign;
                    FormDataRec.FailoverSite_Deployment = form.FailoverSite_Deployment;
                    FormDataRec.FilesCopiedToDestination_Deployment = form.FilesCopiedToDestination_Deployment;
                    FormDataRec.FilesExecuted_Deployment = form.FilesExecuted_Deployment;
                    FormDataRec.Production_Deployment = form.Production_Deployment;
                    FormDataRec.DateAndTime_Deployment = form.DateAndTime_Deployment;
                    FormDataRec.UnitInchargeName_Deployment = form.UnitInchargeName_Deployment;
                    FormDataRec.UnitInchargeSign_Deployment = form.UnitInchargeSign_Deployment;
                }

                    c.FormDatas.Add(FormDataRec);
                    c.SaveChanges();

                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Data is incorrect!");
                }
            }
            //catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            //{
            //    Exception raise = dbEx;
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            string message = string.Format("{0}:{1}",
            //                validationErrors.Entry.Entity.ToString(),
            //                validationError.ErrorMessage);
            //            // raise a new exception nesting
            //            // the current instance as InnerException
            //            raise = new InvalidOperationException(message, raise);
            //        }
            //    }
            //    throw raise;
            //}
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return View();
        }

        //<--------------------------------------                                                    ------------------------------------------->
        //                                           Released Files list in Version Release Form Page.
        //
        //public ActionResult ReleasedFilesPage()
        //{
        //    ViewBag.Username = uname;
        //    ViewBag.UserType = utype;
        //    ViewBag.UserRole = urole;
        //    ViewBag.UserType = utype;
        //    try
        //    {
        //        c = new DatabaseZTBLEntities();

        //        ViewBag.ProjectName = c.FormDatas.Single(u => u.FormId.Equals(formid)).ProjectName;
        //        ViewBag.VersionNumber = c.FormDatas.Single(u => u.FormId.Equals(formid)).VersionNo;
        //        var userrole = c.Users.Single(u => u.PPNumber.Equals(upp)).UserRole;
        //        int xx = c.ReleasedFilesLists.AsNoTracking().Where(u => u.FormId.Equals(formid)).Count();
        //        ViewBag.filesCount = xx;
        //        ViewBag.ReleasedFilesName = c.ReleasedFilesLists.AsNoTracking().Where(u => u.FormId.Equals(formid)).ToList();
        //        if (userrole.Contains("Manager(SDD)") || userrole.Contains("Team Lead(SDD)") || userrole.Contains("Lead Developer"))
        //        {
        //            ViewBag.Access = "allowed";
        //        }
        //        else
        //        {
        //            ViewBag.Access = "not";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ViewBag.Error = e.Message;
        //        ViewBag.Username = uname;
        //        ViewBag.UserType = utype;
        //        ViewBag.UserRole = urole;
        //        ViewBag.UserType = utype;
        //        return View("error");
        //    }
        //    finally
        //    {
        //        c.Database.Connection.Close();  

        //    }
        //    return View();
        //}

        //<--------------------------------------                       ------------------------------------------->
        //                                           Software Features list attache to Version Release Form Page.
        
        //public ActionResult FeaturesListPage()
        //{

        //    ViewBag.Username = uname;
        //    ViewBag.UserType = utype;
        //    ViewBag.UserRole = urole;
        //    ViewBag.UserType = utype;
        //    c= new DatabaseZTBLEntities();

        //    ViewBag.ProjectName = c.FormDatas.Single(u => u.FormId.Equals(formid)).ProjectName;
        //    ViewBag.VersionNumber = c.FormDatas.Single(u => u.FormId.Equals(formid)).VersionNo;
        //    var userrole = c.Users.Single(u => u.PPNumber.Equals(upp)).UserRole;
        //    if (userrole.Contains("Manager(SDD)") || userrole.Contains("Team Lead(SDD)") || userrole.Contains("Lead Developer"))
        //    {
        //        ViewBag.addAccess = "allowed";
        //    }
        //    else
        //    {
        //        ViewBag.addAccess = "not";
        //    }
        //    ViewBag.FeaturesName = c.SoftwareFeatureLists.AsNoTracking().Where(u=> u.FormId.Equals(formid)).ToList();            
            
        //    c.Database.Connection.Close();

        //    return View();
        //}

        //<--------------------------------------   Add Release Files --------------------------------------------->
        
        public ActionResult AddReleasedFiles()
        {

            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View();
        }

        [HttpPost]
        
        public ActionResult AddReleasedFiles(Models.ReleasedFilesList rfl)
        {

            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                rfl.FormId = formid;
                int identity = c.ReleasedFilesLists.Where(u => u.FormId.Equals(formid)).Count() + 1;
                rfl.S_No_ = identity;
                
                c.ReleasedFilesLists.Add(rfl);

                c.SaveChanges();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message; ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }

            return Redirect("VersionReleaseForm?formId="+formid);
        }

        //<--------------------------------------                                           ------------------------------------------->
        //                                           Delete Feature from Software Feature List Page.
        
        public ActionResult DeleteReleasedFile(int deleteId)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var x = c.ReleasedFilesLists.Single(u => u.ID.Equals(deleteId));

                if (x != null)
                {
                    int xxxx = c.ReleasedFilesLists.Count();
                    //int toBeRemoved = x.S_No_;
                    c.ReleasedFilesLists.Remove(x);
                    c.SaveChanges();
                    //var cc = c.SoftwareFeatureLists.Where(u => u.FormId.Equals(formid));


                    //for (; toBeRemoved <= xxxx; toBeRemoved++)
                    //{

                    //}

                }
                return Redirect("VersionReleaseForm?formid="+formid);
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message; 
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }


        public ActionResult getProjectInfo(String Name)
        {
            c = new DatabaseZTBLEntities();

            var dd = c.Projects.SingleOrDefault(u => u.ProjectName == Name);


            return Json(new { version = dd.Version, description = dd.ProjectDescription});
        }


        //<--------------------------------------                       ------------------------------------------->
        //                                           Add features to the Software Features list Page.

        
        public ActionResult AddFeature()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View();
        }


        [HttpPost]
        
        public ActionResult AddFeature(Models.SoftwareFeatureList s)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                s.FormId = formid;
                int identity = c.SoftwareFeatureLists.Where(u=>u.FormId.Equals(formid)).Count() +1;
                s.S_No_ = identity;
                c.SoftwareFeatureLists.Add(s);

                c.SaveChanges();
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("VersionReleaseForm?formId="+formid);
        }

            //<--------------------------------------                                           ------------------------------------------->
            //                                           Delete Feature from Software Feature List Page.
            
            public ActionResult DeleteFeature(int deleteId)
            {
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                if (ViewBag.Username == null)
                {
                    return Redirect("Logout");
                }
                try
                {
                    c = new DatabaseZTBLEntities();
                    var x = c.SoftwareFeatureLists.Single(u => u.ID.Equals(deleteId));

                    if (x != null)
                    {
                        int xxxx = c.SoftwareFeatureLists.Count();
                        //int toBeRemoved = x.S_No_;
                        c.SoftwareFeatureLists.Remove(x);
                        c.SaveChanges();
                        //var cc = c.SoftwareFeatureLists.Where(u => u.FormId.Equals(formid));


                        //for (; toBeRemoved <= xxxx; toBeRemoved++)
                        //{

                        //}

                    }
                    return Redirect("VersionReleaseForm?formId="+formid);
                }
                catch (Exception e)
                {
                    ViewBag.Error = e.Message;
                    ViewBag.Username = uname;
                    ViewBag.UserType = utype;
                    ViewBag.UserRole = urole;
                    ViewBag.UserType = utype;
                    return View("error");
                }
                finally
                {
                    c.Database.Connection.Close();
                }
            }

        //<--------------------------------------                       ------------------------------------------->
        //                                           Create new Version Release Form Page.


        [HttpGet]
        
        public ActionResult CreateNewForm()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            ViewBag.upp = upp;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View(); 
        }

        [HttpGet]
        
        public ActionResult CreateNewVersionRelease()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            ViewBag.upp = upp;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            c = new DatabaseZTBLEntities();
            var x = c.Projects.ToList();
            
            List<String> y = new List<String>();
            foreach (Project j in x)
            {
                y.Add(j.ProjectName);
            }
            
            ViewBag.Forms = c.Projects.ToList().Select(hh => new System.Web.Mvc.SelectListItem { Text = hh.ProjectName, Value = hh.Id.ToString()});

            /*List<String> y = new List<String>();
            foreach (Project j in x)
            {
                y.Add(j.ProjectName);
            }*/
            ViewBag.ProjectsAvailable = y;
            ViewBag.ProjectsIdMatching = x;
            return View();
        }


        //<--------------------------------------                       ------------------------------------------->
        //                                           Admin Panel page Page.
        
        public ActionResult AdminPanel(int? pp)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                //if (User.Identity.IsAuthenticated)
                //{
                    if (utype != "Admin")
                    {
                        return Redirect("Dashboard");
                    }
                    ViewBag.Operation = pp;
                    c = new DatabaseZTBLEntities();
                    ViewBag.Name = c.Users.ToList();
                    c.Database.Connection.Close();
                    return View();
                //}
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("Login");
        }

        //<--------------------------------------                                        ------------------------------------------->
        //                                           Method to validate username and password


        private bool IsValid(string ppnumber, string password)
        {

            bool IsValid = false;

            c = new DatabaseZTBLEntities();
            var user = c.Users.FirstOrDefault(u => u.PPNumber.Equals(ppnumber));
            if (user != null)
            {
                if (user.Password == password)
                {
                    IsValid = true;
                }
            }

            c.Database.Connection.Dispose();
            return IsValid;
        }
        public static string epp, eemail, ename, epass, etype, edesig, erole;


        //<--------------------------------------                                                ------------------------------------------->
        //                                           Method to fetch info of user into Edit User Page.


        public ActionResult fetchInfo(int? pp)
        {
            try
            {
                User g=null;
                if (pp != null)
                {
                    String p = pp.ToString();

                    c = new DatabaseZTBLEntities();
                    g = c.Users.Single(u => u.PPNumber == p);

                    epp = g.PPNumber;
                    ename = g.Name;
                    eemail = g.Email;
                    epass = g.Password;
                    etype = g.UserType;
                    edesig = g.Designation;
                    erole = g.UserRole;
                }
                c.Database.Connection.Close();
                return Redirect("Edit");
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                c.Database.Connection.Close();
                return View("error");
            }
        }

        //<--------------------------------------                       ------------------------------------------->
        //                                           Edit User page Page.

        
        
        public ActionResult Edit(Models.User user)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                if (user.PPNumber != null)
                {

                    if (ModelState.IsValid)
                    {

                        c = new DatabaseZTBLEntities();
                        User x = c.Users.Single(h => h.PPNumber == epp);
                        c.Users.Remove(x);

                        var newUser = c.Users.Create();
                        newUser.Name = user.Name;
                        newUser.PPNumber = user.PPNumber;
                        newUser.Email = user.Email;
                        newUser.Password = user.Password;

                        newUser.UserType = user.UserType;
                        if (user.Designation == null)
                            newUser.Designation = edesig;
                        else
                            newUser.Designation = user.Designation;
                        if (user.UserRole == null)
                            newUser.UserRole = erole;
                        else
                            newUser.UserRole = user.UserRole;



                        c.Users.Add(newUser);
                        c.SaveChanges();
                        if (upp == x.PPNumber)
                        {
                            c.Database.Connection.Close();
                            return Redirect("Logout");
                        }
                        return Redirect("AdminPanel");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Data is incorrect!");
                        int xx = int.Parse(user.PPNumber);
                        fetchInfo(xx);
                    }
                }
                else
                {
                    ViewBag.EditPP = epp;
                    ViewBag.EditName = ename;
                    ViewBag.EditPass = epass;
                    ViewBag.etype = etype;
                    ViewBag.edesig = edesig;
                    ViewBag.erole = erole;
                    ViewBag.EditEmail = eemail;
                    var f = c.Users.ToList().Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
                    ViewBag.Users = f;
                    List<String> ty = new List<String>();
                    ty.Add("User");
                    ty.Add("Admin");

                    List<String> desigList = new List<String>();
                    desigList.Add("OG-III");
                    desigList.Add("OG-II");
                    desigList.Add("OG-I");
                    desigList.Add("AVP");
                    desigList.Add("VP");
                    desigList.Add("SVP");
                    desigList.Add("EVP");

                    ViewBag.Designation = desigList;
                    ViewBag.EditType = ty;

                    return View();
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    System.Diagnostics.Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
            //catch (Exception e)
            //{
            //    if (e.Message.Contains("An error occurred while updating the entries. See the inner exception for details"))
            //    {
            //        ViewBag.Error = "Cannot update, Record already exists";
            //    }
            //    else
            //    {
            //        ViewBag.Error = e.Message;
            //    }
            //    ViewBag.Username = uname;
            //    ViewBag.UserType = utype;
            //    ViewBag.UserRole = urole;
            //    ViewBag.UserType = utype;
            //    if (c != null)
            //    return View("error");
            //}
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("AdminPanel");
        }

        [HttpGet]
        public ActionResult Edited()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            return View();
        }

        [HttpPost]
        public ActionResult Edited(Models.User Userr)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            User u = c.Users.Single(v => v.PPNumber == Userr.PPNumber);
            if (u != null)
            {
                u.Name = Userr.Name;
                u.Password = Userr.Password;
                u.PPNumber = Userr.PPNumber;
            }
            c.SaveChanges();
            c.Database.Connection.Close();

            return View();
        }

        [HttpPost]
        public ActionResult Edited(String name, String type, String pp, String password)
        {
            c = new DatabaseZTBLEntities();
            selectedUser = c.Users.Single(v => v.PPNumber == pp.ToString());
            User newuser = null;
            if (selectedUser != null)
            {
                newuser.Name = name.ToString();
                newuser.Password = password.ToString();
                newuser.PPNumber = pp.ToString();
                newuser.UserType = type.ToString();

                c.Users.Remove(selectedUser);
                c.Users.Add(newuser);
                c.SaveChanges();
                c.Database.Connection.Close();
            }

            return View();
        }

        public ActionResult DeleteProject(int prjid)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                String p;
                Project objProject = null;
                p = prjid.ToString();
                c = new DatabaseZTBLEntities();

                objProject = c.Projects.Single(u => u.Id.Equals(prjid));
                if (objProject != null)
                {
                    c.Projects.Remove(objProject);
                    c.SaveChanges();
                }
                return RedirectToAction("ManageProject");
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
        }

        public static int prjEditId;
        public ActionResult EditProject(int prjid)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            
            try
            {
                c = new DatabaseZTBLEntities();
                Project x = c.Projects.Single(u => u.Id.Equals(prjid));
                prjEditId = x.Id;
                ViewBag.prjname = x.ProjectName;
                 string SelectedManagerSDD = (c.Projects.Single(h => h.Id.Equals(prjid))).Manager_SDD;
                 string SelectedManagerQA = (c.Projects.Single(h => h.Id.Equals(prjid))).Manager_QA;
                 string SelectedTeamLeadSDD = (c.Projects.Single(h => h.Id.Equals(prjid))).UnitIncharge_SDD;
                string SelectedManagerOperations = (c.Projects.Single(h => h.Id.Equals(prjid))).Manager_Operations;
                string SelectedTeamLeadQA = (c.Projects.Single(h => h.Id.Equals(prjid))).UnitIncharge_QA;
                string SelectedTeamLeadOperations = (c.Projects.Single(h => h.Id.Equals(prjid))).UnitIncharge_Operations_;
                string SelectedLeadDeveloperSDD = (c.Projects.Single(h => h.Id.Equals(prjid))).LeadDeveloper_SDD;

                ViewBag.ManagerSDD = c.Users.Where(hh => hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedManagerSDD });
                ViewBag.TeamLeadSDD = c.Users.Where(hh => hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedTeamLeadSDD});
                ViewBag.LeadDeveloperSDD = c.Users.Where(hh => hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Developer")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedLeadDeveloperSDD });
                ViewBag.ManagerQA = c.Users.Where(hh => hh.UserRole.Contains("Manager") && hh.UserRole.Contains("QA")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedManagerQA });
                ViewBag.TeamLeadQA = c.Users.Where(hh => hh.UserRole.Contains("QA") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedTeamLeadQA });
                ViewBag.ManagerOperations = c.Users.Where(hh => hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedManagerOperations });
                ViewBag.TeamLeadOperations = c.Users.Where(hh => hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber, Selected = hh.PPNumber == SelectedTeamLeadOperations });

                c.Database.Connection.Close();

            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            
            return View();
        }

        [HttpPost]
        public ActionResult EditProject(Project pro)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {

           
            c = new DatabaseZTBLEntities();
            var newProject = c.Projects.Single(hh=>hh.Id.Equals(prjEditId));
            newProject.ProjectName = pro.ProjectName;
            newProject.Manager_SDD = pro.Manager_SDD;
            newProject.Manager_QA = pro.Manager_QA;
            newProject.Manager_Operations = pro.Manager_Operations;
            newProject.UnitIncharge_SDD = pro.UnitIncharge_SDD;
            newProject.UnitIncharge_QA = pro.UnitIncharge_QA;
            newProject.LeadDeveloper_SDD = pro.LeadDeveloper_SDD;
            newProject.UnitIncharge_Operations_ = pro.UnitIncharge_Operations_;
            
            c.SaveChanges();
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }

            return RedirectToAction("ManageProject");
        }

        public ActionResult ManageProject()
        {
            c = new DatabaseZTBLEntities();
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            ViewBag.Name = c.Projects.ToList();
            c.Database.Connection.Close();
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            return View();
        }

        public ActionResult AddProject()
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            c = new DatabaseZTBLEntities();
            ViewBag.ManagerSDD = c.Users.Where(hh => hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            ViewBag.TeamLeadSDD = c.Users.Where(hh =>  hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            ViewBag.LeadDeveloperSDD = c.Users.Where(hh=> hh.UserRole.Contains("SDD") & hh.UserRole.Contains("Developer")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber  });
            ViewBag.ManagerQA = c.Users.Where(hh=> hh.UserRole.Contains("Manager") && hh.UserRole.Contains("QA")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            ViewBag.TeamLeadQA =c.Users.Where(hh => hh.UserRole.Contains("QA") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            ViewBag.ManagerOperations = c.Users.Where(hh =>  hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Manager")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });
            ViewBag.TeamLeadOperations = c.Users.Where(hh =>  hh.UserRole.Contains("Operations") & hh.UserRole.Contains("Team")).Select(hh => new System.Web.Mvc.SelectListItem { Text = "PP: " + hh.PPNumber + " [" + hh.Name + ", " + hh.UserRole + "]", Value = hh.PPNumber });

            c.Database.Connection.Close();
            return View();
        }

        [HttpPost]
        public ActionResult AddProject(Models.Project pro)
        {
            ViewBag.Username = uname;
            ViewBag.UserType = utype;
            ViewBag.UserRole = urole;
            ViewBag.UserType = utype;
            if (ViewBag.Username == null)
            {
                return Redirect("Logout");
            }
            try
            {
                c = new DatabaseZTBLEntities();
                var newProject = c.Projects.Create();
                newProject.ProjectName = pro.ProjectName;
                newProject.Manager_SDD = pro.Manager_SDD;
                newProject.Manager_QA = pro.Manager_QA;
                newProject.Manager_Operations = pro.Manager_Operations;
                newProject.UnitIncharge_SDD = pro.UnitIncharge_SDD;
                newProject.UnitIncharge_QA = pro.UnitIncharge_QA;
                newProject.LeadDeveloper_SDD = pro.LeadDeveloper_SDD;
                newProject.UnitIncharge_Operations_ = pro.UnitIncharge_Operations_;
                c.Projects.Add(newProject);
                c.SaveChanges();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                ViewBag.Username = uname;
                ViewBag.UserType = utype;
                ViewBag.UserRole = urole;
                ViewBag.UserType = utype;
                return View("error");
            }
            finally
            {
                c.Database.Connection.Close();
            }
            return Redirect("ManageProject");
        }

        //public void GetPdf()
        //{
        //    //SelectPdf.PdfDocument doc = new SelectPdf.PdfDocument();
        //    //SelectPdf.PdfPage page = doc.AddPage();

        //    //SelectPdf.PdfFont font = doc.AddFont(SelectPdf.PdfStandardFont.Helvetica);
        //    //font.Size = 20;

        //    //HtmlToPdf d = new HtmlToPdf();


        //    ////SelectPdf.PdfTextElement text = new SelectPdf.PdfTextElement(50, 50, "Hello world!", font);
        //    ////page.Add(text);

        //    //doc.Save("test.pdf");
        //    //doc.Close();
        //    HtmlToPdf converter = new HtmlToPdf();
    
        //    PdfDocument doc = converter.ConvertUrl("localhost:4372\\Home\\VersionReleaseForm?formid="+formid);

        //    doc.Save("test.pdf");
        //    doc.Close();

            
        //}

        public IView objUser { get; set; }
    }
}


//<--------------------------------------                       ------------------------------------------->
//                                           Class for sending Email.


public class GMailer
{
    public static string GmailUsername { get; set; }
    public static string GmailPassword { get; set; }
    public static string GmailHost { get; set; }
    public static int GmailPort { get; set; }
    public static bool GmailSSL { get; set; }

    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }

    static GMailer()
    {
        GmailHost = "smtp.gmail.com";
        GmailPort = 25; // Gmail can use ports 25, 465 & 587; but must be 25 for medium trust environment.
        GmailSSL = true;
    }

    public void Send()
    {
        SmtpClient smtp = new SmtpClient();
		smtp.Host = GmailHost;
        smtp.Port = GmailPort;
        smtp.EnableSsl = GmailSSL;
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtp.UseDefaultCredentials = false;
        smtp.Credentials = new NetworkCredential(GmailUsername, GmailPassword);

        using (var message = new MailMessage(GmailUsername, ToEmail))
        {
            message.Subject = Subject;
            message.Body = Body;
            message.IsBodyHtml = IsHtml;
            smtp.Send(message);
        }
    }

    }

