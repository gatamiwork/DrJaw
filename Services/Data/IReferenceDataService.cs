using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DrJaw.Models;

namespace DrJaw.Services.Data
{
    public interface IReferenceDataService
    {
        ReadOnlyObservableCollection<MSSQLUser> Users { get; }
        ReadOnlyObservableCollection<MSSQLMart> Marts { get; }
        ReadOnlyObservableCollection<MSSQLMetal> Metals { get; }

        Task EnsureLoadedAsync();
        Task ReloadAsync();

        MSSQLUser? FindUser(int id);
        MSSQLMart? FindMart(int id);
        MSSQLMetal? FindMetal(int id);

        event EventHandler? Changed;
    }
}
