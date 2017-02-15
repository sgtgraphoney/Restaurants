using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplicationMvc01.Models
{
    public class Attestation
    {
        [Key]
        public int Id { get; set; }
        public string Specialization { get; set; }
        [ForeignKey("Employee")]
        public List<Employee> Employees { get; set; }

        public Attestation()
        {
            Employees = new List<Employee>();
        }
    }
}