﻿using ASPSnippets.SmsAPI;
using MVCTutorial.Business.Interface;
using MVCTutorial.Domain;
using MVCTutorial.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace MVCTutorial.Controllers
{

    public class TestController : Controller
    {
        IEmployeeBusiness empBusiness;
        IDepartmentBusiness departmentBusiness;
        
        public TestController(IEmployeeBusiness _empBusiness, IDepartmentBusiness _departmentBusiness)
        {
            empBusiness = _empBusiness;
            departmentBusiness = _departmentBusiness;
        }

        public ActionResult Index()
        {
            //Used to bind department dropdown
            List<DepartmentDomainModel> list = departmentBusiness.GetAllDepartment();
            ViewBag.DepartmentList = new SelectList(list, "DepartmentId", "DepartmentName");
            
            return View();
        }

        /// <summary>
        /// Get employee records, it includes search functionality
        /// </summary>
        /// <param name="param"></param>
        /// <param name="EName"></param>
        /// <returns></returns>
        public JsonResult GetEmployeeRecord(DataTablesParam param, string EName)
        {

            List<EmployeeDomainModel> List = new List<EmployeeDomainModel>();

            int pageNo = 1;

            if (param.iDisplayStart >= param.iDisplayLength)
            {
                pageNo = (param.iDisplayStart / param.iDisplayLength) + 1;

            }

            int totalCount = 0; //total records in table 

            //Search by department,employeeName,and Country
            if (param.sSearch != null)
            {
                totalCount = empBusiness.TotalEmployeeCount(param.sSearch);

                List = empBusiness.SearchEmployee(param.sSearch, pageNo, param.iDisplayLength);
            }

            //Datatable Exrernal Search (by employeeName)
            else if (EName != "")
            {
                totalCount = empBusiness.TotalEmployeeCountByEmployeeName(EName);

                List = empBusiness.SearchEmployeeByEmployeeName(EName, pageNo, param.iDisplayLength);

            }
            //Get All Employee without any search params
            else
            {
                totalCount = empBusiness.TotalEmployeeRecord();

                List = empBusiness.GetEmployeeRecords(pageNo, param.iDisplayLength);
            }

            List<EmployeeViewModel> empVMList = new List<EmployeeViewModel>();

            AutoMapper.Mapper.Map(List, empVMList);

            return Json(new
            {
                aaData = empVMList,
                sEcho = param.sEcho,
                iTotalDisplayRecords = totalCount,
                iTotalRecords = totalCount

            }
                , JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Add Update employee based on employeeId
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Index(EmployeeViewModel model)
        {
            try
            {
                List<DepartmentDomainModel> list = departmentBusiness.GetAllDepartment();
                ViewBag.DepartmentList = new SelectList(list, "DepartmentId", "DepartmentName");

                EmployeeDomainModel empDomain = new EmployeeDomainModel();
                AutoMapper.Mapper.Map(model, empDomain);

                empDomain = empBusiness.AddEditEmployee(empDomain);

                AutoMapper.Mapper.Map(empDomain, model);

                return View(model);

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        /// <summary>
        /// Called when we click on edit button, it renders employee record by Id using Partial view
        /// </summary>
        /// <param name="EmployeeId"></param>
        /// <returns></returns>
        public ActionResult AddEditEmployee(int EmployeeId)
        {
            List<DepartmentDomainModel> list = departmentBusiness.GetAllDepartment();
            ViewBag.DepartmentList = new SelectList(list, "DepartmentId", "DepartmentName");

            EmployeeViewModel model = new EmployeeViewModel();
            EmployeeDomainModel empDomainModel = new EmployeeDomainModel();

            if (EmployeeId > 0)
            {
                empDomainModel = empBusiness.GetEmployeeById(EmployeeId);
                AutoMapper.Mapper.Map(empDomainModel, model);
            }
            return PartialView("Partial2", model);
        }

        /// <summary>
        /// Delete employee by Id (set IsDelete==false)
        /// </summary>
        /// <param name="EmployeeId"></param>
        /// <returns></returns>
        public ActionResult DeleteEmployee(int EmployeeId)
        {
            bool result = empBusiness.DeleteEmployee(EmployeeId);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Returns a piece of html using Partial view. This can be view of the left side of page
        /// </summary>
        /// <returns></returns>
        public ActionResult SideMenu()
        {
            return PartialView("SideMenu");
        }


        #region 
        //Send OTP 

        public JsonResult SendOTP()
        {
            int otpValue = new Random().Next(100000, 999999);
            var status = "";
            try
            {
                string recipient = ConfigurationManager.AppSettings["RecipientNumber"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();

                string message = "Your OTP Number is " + otpValue + " ( Sent By : Technotips-Ashish )";
                String encodedMessage = HttpUtility.UrlEncode(message);

                using (var webClient = new WebClient())
                {
                    byte[] response = webClient.UploadValues("https://api.textlocal.in/send/", new NameValueCollection(){
                                        
                                         {"apikey" , APIKey},
                                         {"numbers" , recipient},
                                         {"message" , encodedMessage},
                                         {"sender" , "TXTLCL"}});

                    string result = System.Text.Encoding.UTF8.GetString(response);

                    var jsonObject = JObject.Parse(result);

                    status = jsonObject["status"].ToString();

                    Session["CurrentOTP"] = otpValue;
                }


                return Json(status, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {

                throw (e);

            }

        }
        public ActionResult EnterOTP()
        {
            return View();
        }
        [HttpPost]
        public JsonResult VerifyOTP(string otp)
        {
            bool result = false;

            string sessionOTP = Session["CurrentOTP"].ToString();

            if (otp == sessionOTP)
            {
                result = true;

            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
