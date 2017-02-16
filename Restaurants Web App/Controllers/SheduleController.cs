using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplicationMvc01.Models;

namespace WebApplicationMvc01.Controllers
{
    public class SheduleController : Controller
    {
        public static readonly Dictionary<Months, int> DaysInMonth;

        private const int HoursInShift = 7, StartTime = 10, EndTime = 24;

        static SheduleController()
        {
            DaysInMonth = new Dictionary<Months, int>()
            {
                { Months.January, 31 },
                { Months.February, 28 },
                { Months.March, 31 },
                { Months.April, 30 },
                { Months.May, 31 },
                { Months.June, 30 },
                { Months.July, 31 },
                { Months.August, 31 },
                { Months.September, 30 },
                { Months.October, 31 },
                { Months.November, 30 },
                { Months.December, 31 },
            };
        }


        private EmployeeContext db = new EmployeeContext();


        [HttpGet]
        public ActionResult SheduleEditor()
        {
            GenerateShedule(Months.February);
            return View();
        }


        private void GenerateShedule(Months month)
        {
            List<Employee> employees = db.Employees.Include("Attestations").ToList();

            for (int i = 1; i <= 2; i++)
            {
                List<Employee> workingToday = new List<Employee>(employees.FindAll(e => WorksToday(e, (int)month, i)));
                workingToday.Sort(new EmployeeComparer());

                foreach (Restaurant r in db.Restaurants)
                {
                    List<Employee> selected = new List<Employee>();
                    int totalHours = 0, j = 0;
                    bool lastSearch = false;

                    while (true)
                    {
                        if (j >= workingToday.Count)
                        {
                            break;
                        }

                        Employee current = workingToday[j];
                        if (selected.Contains(current))
                        {
                            if (lastSearch)
                            {
                                j--;
                            }
                            else
                            {
                                j++;
                            }
                            continue;
                        }

                        if (current.Shift != null)
                        {
                            if (current.Shift.Equals("утренняя") && totalHours + current.AmountOfWorkingHours > HoursInShift)
                            {
                                if (lastSearch)
                                {
                                    j--;
                                }
                                else
                                {
                                    j++;
                                }
                                continue;
                            }

                            if (current.Shift.Equals("вечерняя") && totalHours < HoursInShift)
                            {
                                if (lastSearch)
                                {
                                    j--;
                                }
                                else
                                {
                                    j++;
                                }
                                continue;
                            }
                        }

                        if (lastSearch)
                        {
                            if (totalHours + current.AmountOfWorkingHours >= HoursInShift * 2)
                            {
                                selected.Add(current);
                                break;
                            }
                            else
                            {
                                j--;
                            }
                        }

                        if (totalHours + current.AmountOfWorkingHours < HoursInShift * 2)
                        {
                            selected.Add(current);
                            totalHours += current.AmountOfWorkingHours;
                            j = 0;
                        }
                        else if (totalHours + current.AmountOfWorkingHours == HoursInShift * 2)
                        {
                            selected.Add(current);
                            totalHours += current.AmountOfWorkingHours;
                            break;
                        }
                        else
                        {
                            if (j < workingToday.Count - 1)
                            {
                                j++;
                            }
                            else
                            {
                                Employee last = selected.Last();

                                if (workingToday.IndexOf(last) < workingToday.Count - 1)
                                {
                                    totalHours -= last.AmountOfWorkingHours;
                                    selected.Remove(last);
                                    j = workingToday.IndexOf(last) + 1;
                                }                                
                                else
                                {
                                    lastSearch = true;
                                }
                                                               
                            }
                        }

                    }

                    List<Shedule> sheduleItems = new List<Shedule>();
                    int startTime = StartTime;

                    foreach (Employee e in selected)
                    {
                        workingToday.Remove(e);

                        Shedule sheduleItem = new Shedule();
                       
                        sheduleItem.RestaurantId = r.Id;
                        sheduleItem.Restaurant = r;
                        sheduleItem.EmployeeId = e.Id;
                        sheduleItem.Employee = e;
                        sheduleItem.Date = Today(e, (int)month, i);
                        
                        if (totalHours > HoursInShift * 2 && e.Equals(selected.Last()))
                        {
                            sheduleItem.From = new DateTime(2000, 1, 1, EndTime - e.AmountOfWorkingHours, 0, 0);
                            sheduleItem.To = new DateTime(2000, 1, 1, EndTime % 24, 0, 0);
                        }
                        else
                        {                            
                            sheduleItem.From = new DateTime(2000, 1, 1, startTime, 0, 0);
                            startTime += e.AmountOfWorkingHours;
                            sheduleItem.To = new DateTime(2000, 1, 1, startTime % 24, 0, 0);
                        }

                        sheduleItems.Add(sheduleItem);
                    }

                    db.Shedules.AddRange(sheduleItems);
                }
            }

            db.SaveChanges();
        }


        private bool WorksToday(Employee e, int month, int day)
        {
            int workingDaysAmount = Int32.Parse(e.Session.Substring(0, 1));
            int weekendAmount = Int32.Parse(e.Session.Substring(2, 1));

            DateTime now = Today(e, month, day);

            TimeSpan diff = now - e.FirstWorkingDay;
            return diff.TotalDays % (workingDaysAmount + weekendAmount) <= workingDaysAmount;
        }


        private DateTime Today(Employee e, int month, int day)
        {
            int year = e.FirstWorkingDay.Year;
            if ((int)month < e.FirstWorkingDay.Month)
            {
                year++;
            };
            return new DateTime(year, month, day);
        }


    }


    class EmployeeComparer : Comparer<Employee>
    {
        public override int Compare(Employee x, Employee y)
        {
            if (x.AmountOfWorkingHours > y.AmountOfWorkingHours)
            {
                return -1;
            }
            else if (x.AmountOfWorkingHours < y.AmountOfWorkingHours)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }


    public enum Months
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

}