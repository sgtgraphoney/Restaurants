using System;
using System.Collections.Generic;
using Restaurants.Models;

namespace Restaurants.Controllers
{
    /// <summary>
    /// A list of instances of <see cref="Employee"/> class.
    /// Contains some methods for searching that take into account specified conditions.
    /// </summary>
    public class EmployeeList : List<Employee>
    {
        public EmployeeList() { }


        public EmployeeList(IEnumerable<Employee> source) : base(source) { }


        /// <summary>
        /// Searches for the next appropriate employee depending on parameters.
        /// </summary>
        /// <param name="fromIndex">What index search begins at</param>
        /// <param name="totalHours">Amount of occupied working hours</param>
        /// <param name="excluded">A list with employees that are already mentioned in the schedule</param>
        /// <returns>Next appropriate employee from the list</returns>
        public Employee NextAppropriate(int fromIndex, int totalHours, List<Employee> excluded)
        {
            for (int i = fromIndex; i < Count; i++)
            {
                if (IsAppropriate(i, totalHours, excluded))
                {
                    return this[i];
                }
            }

            return null;
        }


        /// <summary>
        /// Searches for the first appropriate employee depending on parameters.
        /// </summary>
        /// <param name="totalHours">Amount of occupied working hours</param>
        /// <param name="excluded">A list with employees that are already mentioned in the schedule</param>
        /// <returns>First appropriate employee from the list</returns>
        public Employee FirstAppropriate(int totalHours, List<Employee> excluded)
        {
            return NextAppropriate(0, totalHours, excluded);
        }


        /// <summary>
        /// Searches for the last appropriate employee depending on parameters.
        /// </summary>
        /// <param name="totalHours">Amount of occupied working hours</param>
        /// <param name="excluded">A list with employees that are already mentioned in the schedule</param>
        /// <returns>First appropriate employee from the list</param>
        /// <returns>Last appropriate employee from the list</returns>
        public Employee LastAppropriate(int totalHours, List<Employee> excluded)
        {
            for (int i = Count - 1; i >= 0 ; i--)
            {
                if (IsAppropriate(i, totalHours, excluded))
                {
                    return this[i];
                }
            }

            return null;
        }


        private bool IsAppropriate(int index, int totalHours, List<Employee> excluded)
        {
            Employee e = this[index];

            if (excluded.Contains(e))
            {
                return false;
            }

            return e.Shift == null || !(e.Shift == ScheduleController.MorningShift 
                && totalHours + e.AmountOfWorkingHours > ScheduleController.HoursInShift ||
                e.Shift == ScheduleController.EveningShift && totalHours < ScheduleController.HoursInShift);
        }

    }
}