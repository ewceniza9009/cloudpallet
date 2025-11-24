using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Abstractions.Caching;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Abstractions.Security;
using WMS.Application.Services;
using WMS.Domain.Services;
using WMS.Infrastructure.Caching;
using WMS.Infrastructure.Integrations;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Persistence.Interceptors;
using WMS.Infrastructure.Persistence.Repositories;
using WMS.Infrastructure.Reports;

namespace WMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditingInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<WmsDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString)
                   .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WmsDbContext>());

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IWarehouseAdminRepository, WarehouseAdminRepository>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<ITruckRepository, TruckRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddScoped<ICargoManifestRepository, CargoManifestRepository>();

        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<IRateRepository, RateRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IBillOfMaterialRepository, BillOfMaterialRepository>();
        services.AddScoped<IPalletTypeRepository, PalletTypeRepository>();

        services.AddScoped<IDockAppointmentRepository, DockAppointmentRepository>();
        services.AddScoped<IReadOnlyAppointmentRepository, ReadOnlyAppointmentRepository>();

        services.AddScoped<IMaterialInventoryRepository, MaterialInventoryRepository>();

        services.AddScoped<IReceivingTransactionRepository, ReceivingTransactionRepository>();
        services.AddScoped<IPutawayTransactionRepository, PutawayTransactionRepository>();
        services.AddScoped<ITransferTransactionRepository, TransferTransactionRepository>();
        services.AddScoped<IItemTransferTransactionRepository, ItemTransferTransactionRepository>();
        services.AddScoped<IPickTransactionRepository, PickTransactionRepository>();
        services.AddScoped<IWithdrawalTransactionRepository, WithdrawalTransactionRepository>();

        services.AddScoped<IVASTransactionRepository, VASTransactionRepository>();
        services.AddScoped<IVASTransactionAmendmentRepository, VASTransactionAmendmentRepository>();

        services.AddScoped<IInventoryAdjustmentRepository, InventoryAdjustmentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IBillingService, BillingService>();

        services.AddScoped<IReportGenerator, PdfReportGenerator>();
        services.AddScoped<IReportRepository, ReportRepository>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHttpClient("ScaleApi", client => { client.BaseAddress = new Uri(configuration["ExternalServices:ScaleApiUrl"]!); });
        services.AddHttpClient("AsrsApi", client => { client.BaseAddress = new Uri(configuration["ExternalServices:AsrsApiUrl"]!); });
        services.AddScoped<IScaleApiService, ScaleApiService>();
        services.AddScoped<IAutomationService, AutomationService>();
        services.AddScoped<IBarcodeGenerationService, BarcodeGenerationService>();

        return services;
    }
}