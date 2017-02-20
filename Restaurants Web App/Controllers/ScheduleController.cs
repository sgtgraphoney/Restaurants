using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Restaurants.Models;

namespace Restaurants.Controllers
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
        public ActionResult Menu()
        {
            ViewBag.Restaurants = db.Restaurants.ToList();
            return View();
        }


        [HttpGet]
        public ActionResult Show(string request, int month, int restaurantId)
        {
            if (request.Equals("Составить"))
            {
                var old = from schedule in db.Schedules
                          where schedule.Date.Month == month && schedule.RestaurantId == restaurantId
                          select schedule;

                db.Schedules.RemoveRange(old);
                db.SaveChanges();

                GenerateSchedule(month);

                SelectSchedule(month, restaurantId);
                return View();
            }
            else if (request.Equals("Показать"))
            {
                SelectSchedule(month, restaurantId);

                ViewBag.RestaurantId = restaurantId;
                ViewBag.Month = month;

                return View();
            }
            else
            {
                return null;
            }            
        }


        private void SelectSchedule(int month, int restaurantId)
        {
            var scheduleList = from schedule in db.Schedules.Include("Employee")
                               where schedule.RestaurantId == restaurantId && schedule.Date.Month == month
                               orderby schedule.Date
                               select schedule;

            ViewBag.ScheduleList = scheduleList.ToList();

            ViewBag.WorkersPerDay = (from schedule in scheduleList
                                     group schedule by schedule.Date into dateGroup
                                     select dateGroup.Count()).ToList();
        }


        private void GenerateSchedule(int month)
        {
            List<Employee> employees = db.Employees.Include("Attestations").ToList();

            for (int i = 1; i <= DaysInMonth[(Months)month]; i++)
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
                            if (totalHours < HoursInShift)
                            {
                                totalHours = 0;

                                Employee next = workingToday.LastAppropriate(totalHours, selected);
                                if (next != null)
                                {
                                    selected.Add(next);
                                }
                                
                                totalHours = HoursInShift;

                                if ((current = workingToday.FirstAppropriate(totalHours, selected)) == null)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
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

                    totalHours = 0;

                    foreach (Employee e in selected)
                    {
                        workingToday.Remove(e);

                        Schedule scheduleItem = new Schedule();

                        scheduleItem.RestaurantId = r.Id;
                        scheduleItem.Restaurant = r;
                        scheduleItem.EmployeeId = e.Id;
                        scheduleItem.Employee = e;
                        scheduleItem.Date = Today(e, (int)month, i);

                        if (e.Shift == null || e.Shift.Equals("вечерняя"))
                        {
                            if (totalHours + e.AmountOfWorkingHours <= HoursInShift * 2)
                            {
                                scheduleItem.From = new TimeSpan(StartTime + totalHours, 0, 0);
                                totalHours += e.AmountOfWorkingHours;
                                scheduleItem.To = new TimeSpan((StartTime + totalHours) % 24, 0, 0);
                            }
                            else
                            {
                                scheduleItem.From = new TimeSpan(StartTime + HoursInShift * 2 - e.AmountOfWorkingHours, 0, 0);
                                scheduleItem.To = new TimeSpan(0, 0, 0);
                            }
                        }
                        else if (e.Shift.Equals("утренняя"))
                        {
                            if (totalHours + e.AmountOfWorkingHours <= HoursInShift)
                            {
                                scheduleItem.From = new TimeSpan(StartTime + totalHours, 0, 0);
                                totalHours += e.AmountOfWorkingHours;
                                scheduleItem.To = new TimeSpan(StartTime + totalHours, 0, 0);
                            }
                            else
                            {
                                scheduleItem.From = new TimeSpan(StartTime + HoursInShift - e.AmountOfWorkingHours, 0, 0);
                                totalHours = HoursInShift;
                                scheduleItem.To = new TimeSpan(StartTime + HoursInShift, 0, 0);
                            }
                        }

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
            return (diff.TotalDays + 1) % (workingDaysAmount + weekendAmount) < workingDaysAmount;
        }


        private DateTime Today(Employee e, int month, int day)
        {
            int year = e.FirstWorkingDay.Year;
            if (month < e.FirstWorkingDay.Month)
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