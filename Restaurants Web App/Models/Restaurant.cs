using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Restaurants.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }
    }
}