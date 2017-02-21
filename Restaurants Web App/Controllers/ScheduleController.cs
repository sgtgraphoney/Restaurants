using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Restaurants.Models;

namespace Restaurants.Controllers
{
    /// <summary>
    /// <c>ScheduleController</c> is responsible for handling requests that relate to the schedule.
    /// </summary>
    public class ScheduleController : Controller
    {
        public const string MorningShift = "утренняя", EveningShift = "вечерняя";
        public const int HoursInShift = 7;

        private static readonly int[] DaysInMonth = new int[MonthsInYear];

        private const int StartTime = 10, EndTime = 24;
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


        /// <summary>
        /// Shows the scheduling menu. It contains buttons for generating and displaying schedules.
        /// </summary>
        /// <returns>A view with the menu.</returns>
        [HttpGet]
        public ActionResult Menu()
        {
            ViewBag.Restaurants = db.Restaurants.ToList();
            return View();
        }


        /// <summary>
        /// Shows a schedule for the specified month if the "Show" button on the menu page is pressed. 
        /// Generates a new schedule if the "Generate" button is pressed.
        /// </summary>
        /// <remarks>
        /// Unfortunately it can't take into account the attestations :(
        /// Therefore the scedule is generated like there's no any cuisines in the rastaurants.
        /// </remarks>
        /// <param name="request">A value from a pressed button</param>
        /// <param name="month">A month that a schedule should be generated for</param>
        /// <param name="restaurantId">A restaurant that a schedule should be generated for</param>
        /// <returns>A view with schedule if parameters are correct or an error page otherwise</returns>
        [HttpGet]
        public ActionResult Show(string request, int month, int restaurantId)
        {
            if (month < 1 || month > 12 || db.Restaurants.First(x => x.Id == restaurantId) == null)
            {
                return null;
            }

            if (request == "Составить")
            {
                var old = from schedule in db.Schedules
                          where schedule.Date.Month == month && schedule.RestaurantId == restaurantId
                          select schedule;

                db.Schedules.RemoveRange(old);
                db.SaveChanges();

                GenerateSchedule(month, restaurantId);

                SelectSchedule(month, restaurantId);
                return View();
            }
            else if (request == "Показать")
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


        // Writes a schedule for the specified month and restaurant into ViewBag.
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


        private void GenerateSchedule(int month, int restaurantId)
        {
            List<Employee> employees = db.Employees.Include("Attestations").ToList();

            for (int i = 1; i <= DaysInMonth[month - 1]; i++)
            {
                EmployeeList workingToday = new EmployeeList(employees.FindAll(e => WorksToday(e, month, i)));
                workingToday.Sort(new EmployeeComparer());

                Restaurant restaurant = db.Restaurants.First(x => x.Id == restaurantId);

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

                WriteScheduleListToDatabase(workingToday, selected, restaurant, month, i);
            }

            db.SaveChanges();
        }


        private void WriteScheduleListToDatabase(List<Employee> workingToday, List<Employee> selected,
            Restaurant restaurant, int month, int day)
        {
            List<Schedule> scheduleItems = new List<Schedule>();

            int totalHours = 0;

            foreach (Employee e in selected)
            {
                workingToday.Remove(e);

                Schedule scheduleItem = new Schedule();

                scheduleItem.RestaurantId = restaurant.Id;
                scheduleItem.Restaurant = restaurant;
                scheduleItem.EmployeeId = e.Id;
                scheduleItem.Employee = e;
                scheduleItem.Date = Today(month, day);

                bool eveningShift = false;
                if (e.Shift == null || (eveningShift = e.Shift == EveningShift))
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
                else if (e.Shift == MorningShift)
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


        // Checks whether an employee works on the specified day.
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


    // Employees are sorted in descending order of amount of working hours.
    internal class EmployeeComparer : Comparer<Employee>
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