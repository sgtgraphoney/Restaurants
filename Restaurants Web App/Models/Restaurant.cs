using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplicationMvc01.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }
        public string Address { get; set; }
    }
}