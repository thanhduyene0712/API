using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Database_UPODContext : DbContext
    {
        public Database_UPODContext()
        {
        }

        public Database_UPODContext(DbContextOptions<Database_UPODContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; } = null!;
        public virtual DbSet<Admin> Admins { get; set; } = null!;
        public virtual DbSet<Agency> Agencies { get; set; } = null!;
        public virtual DbSet<Area> Areas { get; set; } = null!;
        public virtual DbSet<Contract> Contracts { get; set; } = null!;
        public virtual DbSet<ContractService> ContractServices { get; set; } = null!;
        public virtual DbSet<Customer> Customers { get; set; } = null!;
        public virtual DbSet<Device> Devices { get; set; } = null!;
        public virtual DbSet<DeviceType> DeviceTypes { get; set; } = null!;
        public virtual DbSet<Image> Images { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<MaintenanceReport> MaintenanceReports { get; set; } = null!;
        public virtual DbSet<MaintenanceReportService> MaintenanceReportServices { get; set; } = null!;
        public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; } = null!;
        public virtual DbSet<Request> Requests { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<Service> Services { get; set; } = null!;
        public virtual DbSet<Skill> Skills { get; set; } = null!;
        public virtual DbSet<Technician> Technicians { get; set; } = null!;
        public virtual DbSet<RequestDevice> RequestDevices { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=localhost,1433;Initial Catalog=Database_UPOD;User ID=sa;Password=THANHDUYEN07121999;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("Account");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Password)
                    .HasMaxLength(2000)
                    .IsUnicode(false);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.Property(e => e.Username).HasMaxLength(2000);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("AccountRole");
            });

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admin");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Address).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Mail).HasMaxLength(2000);

                entity.Property(e => e.Name).HasMaxLength(2000);

                entity.Property(e => e.Telephone).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Admins)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_Admin_Account");
            });

            modelBuilder.Entity<Agency>(entity =>
            {
                entity.ToTable("Agency");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AgencyName).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.ManagerName).HasMaxLength(2000);

                entity.Property(e => e.Telephone)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.Agencies)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_Agency_Area");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Agencies)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("AgencyAccount");

                entity.HasOne(d => d.Technician)
                    .WithMany(p => p.Agencies)
                    .HasForeignKey(d => d.TechnicianId)
                    .HasConstraintName("FK_Agency_Technician");
            });

            modelBuilder.Entity<Area>(entity =>
            {
                entity.ToTable("Area");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AreaName).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Contract>(entity =>
            {
                entity.ToTable("Contract");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Attachment).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.ContractName).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.RejectReason).HasMaxLength(2000);

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.Property(e => e.TerminalContent).HasMaxLength(2000);

                entity.Property(e => e.TerminalTime).HasColumnType("datetime");

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("ContractCompany");
            });

            modelBuilder.Entity<ContractService>(entity =>
            {
                entity.ToTable("ContractService");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Contract)
                    .WithMany(p => p.ContractServices)
                    .HasForeignKey(d => d.ContractId)
                    .HasConstraintName("ContractServiceITSupporterContract");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.ContractServices)
                    .HasForeignKey(d => d.ServiceId)
                    .HasConstraintName("ContractServiceITSupporterServiceItSupport");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Address).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.Mail).HasMaxLength(2000);

                entity.Property(e => e.Name).HasMaxLength(2000);

                entity.Property(e => e.Phone).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Customers)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_Company_Account");
            });

            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("Device");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.DeviceName).HasMaxLength(2000);

                entity.Property(e => e.GuarantyEndDate).HasColumnType("datetime");

                entity.Property(e => e.GuarantyStartDate).HasColumnType("datetime");

                entity.Property(e => e.Other).HasMaxLength(2000);

                entity.Property(e => e.SettingDate).HasColumnType("datetime");

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Agency)
                    .WithMany(p => p.Devices)
                    .HasForeignKey(d => d.AgencyId)
                    .HasConstraintName("FK_Device_Agency");

                entity.HasOne(d => d.DeviceType)
                    .WithMany(p => p.Devices)
                    .HasForeignKey(d => d.DeviceTypeId)
                    .HasConstraintName("DeviceDeviceType");
            });

            modelBuilder.Entity<DeviceType>(entity =>
            {
                entity.ToTable("DeviceType");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.DeviceTypeName).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.DeviceTypes)
                    .HasForeignKey(d => d.ServiceId)
                    .HasConstraintName("DeviceTypeServiceITSupport");
            });


            modelBuilder.Entity<Image>(entity =>
            {
                entity.ToTable("Image");

                entity.Property(e => e.Link).HasMaxLength(2000);

                entity.Property(e => e.ObjectName).HasMaxLength(2000);

                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notification");

                entity.Property(e => e.NotificationContent).HasMaxLength(2000);

                entity.Property(e => e.ObjectName).HasMaxLength(2000);

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<MaintenanceReport>(entity =>
            {
                entity.ToTable("MaintenanceReport");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(2000);

                entity.Property(e => e.Status).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Agency)
                    .WithMany(p => p.MaintenanceReports)
                    .HasForeignKey(d => d.AgencyId)
                    .HasConstraintName("FK_MaintenanceReport_Agency");

                entity.HasOne(d => d.CreateByNavigation)
                    .WithMany(p => p.MaintenanceReports)
                    .HasForeignKey(d => d.CreateBy)
                    .HasConstraintName("FK_MaintenanceReport_Technician");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.MaintenanceReports)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK_MaintenanceReport_Customer");

                entity.HasOne(d => d.MaintenanceSchedule)
                    .WithMany(p => p.MaintenanceReports)
                    .HasForeignKey(d => d.MaintenanceScheduleId)
                    .HasConstraintName("FK_MaintenanceReport_MaintenanceSchedule1");
            });

            modelBuilder.Entity<MaintenanceReportService>(entity =>
            {
                entity.ToTable("MaintenanceReportService");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.HasOne(d => d.MaintenanceReport)
                    .WithMany(p => p.MaintenanceReportServices)
                    .HasForeignKey(d => d.MaintenanceReportId)
                    .HasConstraintName("FK_MaintenanceReportService_MaintenanceReport1");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.MaintenanceReportServices)
                    .HasForeignKey(d => d.ServiceId)
                    .HasConstraintName("FK_MaintenanceReportService_Service");
            });

            modelBuilder.Entity<MaintenanceSchedule>(entity =>
            {
                entity.ToTable("MaintenanceSchedule");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.MaintainTime).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(2000);

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.Property(e => e.Status).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Agency)
                    .WithMany(p => p.MaintenanceSchedules)
                    .HasForeignKey(d => d.AgencyId)
                    .HasConstraintName("FK_MaintenanceSchedule_Agency");

                entity.HasOne(d => d.Contract)
                    .WithMany(p => p.MaintenanceSchedules)
                    .HasForeignKey(d => d.ContractId)
                    .HasConstraintName("FK_MaintenanceSchedule_Contract");

                entity.HasOne(d => d.Technician)
                    .WithMany(p => p.MaintenanceSchedules)
                    .HasForeignKey(d => d.TechnicianId)
                    .HasConstraintName("FK_MaintenanceSchedule_Technician");
            });

            modelBuilder.Entity<Request>(entity =>
            {
                entity.ToTable("Request");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CancelReason).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CurrentTechnicianId).HasColumnName("CurrentTechnician_Id");

                entity.Property(e => e.EndTime).HasColumnType("datetime");

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Admin)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.AdminId)
                    .HasConstraintName("FK_Request_Admin");

                entity.HasOne(d => d.Agency)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.AgencyId)
                    .HasConstraintName("FK_Request_Agency");

                entity.HasOne(d => d.Contract)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.ContractId)
                    .HasConstraintName("FK_Request_Contract");

                entity.HasOne(d => d.CurrentTechnician)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.CurrentTechnicianId)
                    .HasConstraintName("FK_Request_ITSupporter");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK_Request_Company");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.ServiceId)
                    .HasConstraintName("FK_Request_Service");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.RoleName)
                    .HasMaxLength(2000)
                    .IsUnicode(false);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.ToTable("Service");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.ServiceName).HasMaxLength(2000);

                entity.Property(e => e.Guideline).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Skill>(entity =>
            {
                entity.ToTable("Skill");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.Skills)
                    .HasForeignKey(d => d.ServiceId)
                    .HasConstraintName("SkillServiceITSupport");

                entity.HasOne(d => d.Technician)
                    .WithMany(p => p.Skills)
                    .HasForeignKey(d => d.TechnicianId)
                    .HasConstraintName("SkillITSupporter");
            });

            modelBuilder.Entity<Technician>(entity =>
            {
                entity.ToTable("Technician");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Address).HasMaxLength(2000);

                entity.Property(e => e.Code).HasMaxLength(2000);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Email)
                    .HasMaxLength(2000)
                    .IsUnicode(false);

                entity.Property(e => e.TechnicianName).HasMaxLength(2000);

                entity.Property(e => e.Telephone)
                    .HasMaxLength(2000)
                    .IsUnicode(false);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Technicians)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("ITSupporterAccount");

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.Technicians)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("ITSupporterDepartment");
            });

            modelBuilder.Entity<RequestDevice>(entity =>
            {
                entity.ToTable("RequestDevice");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Solution).HasMaxLength(2000);

                entity.Property(e => e.UpdateDate).HasColumnType("datetime");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.RequestDevices)
                    .HasForeignKey(d => d.DeviceId)
                    .HasConstraintName("TicketDevice");

                entity.HasOne(d => d.Request)
                    .WithMany(p => p.RequestDevices)
                    .HasForeignKey(d => d.RequestId)
                    .HasConstraintName("FK_Ticket_Request");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
