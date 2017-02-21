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
        public static readonly int[] DaysInMonth = new int[MonthsInYear];

        private const int HoursInShift = 7, StartTime = 10, EndTime = 24;
        private const int MonthsInYear = 12;

        static ScheduleController()
        {
            int i = 0;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 28;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 30;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 30;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 30;
            DaysInMonth[i++] = 31;
            DaysInMonth[i++] = 30;
            DaysInMonth[i++] = 31;
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
            if (request == null)
            {
                return null;
            }

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

            for (int i = 1; i <= DaysInMonth[month - 1]; i++)
            {
                EmployeeList workingToday = new EmployeeList(employees.FindAll(e => WorksToday(e, month, i)));
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
                        scheduleItem.Date = Today(month, i);

                        bool eveningShift = false;
                        if (e.Shift == null || (eveningShift = e.Shift.Equals("вечерняя")))
                        {
                            if (eveningShift && totalHours < HoursInShift)
                            {
                                totalHours = HoursInShift;
                            }

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

            DateTime now = Today(month, day);
            if (now < e.FirstWorkingDay)
            {
                return false;
            }

            TimeSpan diff = now - e.FirstWorkingDay;
            return (diff.TotalDays + 1) % (workingDaysAmount + weekendAmount) < workingDaysAmount;
        }


        private DateTime Today(int month, int day)
        {
            int year = DateTime.Now.Year;
            if (month < DateTime.Now.Month)
            {
                year++;
            }
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

}