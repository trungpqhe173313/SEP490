using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NB.Model.Entities;

public partial class NutriBarn2025Context : DbContext
{
    public NutriBarn2025Context()
    {
    }

    public NutriBarn2025Context(DbContextOptions<NutriBarn2025Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public virtual DbSet<Finishproduct> Finishproducts { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PriceList> PriceLists { get; set; }

    public virtual DbSet<PriceListDetail> PriceListDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }

    public virtual DbSet<ReturnTransaction> ReturnTransactions { get; set; }

    public virtual DbSet<ReturnTransactionDetail> ReturnTransactionDetails { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }

    public virtual DbSet<StockAdjustmentDetail> StockAdjustmentDetails { get; set; }

    public virtual DbSet<StockBatch> StockBatches { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TransactionDetail> TransactionDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<Worklog> Worklogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);Database=NutriBarn2025;User Id=sa;Password=123;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__19093A2BC08476DE");

            entity.ToTable("Category");

            entity.HasIndex(e => e.CategoryName, "UQ__Category__8517B2E0F8063CB7").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__C90D340983696E22");

            entity.ToTable("Contract");

            entity.Property(e => e.ContractId).HasColumnName("ContractID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Image).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__Contract__Suppli__29221CFB");

            entity.HasOne(d => d.User).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Contract__UserID__2A164134");
        });

        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.HasKey(e => e.FinancialTransactionId).HasName("PK__Financia__6418105764744B83");

            entity.ToTable("FinancialTransaction");

            entity.Property(e => e.FinancialTransactionId).HasColumnName("FinancialTransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.RelatedTransactionId).HasColumnName("RelatedTransactionID");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.RelatedTransaction).WithMany(p => p.FinancialTransactions)
                .HasForeignKey(d => d.RelatedTransactionId)
                .HasConstraintName("FK__Financial__Relat__2B0A656D");
        });

        modelBuilder.Entity<Finishproduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Finishpr__3214EC27D9D5AFA3");

            entity.ToTable("Finishproduct");

            entity.HasIndex(e => e.ProductId, "IX_ProductionOutput_ProductID");

            entity.HasIndex(e => e.ProductionId, "IX_ProductionOutput_ProductionID");

            entity.HasIndex(e => e.WarehouseId, "IX_ProductionOutput_WarehouseID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductionId).HasColumnName("ProductionID");
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Product).WithMany(p => p.Finishproducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Finishpro__Produ__2CF2ADDF");

            entity.HasOne(d => d.Production).WithMany(p => p.Finishproducts)
                .HasForeignKey(d => d.ProductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Finishpro__Produ__2BFE89A6");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Finishproducts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Finishpro__Wareh__2DE6D218");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6D35CA0BB94");

            entity.ToTable("Inventory");

            entity.HasIndex(e => e.ProductId, "IX_Inventory_ProductID");

            entity.HasIndex(e => e.WarehouseId, "IX_Inventory_WarehouseID");

            entity.HasIndex(e => new { e.WarehouseId, e.ProductId }, "UQ_Inventory_Warehouse_Product").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Produ__2EDAF651");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Wareh__2FCF1A8A");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Job__3214EC271F77432F");

            entity.ToTable("Job");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");
            entity.Property(e => e.JobName).HasMaxLength(100);
            entity.Property(e => e.PayType).HasMaxLength(50);
            entity.Property(e => e.Rate).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Material__3214EC274BEFD44D");

            entity.ToTable("Material");

            entity.HasIndex(e => e.ProductId, "IX_ProductionInput_ProductID");

            entity.HasIndex(e => e.ProductionId, "IX_ProductionInput_ProductionID");

            entity.HasIndex(e => e.WarehouseId, "IX_ProductionInput_WarehouseID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductionId).HasColumnName("ProductionID");
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Product).WithMany(p => p.Materials)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Material__Produc__30C33EC3");

            entity.HasOne(d => d.Production).WithMany(p => p.Materials)
                .HasForeignKey(d => d.ProductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Material__Produc__31B762FC");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Materials)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Material__Wareho__32AB8735");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("PK__Payroll__99DFC69277F3C5B6");

            entity.ToTable("Payroll");

            entity.Property(e => e.PayrollId).HasColumnName("PayrollID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.FinancialTransactionId).HasColumnName("FinancialTransactionID");
            entity.Property(e => e.IsPaid).HasDefaultValue(false);
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaidDate).HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payroll__Employe__339FAB6E");

            entity.HasOne(d => d.FinancialTransaction).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.FinancialTransactionId)
                .HasConstraintName("FK__Payroll__Financi__3493CFA7");
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.PriceListId).HasName("PK__PriceLis__1E30F34C6FA6330E");

            entity.ToTable("PriceList");

            entity.Property(e => e.PriceListId).HasColumnName("PriceListID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PriceListName).HasMaxLength(100);
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<PriceListDetail>(entity =>
        {
            entity.HasKey(e => e.PriceListDetailId).HasName("PK__PriceLis__7BE7C664535BD4BF");

            entity.ToTable("PriceListDetail");

            entity.Property(e => e.PriceListDetailId).HasColumnName("PriceListDetailID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PriceListId).HasColumnName("PriceListID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.PriceList).WithMany(p => p.PriceListDetails)
                .HasForeignKey(d => d.PriceListId)
                .HasConstraintName("FK__PriceList__Price__3587F3E0");

            entity.HasOne(d => d.Product).WithMany(p => p.PriceListDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__PriceList__Produ__367C1819");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__B40CC6ED5DBA61E3");

            entity.ToTable("Product");

            entity.HasIndex(e => e.CategoryId, "IX_Product_CategoryID");

            entity.HasIndex(e => e.SupplierId, "IX_Product_SupplierID");

            entity.HasIndex(e => e.ProductCode, "UQ__Product__2F4E024F7AF0B4A5").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(200)
                .HasColumnName("ImageURL");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.WeightPerUnit).HasColumnType("decimal(10, 3)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Product__Categor__37703C52");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Product__Supplie__3864608B");
        });

        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producti__3214EC2705ADAE14");

            entity.ToTable("ProductionOrder");

            entity.HasIndex(e => e.StartDate, "IX_ProductionOrder_StartDate");

            entity.HasIndex(e => e.Status, "IX_ProductionOrder_Status");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue(2);
        });

        modelBuilder.Entity<ReturnTransaction>(entity =>
        {
            entity.HasKey(e => e.ReturnTransactionId).HasName("PK__ReturnTr__4E8C11A66868D007");

            entity.ToTable("ReturnTransaction");

            entity.Property(e => e.ReturnTransactionId).HasColumnName("ReturnTransactionID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");

            entity.HasOne(d => d.Transaction).WithMany(p => p.ReturnTransactions)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReturnTra__Trans__395884C4");
        });

        modelBuilder.Entity<ReturnTransactionDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnTr__3214EC27F15126BD");

            entity.ToTable("ReturnTransactionDetail");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ReturnTransactionId).HasColumnName("ReturnTransactionID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.ReturnTransactionDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReturnTra__Produ__3A4CA8FD");

            entity.HasOne(d => d.ReturnTransaction).WithMany(p => p.ReturnTransactionDetails)
                .HasForeignKey(d => d.ReturnTransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReturnTra__Retur__3B40CD36");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3A2AC65594");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61601B45407E").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId).HasName("PK__StockAdj__E60DB8B3FC00D789");

            entity.ToTable("StockAdjustment");

            entity.Property(e => e.AdjustmentId).HasColumnName("AdjustmentID");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockAdjustments)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockAdjustment_Warehouse");
        });

        modelBuilder.Entity<StockAdjustmentDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__StockAdj__135C314D238817F6");

            entity.ToTable("StockAdjustmentDetail");

            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.ActualQuantity).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.AdjustmentId).HasColumnName("AdjustmentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Adjustment).WithMany(p => p.StockAdjustmentDetails)
                .HasForeignKey(d => d.AdjustmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockAdjustmentDetail_StockAdjustment");

            entity.HasOne(d => d.Product).WithMany(p => p.StockAdjustmentDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockAdjustmentDetail_Product");
        });

        modelBuilder.Entity<StockBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__StockBat__5D55CE38AE290C30");

            entity.ToTable("StockBatch");

            entity.HasIndex(e => new { e.WarehouseId, e.ProductId }, "StockBatch_index_23");

            entity.HasIndex(e => new { e.ProductId, e.ImportDate }, "StockBatch_index_24");

            entity.HasIndex(e => e.BatchCode, "UQ__StockBat__B22ADA8EC12B1922").IsUnique();

            entity.Property(e => e.BatchId).HasColumnName("BatchID");
            entity.Property(e => e.BatchCode).HasMaxLength(50);
            entity.Property(e => e.ExpireDate).HasColumnType("datetime");
            entity.Property(e => e.ImportDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductionFinishId).HasColumnName("ProductionFinishID");
            entity.Property(e => e.QuantityIn).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.QuantityOut)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Product).WithMany(p => p.StockBatches)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StockBatc__Produ__3F115E1A");

            entity.HasOne(d => d.ProductionFinish).WithMany(p => p.StockBatches)
                .HasForeignKey(d => d.ProductionFinishId)
                .HasConstraintName("FK__StockBatc__Produ__40058253");

            entity.HasOne(d => d.Transaction).WithMany(p => p.StockBatches)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("FK__StockBatc__Trans__40F9A68C");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockBatches)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StockBatc__Wareh__41EDCAC5");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666945D9729E9");

            entity.ToTable("Supplier");

            entity.HasIndex(e => e.Email, "UQ__Supplier__A9D105349D40375F").IsUnique();

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SupplierName).HasMaxLength(100);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A4B1B211370");

            entity.ToTable("Transaction");

            entity.HasIndex(e => e.TransactionDate, "IX_Transaction_Date");

            entity.HasIndex(e => e.CustomerId, "IX_Transaction_UserID");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PriceListId).HasColumnName("PriceListID");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.TransactionCode).HasMaxLength(100);
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TransactionQr)
                .HasMaxLength(500)
                .HasColumnName("TransactionQR");
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.WarehouseInId).HasColumnName("WarehouseInID");
        });

        modelBuilder.Entity<TransactionDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transact__3214EC276AC542D8");

            entity.ToTable("TransactionDetail");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.TransactionDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__Produ__42E1EEFE");

            entity.HasOne(d => d.Transaction).WithMany(p => p.TransactionDetails)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__Trans__43D61337");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCACDF3ADD0E");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "IX_User_Email");

            entity.HasIndex(e => e.Username, "IX_User_Username");

            entity.HasIndex(e => e.Username, "UQ__User__536C85E4B5D0F734").IsUnique();

            entity.HasIndex(e => e.Phone, "UQ__User__5C7E359E9C47F59E").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534BDA1E04B").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Image).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RefreshTokenExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK__UserRole__3D978A5598895158");

            entity.ToTable("UserRole");

            entity.HasIndex(e => e.RoleId, "IX_UserRole_RoleID");

            entity.HasIndex(e => e.UserId, "IX_UserRole_UserID");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "UQ_UserRole_User_Role").IsUnique();

            entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__RoleID__44CA3770");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__UserID__45BE5BA9");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFD963218374");

            entity.ToTable("Warehouse");

            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.WarehouseName).HasMaxLength(100);
        });

        modelBuilder.Entity<Worklog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Worklog__3214EC275F752BAB");

            entity.ToTable("Worklog");

            entity.HasIndex(e => e.EmployeeId, "IX_Worklog_EmployeeID");

            entity.HasIndex(e => e.JobId, "IX_Worklog_JobID");

            entity.HasIndex(e => e.TransactionId, "IX_Worklog_TransactionID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Rate).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.WorkDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.Worklogs)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Worklog__Employe__46B27FE2");

            entity.HasOne(d => d.Job).WithMany(p => p.Worklogs)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Worklog__JobID__47A6A41B");

            entity.HasOne(d => d.Transaction).WithMany(p => p.Worklogs)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("FK__Worklog__Transac__489AC854");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
