using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages_Appointments
{
    public class DetailsModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;

        public DetailsModel(HospitalWebApp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FirstOrDefaultAsync(m => m.Id == id);

            if (appointment is not null)
            {
                Appointment = appointment;

                return Page();
            }

            return NotFound();
        }
    }
}
