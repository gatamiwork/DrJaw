using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DrJaw.Models;
using DrJaw.Services.MSSQL;

namespace DrJaw.Services.Data
{
    public sealed class ReferenceDataService : IReferenceDataService
    {
        private readonly IMssqlRepository _repo;

        private readonly ObservableCollection<MSSQLUser> _users = new();
        private readonly ObservableCollection<MSSQLMart> _marts = new();
        private readonly ObservableCollection<MSSQLMetal> _metals = new();

        public ReadOnlyObservableCollection<MSSQLUser> Users { get; }
        public ReadOnlyObservableCollection<MSSQLMart> Marts { get; }
        public ReadOnlyObservableCollection<MSSQLMetal> Metals { get; }


        public event EventHandler? Changed;

        public ReferenceDataService(IMssqlRepository repo)
        {
            _repo = repo;
            Users = new ReadOnlyObservableCollection<MSSQLUser>(_users);
            Marts = new ReadOnlyObservableCollection<MSSQLMart>(_marts);
            Metals = new ReadOnlyObservableCollection<MSSQLMetal>(_metals);
        }

        public async Task EnsureLoadedAsync()
        {
            if (_users.Count > 0 && _marts.Count > 0) return;
            await ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            var users = await _repo.GetUsersAsync();
            var marts = await _repo.GetMartsAsync();
            var metals = await _repo.GetMetalsAsync(); // ← добавь

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                Apply(users, marts, metals);
            }
            else
            {
                await Application.Current!.Dispatcher.InvokeAsync(() => Apply(users, marts, metals));
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }
        private void Apply(
            System.Collections.Generic.IReadOnlyList<MSSQLUser> users,
            System.Collections.Generic.IReadOnlyList<MSSQLMart> marts,
            System.Collections.Generic.IReadOnlyList<MSSQLMetal> metals)
        {
            _users.Clear();
            foreach (var u in users.OrderBy(x => x.Name))
                _users.Add(u);

            _marts.Clear();
            foreach (var m in marts.OrderBy(x => x.Name))
                _marts.Add(m);

            _metals.Clear();
            foreach (var m in metals.OrderBy(x => x.Name))
                _metals.Add(m);
        }

        public MSSQLUser? FindUser(int id) => _users.FirstOrDefault(u => u.Id == id);
        public MSSQLMart? FindMart(int id) => _marts.FirstOrDefault(m => m.Id == id);
        public MSSQLMetal? FindMetal(int id) => _metals.FirstOrDefault(x => x.Id == id);
    }
}
