using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplicationMvc01.Controllers
{
    public class SheduleController : Controller
    {
        // GET: Shedule
        public ActionResult SheduleEditor()
        {
            return View();
        }
    }
}