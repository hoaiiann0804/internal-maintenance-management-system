using InternalMaintenance.Api.Common;
using InternalMaintenance.Api.Common.Pagination;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Tickets.Contracts;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalMaintenance.Api.Services.Interface;

namespace InternalMaintenance.Api.Modules.Tickets;

[ApiController]
[Route("api/tickets")]
public class MaintenanceTicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;

    private readonly ITicketCodeGenerator _ticketCodeGenerator;

    public MaintenanceTicketsController(
        AppDbContext context,
        CurrentUserService currentUserService,
        ITicketCodeGenerator ticketCodeGenerator

        )
    {
        _context = context;
        _currentUserService = currentUserService;
        _ticketCodeGenerator = ticketCodeGenerator;
    }



    // Ap dung quyen truy cap du lieu ticket theo role.
    // Moi role chi xem duoc ticket nam trong pham vi cho phep.

    private IQueryable<MaintenanceTicket> ApplyTicketAccessControl(
        IQueryable<MaintenanceTicket> query
    )
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var departmentId = _currentUserService.DepartmentId;

        // Admin xem toan bo ticket.
        if (role == UserRoles.Admin)
        {
            return query;
        }
        // Manager chi xem ticket cua phong ban minh quan ly hoac pham vi bao tri cua phong minh.
        if (role == UserRoles.Manager)
        {
            return query.Where(
                ticket => ticket.Equipment!.DepartmentId == departmentId ||
                          ticket.Equipment!.MaintenanceDepartmentId == departmentId
            );
        }
        // Staff chi duoc xem ticket do chinh minh tao.
        if (role == UserRoles.Staff)
        {
            return query.Where(
                ticket => ticket.CreatedByUserId == userId
            );
        }
        // Technician chi duoc xem ticket duoc giao xu ly hoac ticket do chinh minh tao.
        if (role == UserRoles.Technician)
        {
            return query.Where(
                ticket => ticket.AssignedTechnicianId == userId ||
                          ticket.SupportTechnicianId == userId ||
                          ticket.CreatedByUserId == userId
            );
        }
        // Role khong hop le.
        return query.Where(ticket => false);
    }



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MaintenanceTicketResponse>>> GetMaintenanceTickets(
        [FromQuery] TicketQuery query
    )
    {
        var ticketQuery = _context.MaintenanceTickets
       .AsNoTracking()
       .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.Status == status
            );
        }

        var priority = query.Priority?.Trim();
        if (!string.IsNullOrWhiteSpace(priority))
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.Priority == priority
            );
        }

        if (query.EquipmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.EquipmentId == query.EquipmentId.Value
            );
        }

        var totalItems = await ticketQuery.CountAsync();
        ticketQuery = ticketQuery
        .OrderByDescending(ticket => ticket.CreatedAt)
        .ThenBy(ticket => ticket.Id);

        ticketQuery = ticketQuery.ApplyPaging(query);

        var tickets = await ticketQuery.Select(
            ticket => new MaintenanceTicketResponse
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                EquipmentId = ticket.EquipmentId,
                EquipmentCode = ticket.Equipment!.Code,
                EquipmentName = ticket.Equipment!.Name,
                EquipmentDepartmentId = ticket.Equipment.DepartmentId,
                EquipmentMaintenanceDepartmentId = ticket.Equipment.MaintenanceDepartmentId,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = ticket.CreatedByUser!.FullName,
                // (co the dung cach  CreatedByUserName = ticket.CreatedByUser!= null? ticket.CreatedByUser.FullName: string.Empty )
                AssignedTechnicianId = ticket.AssignedTechnicianId,
                AssignedTechnicianName = ticket.AssignedTechnician != null ? ticket.AssignedTechnician.FullName : null,
                SupportTechnicianId = ticket.SupportTechnicianId,
                SupportTechnicianName = ticket.SupportTechnician != null ? ticket.SupportTechnician.FullName : null,
                Priority = ticket.Priority,
                Status = ticket.Status,
                ResolutionNote = ticket.ResolutionNote,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,
                DueAt = ticket.DueAt,
                SlaStatus = ticket.SlaStatus
            }
        ).ToListAsync();
        return Ok(tickets.ToPagedResponse(query, totalItems));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> GetMaintenanceTicketById(int id)
    {
        // Khoi tao truy van va ap dung phan quyen theo role.
        var ticketQuery = _context.MaintenanceTickets
        .AsNoTracking()
        .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticket = await ticketQuery.Where(
            ticket => ticket.Id == id
        ).Select(
            ticket => new MaintenanceTicketResponse
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                EquipmentId = ticket.EquipmentId,
                EquipmentCode = ticket.Equipment!.Code,
                EquipmentName = ticket.Equipment!.Name,
                EquipmentDepartmentId = ticket.Equipment.DepartmentId,
                EquipmentMaintenanceDepartmentId = ticket.Equipment.MaintenanceDepartmentId,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = ticket.CreatedByUser!.FullName,
                // (co the dung cach  CreatedByUserName = ticket.CreatedByUser!= null? ticket.CreatedByUser.FullName: string.Empty )
                AssignedTechnicianId = ticket.AssignedTechnicianId,
                AssignedTechnicianName = ticket.AssignedTechnician != null ? ticket.AssignedTechnician.FullName : null,
                SupportTechnicianId = ticket.SupportTechnicianId,
                SupportTechnicianName = ticket.SupportTechnician != null ? ticket.SupportTechnician.FullName : null,
                Priority = ticket.Priority,
                Status = ticket.Status,
                ResolutionNote = ticket.ResolutionNote,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,
                DueAt = ticket.DueAt,
                SlaStatus = ticket.SlaStatus
            }
        ).FirstOrDefaultAsync();

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                }
            );
        }
        return Ok(ticket);
    }

    [Authorize()]
    [HttpPost]
    public async Task<ActionResult<MaintenanceTicketResponse>> CreateMaintenanceTicket(CreateTicketRequest request)
    {
        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;
        var equipment = await _context.Equipment
        .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
        {
            return NotFound(
               new
               {
                   message = "Equipment does not exist"
               }
            );
        }
        // Khong cho tao ticket cho thiet bi da thanh ly.

        if (equipment.Status == EquipmentStatuses.Retired)
        {
            return BadRequest(
                new
                {
                    message = "Cannot create ticket for retired equipment"
                }
            );
        }

        // Neu thiet bi dang under maintenance thi khong duoc tao ticket moi.
        if (equipment.Status == EquipmentStatuses.UnderMaintenance)
        {
            return BadRequest(
                new
                {
                    message = "Equipment is already under maintenance"
                }
            );
        }

        // Thiet bi self-managed = khong co team bao tri rieng.
        // Rule nghiep vu: thiet bi nay khong di vao workflow ticket bao tri de tranh tao ticket du.
        if (!equipment.MaintenanceDepartmentId.HasValue)
        {
            return BadRequest(
                new
                {
                    message = "This equipment is self-managed and does not create maintenance tickets"
                }
            );
        }

        // Khong cho tao nhieu ticket dang mo cho cung mot thiet bi.
        // Neu da co ticket Pending/Assigned/InProgress thi ticket truoc chua dong xong.
        // Chan truong hop tao ticket trung lap.

        var hasOpenTicket = await _context.MaintenanceTickets
        .AnyAsync(ticket => ticket.EquipmentId == request.EquipmentId
        && TicketWorkflowRules.OpenStatuses.Contains(ticket.Status));

        if (hasOpenTicket)
        {
            return BadRequest(
                new
                {
                    message = "This equipment already has an open maintenance ticket"
                }
            );
        }

        var priority = string.IsNullOrWhiteSpace(request.Priority)
        ? TicketPriorities.Medium : request.Priority.Trim();

        if (!TicketWorkflowRules.IsAllowedPriority(priority))
        {
            return BadRequest(
                new
                {
                    message = "Invalid ticket priority"
                }
            );
        }



        var ticket = new MaintenanceTicket
        {
            TicketCode = _ticketCodeGenerator.GenerateTicketCode(),
            Title = title,
            Description = description,
            EquipmentId = request.EquipmentId,
            Priority = priority,
            Status = TicketStatuses.Pending,
            CreatedByUserId = userId,
            AssignedTechnicianId = null,
            CreatedAt = DateTime.UtcNow,
            DueAt = SlaPolicy.CalculateDueAt(DateTime.UtcNow, priority),
            SlaStatus = SlaPolicy.InSLA
        };

        _context.MaintenanceTickets.Add(ticket);
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
        .Where(t => t.Id == ticket.Id)
        .Select(
            t => new MaintenanceTicketResponse
            {
                Id = t.Id,
                TicketCode = t.TicketCode,
                Title = t.Title,
                Description = t.Description,
                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment!.Name,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser!.FullName,
                AssignedTechnicianId = t.AssignedTechnicianId,
                AssignedTechnicianName = t.AssignedTechnician != null ? t.AssignedTechnician.FullName : null,
                SupportTechnicianId = t.SupportTechnicianId,
                SupportTechnicianName = t.SupportTechnician != null ? t.SupportTechnician.FullName : null,
                Priority = t.Priority,
                Status = t.Status,
                ResolutionNote = t.ResolutionNote,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt,
                DueAt = t.DueAt,
                SlaStatus = t.SlaStatus
            }
        ).FirstOrDefaultAsync();

        return CreatedAtAction(
            nameof(GetMaintenanceTicketById),
            new { id = ticket.Id },
            response
        );
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> UpdateTicket(int id, UpdateTicketRequest request)
    {
        var ticket = await _context.MaintenanceTickets
        // Load cac thong tin lien quan de tra ve du lieu ticket day du sau khi cap nhat.
        .Include(t => t.Equipment)
        .Include(t => t.CreatedByUser)
        .Include(t => t.AssignedTechnician)
        .FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket Not Found"
                }
            );
        }

        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var department = _currentUserService.DepartmentId;

        // Khong cho phep bat ky ai (ke ca Admin) sua thong tin ticket khi da chot (Closed/Resolved/Cancelled)
        // De tranh viec vo tinh cap nhat lai SlaStatus hoac DueAt.
        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(new { message = "Cannot edit a finalized ticket" });
        }

        // Requester, bat ke role nao, chi duoc sua ticket cua minh khi ticket con Pending.
        if (ticket.CreatedByUserId == userId)
        {
            if (ticket.Status != TicketStatuses.Pending)
                return BadRequest(new { message = "Ticket already in processing state" });
        }
        else
        {
            // Neu khong phai requester thi chi Admin hoac Manager moi duoc sua.
            if (role != UserRoles.Admin && role != UserRoles.Manager)
            {
                return Forbid();
            }

            // Manager chi duoc sua ticket nam trong pham vi phong ban minh quan ly.
            if (role == UserRoles.Manager && !TicketAccessPolicy.CanAccess(ticket, _currentUserService))
            {
                return Forbid();
            }
        }

        if (!TicketWorkflowRules.IsAllowedPriority(request.Priority))
        {
            return BadRequest("Invalid priority");
        }

        if (ticket.Priority != request.Priority.Trim())
        {
            ticket.DueAt = SlaPolicy.CalculateDueAt(ticket.CreatedAt, request.Priority.Trim());
            ticket.NearBreachNotifiedAt = null;
            ticket.BreachedNotifiedAt = null;
            ticket.EscalatedNotifiedAt = null;
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Priority = request.Priority.Trim();

        await _context.SaveChangesAsync();

        var response = new MaintenanceTicketResponse
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Description = ticket.Description,
            EquipmentId = ticket.EquipmentId,
            EquipmentCode = ticket.Equipment!.Code,
            EquipmentName = ticket.Equipment!.Name,
            EquipmentDepartmentId = ticket.Equipment.DepartmentId,
            EquipmentMaintenanceDepartmentId = ticket.Equipment.MaintenanceDepartmentId,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUser!.FullName,
            AssignedTechnicianId = ticket.AssignedTechnicianId,
            AssignedTechnicianName = ticket.AssignedTechnician?.FullName,
            SupportTechnicianId = ticket.SupportTechnicianId,
            SupportTechnicianName = ticket.SupportTechnician?.FullName,
            Priority = ticket.Priority,
            Status = ticket.Status,
            ResolutionNote = ticket.ResolutionNote,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            DueAt = ticket.DueAt,
            SlaStatus = ticket.SlaStatus
        };

        return Ok(response);
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [HttpPatch("{id:int}/assign")]
    public async Task<ActionResult<MaintenanceTicketResponse>> AssignTicket(int id, AssignTicketRequest request)
    {
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .Include(t => t.CreatedByUser)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
             new
             {
                 message = "Ticket Not Found"
             }
            );
        }

        if (!TicketAccessPolicy.CanAssign(ticket, _currentUserService))
        {
            return Forbid();
        }

        // Thiet bi self-managed khong duoc phan cong technician.
        // Neu khong co maintenance department rieng thi ticket khong di vao luong assign.
        if (!ticket.Equipment!.MaintenanceDepartmentId.HasValue)
        {
            return BadRequest(new
            {
                message = "This equipment is self-managed and cannot be assigned to a technician"
            });
        }

        var targetDeptId = ticket.Equipment.MaintenanceDepartmentId.Value;

        // Chan phan cong khi ticket da dong hoac da huy.
        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(new
            {
                message = "Cannot assign a closed or cancelled ticket"
            });
        }
        // Chi cho assign ticket dang Pending hoac Assigned.
        // Pending = gan lan dau, Assigned = doi technician neu can.

        if (!TicketWorkflowRules.IsAssignableStatus(ticket.Status))
        {
            return BadRequest(
                  new
                  {
                      message = "Only pending or assigned tickets can be assigned "
                  }
            );

        }


        var technician = await _context.Users
        .Include(user => user.Role)
        .FirstOrDefaultAsync(u => u.Id == request.AssignedTechnicianId);

        if (technician is null)
        {
            return NotFound(
                new
                {
                    message = "Technician does not exists"
                }
            );
        }

        if (technician.Role == null || technician.Role.Name != UserRoles.Technician)
        {
            return BadRequest(
                new
                {
                    message = "Assigned user must be a technician"
                }
            );
        }

        if (technician.DepartmentId != targetDeptId)
        {
            return BadRequest(
                new
                {
                    message = "Assigned technician must belong to the maintenance department for this equipment"
                }
            );
        }

        if (!technician.IsActive)
        {
            return BadRequest(
                new
                {
                    message = "Technician account is inactive"
                });
        }

        User? supportTechnician = null;
        if (request.SupportTechnicianId.HasValue)
        {
            supportTechnician = await _context.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(u => u.Id == request.SupportTechnicianId.Value);

            if (supportTechnician is null)
            {
                return NotFound(
                    new
                    {
                        message = "Support technician does not exists"
                    }
                );
            }

            if (supportTechnician.Role == null || supportTechnician.Role.Name != UserRoles.Technician)
            {
                return BadRequest(
                    new
                    {
                        message = "Support user must be a technician"
                    }
                );
            }

            if (supportTechnician.DepartmentId != targetDeptId)
            {
                return BadRequest(
                    new
                    {
                        message = "Support technician must belong to the maintenance department for this equipment"
                    }
                );
            }

            if (!supportTechnician.IsActive)
            {
                return BadRequest(
                    new
                    {
                        message = "Support technician account is inactive"
                    }
                );
            }

            if (supportTechnician.Id == technician.Id)
            {
                return BadRequest(
                    new
                    {
                        message = "Support technician must be different from the main technician"
                    }
                );
            }
        }

        var oldStatus = ticket.Status;
        // Neu technician khong thay doi thi khong can cap nhat.
        // Tranh tao history moi khi assign dung nguoi hien tai.
        if (ticket.AssignedTechnicianId == request.AssignedTechnicianId)
        {
            return BadRequest(
                new
                {
                    message = "Technician already assigned"
                }
            );
        }
        // Cap nhat technician phu trach ticket.
        ticket.AssignedTechnicianId = request.AssignedTechnicianId;
        ticket.SupportTechnicianId = request.SupportTechnicianId;
        ticket.Status = TicketStatuses.Assigned;

        // Ghi lai lich su assign.
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatuses.Assigned,

            // Lay user hien tai tu CurrentUserService.
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Note = request.Note
        };

        _context.TicketStatusHistories.Add(history);

        // Luu thay doi ticket va history xuong database.
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
            .Where(t => t.Id == id)
            .Select(t => new MaintenanceTicketResponse
            {
                Id = t.Id,
                TicketCode = t.TicketCode,
                Title = t.Title,
                Description = t.Description,

                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment.Name,
                EquipmentDepartmentId = t.Equipment.DepartmentId,
                EquipmentMaintenanceDepartmentId = t.Equipment.MaintenanceDepartmentId,

                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser!.FullName,

                AssignedTechnicianId = t.AssignedTechnicianId,
                AssignedTechnicianName = t.AssignedTechnician != null
                    ? t.AssignedTechnician.FullName
                    : null,
                SupportTechnicianId = t.SupportTechnicianId,
                SupportTechnicianName = t.SupportTechnician != null
                    ? t.SupportTechnician.FullName
                    : null,

                Priority = t.Priority,
                Status = t.Status,
                ResolutionNote = t.ResolutionNote,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt,
                DueAt = t.DueAt,
                SlaStatus = t.SlaStatus

            }).FirstAsync();
        return Ok(response);
    }

    [Authorize]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<MaintenanceTicketResponse>> TransitionTicketStatus(int id, ChangeTicketStatusRequest request)
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .Include(t => t.CreatedByUser)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                });
        }

        // Ticket luon gan voi mot thiet bi.
        // Trang thai thiet bi phai dong bo voi trang thai ticket.
        var equipment = ticket.Equipment;
        if (equipment is null)
        {
            return NotFound(
                new
                {
                    message = "Equipment not found"
                }
            );
        }
        var newStatus = request.Status.Trim();
        // Chi cho phep cac status nam trong workflow xu ly.
        // Khong cho client gui status tuy y.
        if (!new[]
            {
                TicketStatuses.InProgress,
                TicketStatuses.Resolved,
                TicketStatuses.Closed,
                TicketStatuses.Cancelled
            }
            .Contains(newStatus))
        {
            return BadRequest(
                new
                {
                    message = "Invalid ticket status"
                }
            );
        }

        // Ticket da Closed nghia la quy trinh da ket thuc.
        // Khong cho doi trang thai nua de tranh sai lich su.

        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(
                new
                {
                    message = "Finalized ticket cannot be updated"
                }
            );
        }

        // Kiem tra chuyen trang thai co dung workflow khong.
        // Chi cho di tung buoc, khong cho nhay coc.

        if (!TicketWorkflowRules.CanTransition(ticket.Status, newStatus))
        {
            return BadRequest(
                new
                {
                    message = $"Cannot change ticket from {ticket.Status} to {newStatus}"
                }
            );
        }
        // Muon chuyen sang InProgress thi ticket phai duoc assign cho technician truoc.
        // Neu chua co AssignedTechnicianId thi khong xac dinh duoc ai dang phu trach.

        if (newStatus == TicketStatuses.InProgress && ticket.AssignedTechnicianId is null)
        {
            return BadRequest(
                new
                {
                    message = "Ticket must be assigned before moving to InProgress"
                }
            );
        }

        // Chi technician duoc assign moi duoc cap nhat trang thai xu ly.
        if (newStatus == TicketStatuses.InProgress ||
        newStatus == TicketStatuses.Resolved)
        {
            if (ticket.AssignedTechnicianId != userId)
            {
                return Forbid();
            }
        }

        // Khi technician bao da xu ly xong, bat buoc phai co resolution note.
        if (newStatus == TicketStatuses.Resolved && string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            return BadRequest(
                new
                {
                    message = "Resolution note is required when resolving a ticket"
                }
            );
        }

        var oldStatus = ticket.Status;



        // Cap nhat trang thai hien tai cua ticket.
        ticket.Status = newStatus;

        if (newStatus == TicketStatuses.InProgress)
        {
            equipment.Status = EquipmentStatuses.UnderMaintenance;
        }

        if (newStatus == TicketStatuses.Resolved)
        {
            ticket.ResolutionNote = request.ResolutionNote?.Trim();
            ticket.ResolvedAt = DateTime.UtcNow;
            
            if (ticket.DueAt.HasValue)
            {
                ticket.SlaStatus = ticket.ResolvedAt <= ticket.DueAt.Value ? SlaPolicy.MetSLA : SlaPolicy.MissedSLA;
            }
        }

        if (newStatus == TicketStatuses.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
            equipment.Status = EquipmentStatuses.Active;
        }

        if (newStatus == TicketStatuses.Cancelled)
        {
            equipment.Status = EquipmentStatuses.Active;
        }

        // Requester, Admin hoac Manager cua phong ban so huu thiet bi duoc phep dong ticket.
        if (newStatus == TicketStatuses.Closed)
        {
            var canClose = TicketAccessPolicy.CanCloseOrCancel(ticket, _currentUserService);

            if (!canClose)
            {
                return Forbid();
            }
        }

        // Requester, Admin hoac Manager duoc phep huy ticket.
        if (newStatus == TicketStatuses.Cancelled)
        {
            var canCancel = TicketAccessPolicy.CanCloseOrCancel(ticket, _currentUserService);

            if (!canCancel)
            {
                return Forbid();
            }
        }

        // Ghi lai lich su doi trang thai.
        // MaintenanceTicket luu trang thai hien tai.
        // TicketStatusHistory luu timeline tung lan thay doi.
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Note = request.Note?.Trim()
        };

        _context.TicketStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
        .Where(t => t.Id == id)
        .Select(
            t => new MaintenanceTicketResponse
            {
                Id = t.Id,
                TicketCode = t.TicketCode,
                Title = t.Title,
                Description = t.Description,

                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment.Name,
                EquipmentDepartmentId = t.Equipment.DepartmentId,
                EquipmentMaintenanceDepartmentId = t.Equipment.MaintenanceDepartmentId,

                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser!.FullName,

                AssignedTechnicianId = t.AssignedTechnicianId,
                AssignedTechnicianName = t.AssignedTechnician != null
                ? t.AssignedTechnician.FullName
                : null,
                SupportTechnicianId = t.SupportTechnicianId,
                SupportTechnicianName = t.SupportTechnician != null
                ? t.SupportTechnician.FullName
                : null,

                Priority = t.Priority,
                Status = t.Status,
                ResolutionNote = t.ResolutionNote,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt,
                DueAt = t.DueAt,
                SlaStatus = t.SlaStatus
            }
        ).FirstAsync();

        return Ok(response);
    }

    [Authorize]
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateComment(int id, CreateTicketCommentRequest request)
    {
        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                }
            );
        }

        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(
                new
                {
                    message = "Cannot comment on finalized ticket"
                }
            );
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
        {
            return Forbid();
        }

        // Lay thong tin nguoi dung dang nhap.
        // UserId lay tu JWT token thong qua CurrentUserService.
        // Can FullName de tra ve TicketCommentResponse.

        var currentUser = await _context.Users
        // Chi doc du lieu, khong cap nhat nen dung AsNoTracking.
        .AsNoTracking()
        .Where(user => user.Id == userId)
        // Chi lay cac field can dung.
        // Tranh load toan bo thong tin User khong can thiet.
        .Select(user => new
        {
            user.FullName
        }).FirstAsync();

        var comment = new TicketComment
        {
            MaintenanceTicketId = ticket.Id,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        var response = new TicketCommentResponse
        {
            Id = comment.Id,
            UserId = userId,
            UserName = currentUser.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetCommentById),
            new { id = comment.Id },
            response
        );
    }

    [Authorize]
    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<List<TicketCommentResponse>>> GetCommentById(int id)
    {
        // Kiem tra ticket co ton tai va nguoi dung co quyen xem hay khong.

        var ticketQuery = _context.MaintenanceTickets
        .AsNoTracking()
        .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticketExists = await ticketQuery.AnyAsync(
            ticket => ticket.Id == id
        );
        if (!ticketExists)
        {
            return NotFound(
            new
            {
                message = "Ticket not found"
            });
        }
        var comments = await _context.TicketComments
        .AsNoTracking()
        .Where(comment => comment.MaintenanceTicketId == id)
        .OrderBy(comment => comment.CreatedAt)
        .Select(comment => new TicketCommentResponse
        {
            Id = comment.Id,
            UserId = comment.UserId,
            UserName = comment.User!.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        }).ToListAsync();

        return Ok(comments);
    }
    [Authorize]
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<TicketStatusHistoryResponse>>> GetTicketHistory(int id)
    {
        var ticketQuery = _context.MaintenanceTickets.AsQueryable();
        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticketExists = await ticketQuery.AnyAsync(t => t.Id == id);
        if (!ticketExists)
        {
            return BadRequest(
                new
                {
                    message = "Ticket not found"
                }
            );
        }
        // Lay danh sach lich su trang thai cua ticket.
        // Sap xep theo ChangedAt tang dan de xem timeline tu cu den moi.
        var histories = await _context.TicketStatusHistories
        .Where(history => history.MaintenanceTicketId == id)
        .OrderBy(history => history.ChangedAt)
        .Select(history => new TicketStatusHistoryResponse
        {
            Id = history.Id,
            MaintenanceTicketId = history.MaintenanceTicketId,
            OldStatus = history.OldStatus,
            NewStatus = history.NewStatus,
            ChangedByUserId = history.ChangedByUserId,
            ChangedByUserName = history.ChangedByUser!.FullName,
            ChangedAt = history.ChangedAt,
            Note = history.Note
        }).ToListAsync();

        return Ok(histories);
    }
}

