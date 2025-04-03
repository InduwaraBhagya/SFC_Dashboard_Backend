using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDB.Models;

namespace SFCDB.Controllers
{
    public class PERecordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PERecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PERecords
        public async Task<IActionResult> Index(string searchType, string peNumber, string customer,
            string jobReference, string soNumber, int pageIndex = 1)
        {
            var query = from r in _context.PERecords
                        select r;

            // Store current filters in ViewData
            ViewData["SearchType"] = searchType ?? "peNumber";
            ViewData["PENumberFilter"] = peNumber;
            ViewData["CustomerFilter"] = customer;
            ViewData["JobReferenceFilter"] = jobReference;
            ViewData["SONumberFilter"] = soNumber;

            // Determine the search string based on the search type
            string searchString = searchType switch
            {
                "customer" => customer,
                "jobReference" => jobReference,
                "soNumber" => soNumber,
                _ => peNumber
            };

            // Apply search filters based on type
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchType)
                {
                    case "customer":
                        query = query.Where(p => p.CUSTOMER != null && p.CUSTOMER.Contains(customer));
                        break;
                    case "jobReference":
                        query = query.Where(p => p.JOB_REFERENCE != null && p.JOB_REFERENCE.Contains(jobReference));
                        break;
                    case "soNumber":
                        query = query.Where(p => p.SO_NUMBER != null && p.SO_NUMBER.Contains(soNumber));
                        break;
                    default: // peNumber
                        query = query.Where(p => p.PE_NUMBER != null && p.PE_NUMBER.Contains(peNumber));
                        break;
                }
            }

            // Return empty list if no search criteria provided
            if (string.IsNullOrEmpty(peNumber) &&
                string.IsNullOrEmpty(customer) &&
                string.IsNullOrEmpty(jobReference) &&
                string.IsNullOrEmpty(soNumber))
            {
                return View(new PaginatedList<PERecord>(new List<PERecord>(), 0, pageIndex, 10));
            }

            // Order results
            query = query.OrderByDescending(p => p.WO_START_DATE)
                        .ThenBy(p => p.PE_NUMBER);

            int pageSize = 10;
            var paginatedList = await PaginatedList<PERecord>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

            // Log the number of records found
            Console.WriteLine($"Total records found: {paginatedList.Count}");

            // Pass search parameters back to the view for maintaining the search state
            ViewData["SearchType"] = searchType;
            ViewData["SearchString"] = searchString;

            return View(paginatedList);
        }


        // GET: PERecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords
                .FirstOrDefaultAsync(m => m.ID == id);
            if (pERecord == null)
            {
                return NotFound();
            }

            return View(pERecord);
        }

        // GET: PERecords/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PERecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PROVINCE,REGION,RTOM,RTOM_DESCRIPTION,JOB_REFERENCE,CONTRACTOR_NAME,PE_NUMBER,PE_ACTIVITY,PE_NATURE,PE_TITLE,PE_OBJECTIVE,PE_AREA,SO_NUMBER,TASK_SEQ,TASK_NAME,TASK_WG,WO_ACTUAL_START_DATE,REQUEST_REFERENCE_NO,SO_ID,REGION_1,PROVINCE_1,RTOM_1,LEA,CCT_ID,SERVICE_CATEGORY,SERVICE_TYPE,SO_CREATE_DATE,ORDER_TYPE,CRM_ORDER,WO_ID,PENDING_TASK_NAME,PENDING_WG,WO_STATUS,WO_START_DATE,SERVICE_SPEED,SERVICE_REQUIRED_DATE,FIBER_PE_NO,FIBER_SO_ID,PRODUCT_SO_ID,FIBER_PE_TASK_NAME,FIBER_PE_TASK_WG,PE_WO_COMMENTS,CUSTOMER,CUS_TYPE,ACCOUNT_MANAGER,SECTION_HANDLED_BY,LOCATION_A_ADDRESS,LOCATION_B_ADDRESS,NTU_TYPE,ACCESS_MEDIUM,ACCESS_MEDIUM_A_END,ACCESS_MEDIUM_B_END,WO_COMMENTS")] PERecord pERecord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pERecord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pERecord);
        }

        // GET: PERecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords.FindAsync(id);
            if (pERecord == null)
            {
                return NotFound();
            }
            return View(pERecord);
        }

        // POST: PERecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PROVINCE,REGION,RTOM,RTOM_DESCRIPTION,JOB_REFERENCE,CONTRACTOR_NAME,PE_NUMBER,PE_ACTIVITY,PE_NATURE,PE_TITLE,PE_OBJECTIVE,PE_AREA,SO_NUMBER,TASK_SEQ,TASK_NAME,TASK_WG,WO_ACTUAL_START_DATE,REQUEST_REFERENCE_NO,SO_ID,REGION_1,PROVINCE_1,RTOM_1,LEA,CCT_ID,SERVICE_CATEGORY,SERVICE_TYPE,SO_CREATE_DATE,ORDER_TYPE,CRM_ORDER,WO_ID,PENDING_TASK_NAME,PENDING_WG,WO_STATUS,WO_START_DATE,SERVICE_SPEED,SERVICE_REQUIRED_DATE,FIBER_PE_NO,FIBER_SO_ID,PRODUCT_SO_ID,FIBER_PE_TASK_NAME,FIBER_PE_TASK_WG,PE_WO_COMMENTS,CUSTOMER,CUS_TYPE,ACCOUNT_MANAGER,SECTION_HANDLED_BY,LOCATION_A_ADDRESS,LOCATION_B_ADDRESS,NTU_TYPE,ACCESS_MEDIUM,ACCESS_MEDIUM_A_END,ACCESS_MEDIUM_B_END,WO_COMMENTS")] PERecord pERecord)
        {
            if (id != pERecord.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pERecord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PERecordExists(pERecord.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pERecord);
        }

        // GET: PERecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pERecord = await _context.PERecords
                .FirstOrDefaultAsync(m => m.ID == id);
            if (pERecord == null)
            {
                return NotFound();
            }

            return View(pERecord);
        }

        // POST: PERecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pERecord = await _context.PERecords.FindAsync(id);
            if (pERecord != null)
            {
                _context.PERecords.Remove(pERecord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PERecordExists(int id)
        {
            return _context.PERecords.Any(e => e.ID == id);
        }
    }
}
