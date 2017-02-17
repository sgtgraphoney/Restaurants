using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplicationMvc01.Models
{
    public class EmployeeList : List<Employee>
    {
        private const string MorningShift = "утренняя", EveningShift = "вечерняя";
        private const int HoursInShift = 7;


        public EmployeeList() { }


        public EmployeeList(IEnumerable<Employee> source) : base(source) { }


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


        public Employee FirstAppropriate(int totalHours, List<Employee> excluded)
        {
            return NextAppropriate(0, totalHours, excluded);
        }


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

            return e.Shift == null || !(e.Shift.Equals(MorningShift) && totalHours + e.AmountOfWorkingHours > HoursInShift ||
                e.Shift.Equals(EveningShift) && totalHours < HoursInShift);
        }

    }
}