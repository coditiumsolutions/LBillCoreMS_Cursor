using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BMSBT.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BMSBT.Models;

public partial class BmsbtContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private bool _isSavingAudit;

    public BmsbtContext()
    {
    }

    public BmsbtContext(DbContextOptions<BmsbtContext> options)
        : base(options)
    {
    }

    public BmsbtContext(DbContextOptions<BmsbtContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<TwoMonthOutstandingBill> TwoMonthOutstandingBills { get; set; }
    public DbSet<SpecialDiscountBill> SpecialDiscountBills { get; set; }
    public DbSet<DashboardStatisticsResult> DashboardStatistics { get; set; }
    public DbSet<BillingReportData> BillingReportData { get; set; }

   

    public virtual DbSet<CustomersMaintenance> CustomersMaintenance { get; set; }
    public virtual DbSet<Configuration> Configurations { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomersDetail> CustomersDetails { get; set; }

    public virtual DbSet<ElectricityBill> ElectricityBills { get; set; }

    public virtual DbSet<MaintenanceBill> MaintenanceBills { get; set; }

    public virtual DbSet<MaintenanceTarrif> MaintenanceTarrifs { get; set; }

    public virtual DbSet<MeterType> MeterTypes { get; set; }

    public virtual DbSet<OperatorsSetup> OperatorsSetups { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ReadingSheet> ReadingSheets { get; set; }

    public virtual DbSet<TariffMaint> TariffMaints { get; set; }

    public virtual DbSet<Tarrif> Tarrifs { get; set; }

    public virtual DbSet<TaxInformation> TaxInformations { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public DbSet<Fine> Fine { get; set; }
    public DbSet<AdditionalCharge> AdditionalCharges { get; set; }
    public DbSet<Adjustment> Adjustments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=172.20.228.2;Database=BMSBT;User ID=sa;Password=Pakistan@786;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Configur__DD701264E9989C35");

            entity.ToTable("Configuration");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.ConfigKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConfigValue)
                .HasMaxLength(100)
                .IsUnicode(false);
        });



        // Configure BillingReportData as keyless entity
        modelBuilder.Entity<BillingReportData>().HasNoKey();

        modelBuilder.Entity<DashboardStatisticsResult>().HasNoKey();
        
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.MeterNumber, "UQ__Customer__999CDEB3036F9F25").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.ConnectionDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.MeterNumber).HasMaxLength(50);
            entity.Property(e => e.OutstandingBalance)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.TariffName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ZipCode).HasMaxLength(10);
        });

        modelBuilder.Entity<CustomersDetail>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Customer__DD701264AF0F1179");

            entity.ToTable("CustomersDetail");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.BankNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.BillStatus).IsUnicode(false);
            entity.Property(e => e.BillStatusMaint).IsUnicode(false);
            entity.Property(e => e.Block)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Btno)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("BTNo");
            entity.Property(e => e.BtnoMaintenance)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("BTNoMaintenance");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cnicno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("CNICNo");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(70)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.CustomerNo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FatherName)
                .HasMaxLength(70)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.GeneratedMonthYear)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InstalledOn)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.LocationSeqNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.MeterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MobileNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Ntnnumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NTNNumber");
            entity.Property(e => e.PloNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PlotType).HasMaxLength(50);
            entity.Property(e => e.Project)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sector)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.SubProject)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TariffName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TelephoneNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<ElectricityBill>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Electric__DD70126479D95C7E");

            entity.HasIndex(e => e.InvoiceNo, "UQ__Electric__D796B2279C42DA06").IsUnique();

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.BankDetail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BillingDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BillingMonth)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BillingYear)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Btno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BTNo");
            entity.Property(e => e.CreateOn)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CustomerNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EnergyCoast)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Furthertax).HasColumnName("FURTHERTAX");
            entity.Property(e => e.Gst).HasColumnName("GST");
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdated)
                .HasMaxLength(100)             // match varchar(100)
                .HasColumnType("varchar(100)") // explicitly set varchar type
                .HasDefaultValueSql("CONVERT(varchar(10), GETDATE(), 120)");  // default to current date as string yyyy-MM-dd
            entity.Property(e => e.MeterNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MeterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Opc)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("OPC");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.Ptvfee).HasColumnName("PTVFEE");
            
        });

       

        modelBuilder.Entity<MaintenanceTarrif>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("MaintenanceTarrif");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Category).IsUnicode(false);
            entity.Property(e => e.History).IsUnicode(false);
            entity.Property(e => e.Project)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Size).HasMaxLength(100);
            entity.Property(e => e.Tax)
                .HasMaxLength(10)
                .IsFixedLength();
        });

        // MaintenanceBills: DB uses decimal for MaintCharges and Arrears; model uses int? for bill math UX.
        modelBuilder.Entity<MaintenanceBill>(entity =>
        {
            entity.ToTable("MaintenanceBills");

            entity.Property(e => e.MaintCharges)
                .HasColumnType("decimal(18,0)")
                .HasConversion(
                    v => v.HasValue ? (decimal)v.Value : (decimal?)null,
                    v => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : (int?)null);

            entity.Property(e => e.Arrears)
                .HasColumnType("decimal(18,1)")
                .HasConversion(
                    v => v.HasValue ? (decimal)v.Value : (decimal?)null,
                    v => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : (int?)null);
        });

        modelBuilder.Entity<MeterType>(entity =>
        {
            entity.HasKey(e => e.MeterId).HasName("PK__MeterTyp__59223BAC81CB4E1A");

            entity.ToTable("MeterType");

            entity.Property(e => e.MeterId).ValueGeneratedNever();
            entity.Property(e => e.Btno)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("BTNo");
            entity.Property(e => e.MeterNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MeterType1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MeterType");
            entity.Property(e => e.MultiplyFactor).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Uid)
                .ValueGeneratedOnAdd()
                .HasColumnName("uid");
        });

        modelBuilder.Entity<OperatorsSetup>(entity =>
        {
            entity.HasKey(e => e.OperatorID);

            entity.ToTable("OperatorsSetup");

            entity.Property(e => e.OperatorID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OperatorID");
            entity.Property(e => e.BankName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BillingMonth)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BillingYear)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OperatorName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Uid)
                .ValueGeneratedOnAdd()
                .HasColumnName("uid");
        });

        modelBuilder.Entity<ReadingSheet>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_ReadingSheet_1");

            entity.ToTable("ReadingSheet");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Btno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BTNo");
            entity.Property(e => e.CustomerNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MeterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Month)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TarrifName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Year)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TariffMaint>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("TariffMaint");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.TariffPrefix)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TariffSize)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TariffType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Tarrif>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("Tarrif");

            entity.HasIndex(e => new { e.TarrifName, e.MeterType }, "IDX_Tariffs_Composite");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Details)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MeterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MinCharges).HasColumnName("Min Charges");
            entity.Property(e => e.Nm).HasColumnName("NM");
            entity.Property(e => e.Range)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sequence).HasMaxLength(50);
            entity.Property(e => e.Slab)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TarrifName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TarrifType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TaxInformation>(entity =>
        {
            entity.HasKey(e => e.TaxId).HasName("PK__TaxInfor__711BE0ACE059CFCB");

            entity.ToTable("TaxInformation");

            entity.Property(e => e.ApplicableFor)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IsActive)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Range)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TaxName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.TaxRate)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Users__DD7012643949B123");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<AdditionalCharge>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("AdditionalCharges");

            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.BTNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ChargesName)
                .HasMaxLength(100)
                .IsUnicode(false);
            // Map ChargesAmountInt to the database column (which is int)
            entity.Property(e => e.ChargesAmountInt)
                .HasColumnName("ChargesAmount")
                .HasColumnType("int");
            
            // ChargesAmount is a computed property, so ignore it in mapping
            entity.Ignore(e => e.ChargesAmount);
        });

        modelBuilder.Entity<Adjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId);

            entity.ToTable("Adjustments");

            entity.Property(e => e.AdjustmentId).HasColumnName("AdjustmentId");
            entity.Property(e => e.BTNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BillingType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdjustmentName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdjustmentType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdjustmentValue)
                .HasColumnType("int");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (_isSavingAudit || ShouldSkipEfAudit())
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var pendingAuditEntries = BuildPendingAuditEntries();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (pendingAuditEntries.Count > 0)
        {
            await PersistAuditEntriesSafelyAsync(pendingAuditEntries, cancellationToken);
        }

        return result;
    }

    private bool ShouldSkipEfAudit()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.Items == null)
        {
            return false;
        }

        return httpContext.Items.TryGetValue("SkipEfAudit", out var skipValue)
               && skipValue is bool shouldSkip
               && shouldSkip;
    }

    private List<PendingAuditEntry> BuildPendingAuditEntries()
    {
        ChangeTracker.DetectChanges();

        var auditEntries = new List<PendingAuditEntry>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            if (entry.Metadata.IsOwned())
            {
                continue;
            }

            if (entry.State != EntityState.Added && entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
            {
                continue;
            }

            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            if (entry.State == EntityState.Added && tableName.Equals("MaintenanceBills", StringComparison.OrdinalIgnoreCase))
            {
                // Bulk bill creation can be very large; handled through one summarized log entry.
                continue;
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                var propertyName = property.Metadata.Name;
                var originalValue = NormalizeValue(property.OriginalValue);
                var currentValue = NormalizeValue(property.CurrentValue);

                switch (entry.State)
                {
                    case EntityState.Added:
                        if (currentValue != null)
                        {
                            newValues[propertyName] = currentValue;
                        }
                        break;
                    case EntityState.Deleted:
                        if (originalValue != null)
                        {
                            oldValues[propertyName] = originalValue;
                        }
                        break;
                    case EntityState.Modified:
                        if (!property.IsModified || Equals(originalValue, currentValue))
                        {
                            continue;
                        }

                        oldValues[propertyName] = originalValue;
                        newValues[propertyName] = currentValue;
                        break;
                }
            }

            if (entry.State == EntityState.Modified && oldValues.Count == 0 && newValues.Count == 0)
            {
                continue;
            }

            auditEntries.Add(new PendingAuditEntry
            {
                Entry = entry,
                TableName = tableName,
                Operation = GetOperation(entry.State),
                RecordId = GetRecordId(entry),
                OldData = oldValues.Count == 0 ? null : oldValues,
                NewData = newValues.Count == 0 ? null : newValues
            });
        }

        return auditEntries;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd"),
            TimeOnly timeOnly => timeOnly.ToString("HH:mm:ss"),
            DateTime dateTime => dateTime.ToString("O"),
            _ => value
        };
    }

    private static string GetOperation(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };
    }

    private static string? GetRecordId(EntityEntry entry)
    {
        // Prefer business identifiers where available, especially BTNo/Btno used across billing flows.
        var btNoValue = TryGetPropertyValue(entry, "BTNo") ?? TryGetPropertyValue(entry, "Btno");
        if (!string.IsNullOrWhiteSpace(btNoValue))
        {
            return btNoValue;
        }

        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null)
        {
            return null;
        }

        var keyValues = primaryKey.Properties
            .Select(pk => entry.Property(pk.Name))
            .Select(p => p.CurrentValue ?? p.OriginalValue)
            .Where(v => v != null)
            .Select(v => v!.ToString())
            .Where(v => !string.IsNullOrWhiteSpace(v));

        return string.Join(",", keyValues!);
    }

    private static string? TryGetPropertyValue(EntityEntry entry, string propertyName)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (prop == null)
        {
            return null;
        }

        var value = prop.CurrentValue ?? prop.OriginalValue;
        return value?.ToString();
    }

    private async Task PersistAuditEntriesSafelyAsync(List<PendingAuditEntry> pendingEntries, CancellationToken cancellationToken)
    {
        try
        {
            _isSavingAudit = true;

            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name
                              ?? _httpContextAccessor?.HttpContext?.Session.GetString("UserName")
                              ?? "System";
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var changedAt = DateTime.Now;

            var logs = pendingEntries.Select(a => new AuditLog
            {
                TableName = a.TableName,
                Operation = a.Operation,
                RecordId = a.RecordId ?? GetRecordId(a.Entry),
                OldData = a.OldData == null ? null : JsonSerializer.Serialize(a.OldData),
                NewData = a.NewData == null ? null : JsonSerializer.Serialize(a.NewData),
                ModuleName = a.TableName,
                ChangedBy = currentUser,
                ChangedAt = changedAt,
                IPAddress = ipAddress
            }).ToList();

            AuditLogs.AddRange(logs);
            await base.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Audit failures must never break primary business operations.
        }
        finally
        {
            _isSavingAudit = false;
        }
    }

    private sealed class PendingAuditEntry
    {
        public required EntityEntry Entry { get; set; }
        public required string TableName { get; set; }
        public required string Operation { get; set; }
        public string? RecordId { get; set; }
        public Dictionary<string, object?>? OldData { get; set; }
        public Dictionary<string, object?>? NewData { get; set; }
    }
}
