using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplicationMvc01.Models;

namespace WebApplicationMvc01.Controllers
{
    public class ScheduleController : Controller
    {
        public static readonly Dictionary<Months, int> DaysInMonth;

        private const int HoursInShift = 7, StartTime = 10, EndTime = 24;

        static ScheduleController()
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
        public ActionResult ScheduleEditor()
        {
            GenerateSchedule(Months.February);
            return View();
        }


        private void GenerateSchedule(Months month)
        {
            List<Employee> employees = db.Employees.Include("Attestations").ToList();

            for (int i = 1; i <= 7; i++)
            {
                EmployeeList workingToday = new EmployeeList(employees.FindAll(e => WorksToday(e, (int)month, i)));
                workingToday.Sort(new EmployeeComparer());

                foreach (Restaurant r in db.Restaurants)
                {
                    List<Employee> selected = new List<Employee>();
                    int totalHours = 0;

                    Employee current = workingToday.FirstAppropriate(totalHours, selected);

                    while (true)
                    {

                        if (current == null)
                        {
                            break;
                        }

                        if (totalHours + current.AmountOfWorkingHours < HoursInShift * 2)
                        {
                            selected.Add(current);
                            totalHours += current.AmountOfWorkingHours;
                            current = workingToday.FirstAppropriate(totalHours, selected);
                            continue;
                        }
                        else if (totalHours + current.AmountOfWorkingHours == HoursInShift * 2)
                        {
                            selected.Add(current);
                            totalHours += current.AmountOfWorkingHours;
                            break;
                        }
                        else
                        {
                            Employee next = workingToday.NextAppropriate(workingToday.IndexOf(current) + 1, totalHours, selected);
                            if (next != null)
                            {
                                current = next;
                            }
                            else
                            {
                                Employee last = selected.Last();

                                selected.Remove(last);
                                totalHours -= last.AmountOfWorkingHours;

                                next = workingToday.NextAppropriate(workingToday.IndexOf(last) + 1, totalHours, selected);
                                if (next == null)
                                {
                                    selected.Add(last);
                                    totalHours += last.AmountOfWorkingHours;

                                    selected.Add(current);
                                    totalHours += current.AmountOfWorkingHours;
                                    break;
                                }
                                else
                                {
                                    selected.Add(next);
                                    totalHours += next.AmountOfWorkingHours;

                                    current = workingToday.FirstAppropriate(totalHours, selected);
                                }
                            }
                        }

                    }

                    List<Schedule> scheduleItems = new List<Schedule>();
                    int startTime = StartTime;

                    foreach (Employee e in selected)
                    {
                        workingToday.Remove(e);

                        Schedule scheduleItem = new Schedule();
                       
                        scheduleItem.RestaurantId = r.Id;
                        scheduleItem.Restaurant = r;
                        scheduleItem.EmployeeId = e.Id;
                        scheduleItem.Employee = e;                     

                        if (totalHours > HoursInShift * 2 && e.Equals(selected.Last()))
                        {
                            scheduleItem.From = new DateTime(2000, 1, 1, EndTime - e.AmountOfWorkingHours, 0, 0);
                            scheduleItem.To = new DateTime(2000, 1, 1, 0, 0, 0);
                        }
                        else
                        {                            
                            scheduleItem.From = new DateTime(2000, 1, 1, startTime, 0, 0);
                            startTime += e.AmountOfWorkingHours;
                            scheduleItem.To = new DateTime(2000, 1, 1, startTime % 24, 0, 0);
                        }

                        scheduleItem.Date = Today(e, (int)month, i);

                        scheduleItems.Add(scheduleItem);
                    }

                    db.Schedules.AddRange(scheduleItems);
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