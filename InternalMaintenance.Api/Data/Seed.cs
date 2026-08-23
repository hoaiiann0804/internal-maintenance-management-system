using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Vendors;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Data;

public static class SeedData
{
    private const string TemporaryPassword = "Temp@123456";
    public static async Task InitializeAsync(AppDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedDepartmentsAsync(context);
        await SeedEquipmentAsync(context);
        await SeedAuthUsersAsync(context);
        await SeedVendorsAsync(context);
    }

    private static async Task SeedVendorsAsync(AppDbContext context)
    {
        if (await context.Vendors.AnyAsync()) return;

        context.Vendors.AddRange(
            new Vendor
            {
                Name = "Daikin Việt Nam (Trung Tâm Bảo Hành)",
                ContactPerson = "Nguyễn Văn Hùng",
                Phone = "18006777",
                Email = "baohanh@daikin.com.vn",
                Address = "184 Cao Thắng, Phường 12, Quận 10, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Dell Vietnam Care Center",
                ContactPerson = "Trần Thị Mỹ",
                Phone = "1800545455",
                Email = "support@dellservice.vn",
                Address = "23 Nguyễn Thị Minh Khai, Quận 1, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Samsung Service Center",
                ContactPerson = "Lê Hoàng Nam",
                Phone = "1800588889",
                Email = "services@samsung.com",
                Address = "99 Nguyễn Huệ, Quận 1, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Phong Vũ Computer & Repairs",
                ContactPerson = "Phạm Quốc Tuấn",
                Phone = "18006868",
                Email = "suachua@phongvu.vn",
                Address = "264 Nguyễn Thị Minh Khai, Quận 3, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();
    }
    private static async Task SeedRolesAsync(AppDbContext context)
    {
        await EnsureRoleAsync(context, "Admin");
        await EnsureRoleAsync(context, "Manager");
        await EnsureRoleAsync(context, "Staff");
        await EnsureRoleAsync(context, "Technician");
        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(AppDbContext context, string roleName)
    {
        var roleExists = await context.Roles
        .AnyAsync(role => role.Name == roleName);
        if (roleExists)
        {
            return;
        }
        context.Roles.Add(new Role
        {
            Name = roleName
        });

    }

    private static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        if (await context.Departments.AnyAsync())
        {
            return;
        }

        context.Departments.AddRange(
            new Department
            {
                Name = "IT",
                Description = "Information Technology Department",
                IsMaintenanceTeam = true
            },
            new Department
            {
                Name = "Accounting",
                Description = "Accounting Department",
                IsMaintenanceTeam = false
            },
            new Department
            {
                Name = "HR",
                Description = "Human Resources Department",
                IsMaintenanceTeam = false
            }
        );

        await context.SaveChangesAsync();
    }

    private static async Task SeedEquipmentAsync(AppDbContext context)
    {
        var accountingDepartment = await context.Departments
            .FirstOrDefaultAsync(d => d.Name == "Accounting");
        var itDepartment = await context.Departments
            .FirstOrDefaultAsync(d => d.Name == "IT");

        if (accountingDepartment is null || itDepartment is null)
        {
            return;
        }

        await EnsureEquipmentAsync(
            context,
            code: "PRN-ACC-001",
            name: "Canon Printer - Accounting Room",
            departmentId: accountingDepartment.Id,
            maintenanceDepartmentId: itDepartment.Id,
            purchasedDate: new DateTime(2025, 1, 10),
            description: "Main printer used by accounting department");

        await EnsureEquipmentAsync(
            context,
            code: "RTR-IT-001",
            name: "Main Office Router",
            departmentId: itDepartment.Id,
            maintenanceDepartmentId: itDepartment.Id,
            purchasedDate: new DateTime(2024, 8, 15),
            description: "Router used for internal office network");

        await context.SaveChangesAsync();
    }

    private static async Task EnsureEquipmentAsync(
        AppDbContext context,
        string code,
        string name,
        int departmentId,
        int? maintenanceDepartmentId,
        DateTime purchasedDate,
        string description)
    {
        var equipment = await context.Equipment
            .FirstOrDefaultAsync(equipment => equipment.Code == code);

        if (equipment is null)
        {
            context.Equipment.Add(new Equipment
            {
                Code = code,
                Name = name,
                DepartmentId = departmentId,
                MaintenanceDepartmentId = maintenanceDepartmentId,
                Status = "Active",
                PurchasedDate = purchasedDate,
                Description = description
            });
            return;
        }

        equipment.DepartmentId = departmentId;
        equipment.MaintenanceDepartmentId = maintenanceDepartmentId ?? equipment.MaintenanceDepartmentId;
        equipment.PurchasedDate = purchasedDate;
        equipment.Description = description;
    }

    public static async Task SeedAuthUsersAsync(AppDbContext context)
    {
        var adminRole = await context.Roles
        .FirstAsync(role => role.Name == "Admin");

        var managerRole = await context.Roles
        .FirstAsync(role => role.Name == "Manager");

        var staffRole = await context.Roles
        .FirstAsync(role => role.Name == "Staff");

        var technicianRole = await context.Roles
        .FirstAsync(role => role.Name == "Technician");

        var itDepartment = await context.Departments
        .FirstOrDefaultAsync(department => department.Name == "IT");

        var accountingDepartment = await context.Departments
       .FirstOrDefaultAsync(department => department.Name == "Accounting");

        await EnsureUserAsync(
            context: context,
            fullName: "System Admin",
            email: "admin@test.com",
            roleId: adminRole.Id,
            departmentId: null
        );

        await EnsureUserAsync(
           context: context,
           fullName: "Manager Test",
           email: "manager@test.com",
           roleId: managerRole.Id,
           departmentId: itDepartment?.Id
       );
        await EnsureUserAsync(
           context: context,
           fullName: "Staff Test",
           email: "staff@test.com",
           roleId: staffRole.Id,
           departmentId: accountingDepartment?.Id
       );
        await EnsureUserAsync(
          context: context,
          fullName: "Technician Test",
          email: "technician@test.com",
          roleId: technicianRole.Id,
          departmentId: itDepartment?.Id
      );
        await context.SaveChangesAsync();
    }
    private static async Task EnsureUserAsync(
        AppDbContext context,
        string fullName,
        string email,
        int roleId,
        int? departmentId)
    {
        var user = await context.Users
        .FirstOrDefaultAsync(existingUser => existingUser.Email == email);

        if (user is null)
        {
            context.Users.Add(
                new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(TemporaryPassword),
                    RoleId = roleId,
                    DepartmentId = departmentId,
                    IsActive = true,
                    MustChangePassword = true,
                    LastLoginAt = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                }
            );
            return;
        }
        //User đã tồn tại từ dữ liệu cũ
        //Giữ nguyên Id không ảnh hưởng các ticket đang liên kết với user này.
        user.FullName = string.IsNullOrWhiteSpace(user.FullName)
        ? fullName : user.FullName;

        user.RoleId = roleId;
        if (departmentId.HasValue)
        {
            user.DepartmentId = departmentId;
        }

        // Không tự động set IsActive = true cho user đã tồn tại,
        // Vì nếu Admin khóa user thì seed không nên tự mở lại 
        // user.IsActive = true;

        // CHỉ cập nhật password cũ chưa phải BCrypt hash.
        // việc này tránh reset password mỗi lần app chạy
        if (!IsBCryptHash(user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(TemporaryPassword);
            user.MustChangePassword = true;
        }
        user.UpdatedAt = DateTime.UtcNow;
    }
    private static bool IsBCryptHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }
        return passwordHash.StartsWith("$2a$")
           || passwordHash.StartsWith("$2b$")
           || passwordHash.StartsWith("$2y$");
    }

}
