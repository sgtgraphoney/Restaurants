using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Restaurants.Models;

namespace Restaurants.Controllers
{
    /**
     * This controller is responsible for handling requests that change information about cooks.
     * It returns the only page with all necessary information.
     * Updating and deleting requests are sent with ajax.
     * All information about employees is taken from a connected database.
     * All changes are written to database after validation.
     */
    public class EmployeesController : Controller
    {

        private EmployeeContext db = new EmployeeContext();


        /**
         * Returns a page with a list of cooks. 
         * On this page you can edit information about the cooks.
         */
        [HttpGet]
        public ActionResult EmployeeEditor()
        {
            ViewBag.Employees = db.Employees.Include("Attestations").ToList();
            ViewBag.Attestations = db.Attestations.ToList();
            return View();
        }


        /**
         * This method is used for ajax updating requests.
         * It updates information about cocks.
         */
        [HttpPost]
        public JsonResult Update(Employee employee)
        { 
            if (validateData(employee))
            {
                int id = employee.Id;
                writeEmployeeToDatabase(employee);

                Employee response = readEmployeeFromDatabase(id);
                foreach (Attestation a in response.Attestations)
                {
                    a.Employees = null;
                }

                return new CorrectJsonResult()
                {
                    Data = new 
                    {
                        success = true,
                        data = response
                    }
                };
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Error: incorrect input"
                });
            }

        }


        /**
         * This method is used for ajax daleting requests.
         * It removes all information about the specified cook.
         */
        [HttpPost]
        public JsonResult Delete(int id)
        {
            Employee employee = db.Employees.First(e => e.Id == id);
            db.Employees.Remove(employee);
            db.SaveChanges();

            return Json(new
            {
                success = true
            });
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        private void writeEmployeeToDatabase(Employee employee)
        {
            var attestationList = new List<Attestation>();
            var dbAttestationList = db.Attestations.Include("Employees").ToList();
            foreach (var a in employee.Attestations)
            {
                attestationList.Add(dbAttestationList.Find(x => x.Specialization.Equals(a.Specialization)));
            }
            employee.Attestations.Clear();
            employee.Attestations.InsertRange(0, attestationList);

            if (employee.Id == -1)
            {
                db.Employees.Add(employee);
            }
            else
            {
                Employee current = db.Employees.First(e => e.Id == employee.Id);
                current.copyFrom(employee);
            }

            db.SaveChanges();
        }


        private Employee readEmployeeFromDatabase(int id)
        {
            if (id < -1 || id == 0)
            {
                return null;
            }
            else if (id == -1)
            {
                return db.Employees.ToList().Last();
            }
            else
            {
                return db.Employees.First(e => e.Id == id);
            }
        }


        private bool validateData(Employee employee)
        {
            if (employee == null || employee.FirstName == null || employee.LastName == null || employee.Session == null)
            {
                return false;
            }

            bool idOk = (employee.Id > 0 || employee.Id == -1);

            Regex nameRegex = new Regex(@"^[A-ZА-Яa-zа-яЁё]+$");
            bool nameOk = nameRegex.IsMatch(employee.LastName) && nameRegex.IsMatch(employee.FirstName) && 
                (employee.Patronymic == null || nameRegex.IsMatch(employee.Patronymic));

            employee.FirstName = setCorrectCaseForName(employee.FirstName);
            employee.LastName= setCorrectCaseForName(employee.LastName);
            if (employee.Patronymic != null)
            {
                employee.Patronymic = setCorrectCaseForName(employee.Patronymic);
            }

            Regex sessionRegex = new Regex(@"^[25]\/2$");
            Regex shiftRegex = new Regex(@"^(утренняя|вечерняя)$");
            bool workInfoOk = (employee.Shift == null || shiftRegex.IsMatch(employee.Shift)) &&
                sessionRegex.IsMatch(employee.Session) &&
                employee.AmountOfWorkingHours >= 4 && employee.AmountOfWorkingHours <= 10 &&
                employee.FirstWorkingDay.Year >= 1970 && employee.FirstWorkingDay.Year <= DateTime.Now.Year;

            return idOk && nameOk && workInfoOk;
        }


        private string setCorrectCaseForName(string name)
        {
            if (name.Length > 0)
            {
                return name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();
            }
            else
            {
                return name;
            }
        }

    }
}