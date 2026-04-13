using StoreManagement.Shared.DTOs;

namespace StoreManagement.Shared.Interfaces;

public interface IBootstrapService
{
    Task<BootstrapDto> GetInitialAppDataAsync();
}
