using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Vendors.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Modules.Vendors;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // KTV cũng cần xem danh sách Vendor để chọn khi gửi đi
    public async Task<ActionResult<List<VendorResponse>>> GetVendors([FromQuery] bool? isActive)
    {
        var query = _context.Vendors.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(v => v.IsActive == isActive.Value);
        }

        var vendors = await query
            .OrderBy(v => v.Name)
            .Select(v => new VendorResponse
            {
                Id = v.Id,
                Name = v.Name,
                ContactPerson = v.ContactPerson,
                Phone = v.Phone,
                Email = v.Email,
                Address = v.Address,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(vendors);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<VendorResponse>> GetVendorById(int id)
    {
        var vendor = await _context.Vendors
            .Where(v => v.Id == id)
            .Select(v => new VendorResponse
            {
                Id = v.Id,
                Name = v.Name,
                ContactPerson = v.ContactPerson,
                Phone = v.Phone,
                Email = v.Email,
                Address = v.Address,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (vendor == null) return NotFound(new { message = "Vendor not found" });

        return Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorResponse>> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Vendors.AnyAsync(v => v.Name == name))
        {
            return BadRequest(new { message = "A vendor with this name already exists" });
        }

        var vendor = new Vendor
        {
            Name = name,
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        var response = new VendorResponse
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            IsActive = vendor.IsActive,
            CreatedAt = vendor.CreatedAt
        };

        return CreatedAtAction(nameof(GetVendorById), new { id = vendor.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VendorResponse>> UpdateVendor(int id, [FromBody] UpdateVendorRequest request)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null) return NotFound(new { message = "Vendor not found" });

        var name = request.Name.Trim();
        if (await _context.Vendors.AnyAsync(v => v.Name == name && v.Id != id))
        {
            return BadRequest(new { message = "A vendor with this name already exists" });
        }

        vendor.Name = name;
        vendor.ContactPerson = request.ContactPerson?.Trim();
        vendor.Phone = request.Phone?.Trim();
        vendor.Email = request.Email?.Trim();
        vendor.Address = request.Address?.Trim();
        vendor.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        var response = new VendorResponse
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            IsActive = vendor.IsActive,
            CreatedAt = vendor.CreatedAt
        };

        return Ok(response);
    }

    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult<VendorResponse>> ToggleVendorActive(int id)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null) return NotFound(new { message = "Vendor not found" });

        vendor.IsActive = !vendor.IsActive;
        await _context.SaveChangesAsync();

        var response = new VendorResponse
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            IsActive = vendor.IsActive,
            CreatedAt = vendor.CreatedAt
        };

        return Ok(response);
    }
}
