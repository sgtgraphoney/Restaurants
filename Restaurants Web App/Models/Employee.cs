using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Restaurants.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string Patronymic { get; set; }
        public string LastName { get; set; }
        public string Shift { get; set; }
        public int AmountOfWorkingHours { get; set; }
        public string Session { get; set; }
        [DataType(DataType.Date)]
        public DateTime FirstWorkingDay { get; set; }
        [ForeignKey("Attestation")]
        public List<Attestation> Attestations { get; set; }

        public Employee()
        {
            Attestations = new List<Attestation>();
        }

        public void copyFrom(Employee e)
        {
            Id = e.Id;
            FirstName = e.FirstName;
            Patronymic = e.Patronymic;
            LastName = e.LastName;
            Shift = e.Shift;
            AmountOfWorkingHours = e.AmountOfWorkingHours;
            Session = e.Session;
            FirstWorkingDay = e.FirstWorkingDay;
            Attestations = e.Attestations;
        }

    }
}