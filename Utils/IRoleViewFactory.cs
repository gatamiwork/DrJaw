using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrJaw.Utils
{
    public interface ICleanup
    {
        void Cleanup(); // отменить фоновые операции, отписаться от событий, очистить коллекции
    }

    public interface IRefreshable
    {
        void Refresh(); // перезагрузить данные
    }
    public interface ISwitchUserPanel : ICleanup, IRefreshable
    {
        // Старое API. Благодаря дефолтной реализации (C# 8+, .NET 8) 
        // новые панели НЕ обязаны его реализовывать.
        void CleanupBeforeUnload() => Cleanup();
    }
}
