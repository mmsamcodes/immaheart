using Microsoft.AspNetCore.Mvc;

public class ServicesController : Controller
{
    // Main Services Index
    public IActionResult Index()
    {
        return View();
    }

    // Service Detail Pages
    public IActionResult EmergencyOutpatient()
    {
        return View();
    }

    public IActionResult MaternityChildHealth()
    {
        return View();
    }

    public IActionResult DiagnosticsLab()
    {
        return View();
    }

    public IActionResult ChronicDisease()
    {
        return View();
    }

    public IActionResult AmbulanceTransport()
    {
        return View();
    }

    public IActionResult DentalClinic()
    {
        return View();
    }

    public IActionResult PhysiotherapyRehab()
    {
        return View();
    }

    // Quick Access Pages
    public IActionResult EmergencyCare()
    {
        return View();
    }

    public IActionResult LaboratoryTests()
    {
        return View();
    }

    public IActionResult ClinicAppointments()
    {
        return View();
    }

    public IActionResult BillingSupport()
    {
        return View();
    }
}